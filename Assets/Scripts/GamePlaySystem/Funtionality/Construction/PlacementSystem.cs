using System;
using System.Runtime.CompilerServices;
using SparFlame.GamePlaySystem.General;
using SparFlame.GamePlaySystem.Mouse;
using SparFlame.GamePlaySystem.Resource;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Physics.Stateful;
using Unity.Rendering;
using Unity.Transforms;
using UnityEngine;
using BoxCollider = Unity.Physics.BoxCollider;

// ReSharper disable ForeachCanBePartlyConvertedToQueryUsingAnotherGetEnumerator
// TODO : Check why location of ghost cannot be sync with hit position
namespace SparFlame.GamePlaySystem.Building
{
    [UpdateAfter(typeof(CustomInputSystem))]
    [UpdateBefore(typeof(TransformSystemGroup))]
    public partial struct PlacementSystem : ISystem
    {
        private ComponentLookup<Constructable> _constructableLookup;

        private BufferLookup<CostList> _costLookup;

        private EntityQuery _entityQuery;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<EndSimulationEntityCommandBufferSystem.Singleton>();
            state.RequireForUpdate<NotPauseTag>();
            state.RequireForUpdate<CustomInputSystemData>();
            state.RequireForUpdate<PlacementSystemConfig>();

            _constructableLookup = state.GetComponentLookup<Constructable>(true);
            _costLookup = state.GetBufferLookup<CostList>(true);

            _entityQuery = SystemAPI.QueryBuilder().WithAllRW<PlacementCommandData>().Build();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            // TODO : Add construction time and animation support
            _constructableLookup.Update(ref state);

            _costLookup.Update(ref state);

            if (_entityQuery.IsEmpty) return;
            var config = SystemAPI.GetSingleton<PlacementSystemConfig>();
            var customInputData = SystemAPI.GetSingleton<CustomInputSystemData>();

            var entities = _entityQuery.ToEntityArray(Allocator.Temp);
            var datas = _entityQuery.ToComponentDataArray<PlacementCommandData>(Allocator.Temp);
            for (var i = 0; i < entities.Length; ++i)
            {
                var data = datas[i];
                var entity = entities[i];
                var resourceData = SystemAPI.GetBuffer<ResourceData>(data.Faction == FactionTag.Ally
                    ? config.AllyResourceEntity
                    : config.EnemyResourceEntity);

                switch (data.CommandType)
                {
                    case PlacementCommandType.Drag:
                        var valid = true;
                        // Check if resource is available
                        if (!data.IsMovementShow)
                        {
                            foreach (var cost in _costLookup[data.TargetBuilding])
                            {
                                if (resourceData[(int)cost.Type].Amount < cost.Amount)
                                {
                                    SwitchBuildingState(ref state, ref data, PlacementStateType.NotEnoughResources,
                                        in config, false);
                                    valid = false;
                                }
                            }
                        }

                        // Check if overlap with other colliders
                        var events = SystemAPI.GetBuffer<StatefulTriggerEvent>(data.GhostTriggerEntity);
                        if (events.Length > 0)
                        {
                            SwitchBuildingState(ref state, ref data, PlacementStateType.Overlapping, in config, false);
                            valid = false;
                        }

                        // Check if mouse hit on constructable area
                        if (!_constructableLookup.TryGetComponent(customInputData.HitEntity, out var constructable) ||
                            constructable.Faction != data.Faction)
                        {
                            SwitchBuildingState(ref state, ref data, PlacementStateType.NotConstructable, in config,
                                false);
                            valid = false;
                        }

                        if (valid)
                            SwitchBuildingState(ref state, ref data, PlacementStateType.Valid, in config, false);
                        // Synchronize the position and rotation of ghost building and ghost trigger with the input position
                        ref var ghostTransform =
                            ref SystemAPI.GetComponentRW<LocalTransform>(data.GhostModelEntity).ValueRW;
                        ref var triggerTransform =
                            ref SystemAPI.GetComponentRW<LocalTransform>(data.GhostTriggerEntity).ValueRW;
                        var targetTransform = new LocalTransform
                        {
                            Position = customInputData.HitPosition,
                            Rotation = data.Rotation,
                            Scale = 1
                        };
                        ghostTransform = targetTransform;
                        triggerTransform = targetTransform;
                        state.EntityManager.SetComponentData(entity, data);
                        break;

                    case PlacementCommandType.Start:
                        // Check if switch building, then should destroy prior ghost preview
                        if (data.GhostModelEntity != Entity.Null)
                        {
                            DestroyPriorGhost(ref state, ref data);
                        }

                        // Create ghost preview
                        data.GhostModelEntity = InstantiateChildrenWithNewParent(ref state, data.TargetBuilding);
                        data.GhostTriggerEntity = state.EntityManager.Instantiate(config.GhostTriggerPrefab);
                        SwitchBuildingState(ref state, ref data, PlacementStateType.Valid, in config, true);
                        AlignTriggerBoxCollider(ref state, in data);
                        data.CommandType = PlacementCommandType.Drag;
                        state.EntityManager.SetComponentData(entity, data);
                        break;

                    case PlacementCommandType.End:
                        if (data.IsMovementShow) // Not move to new place, should return to original location
                        {
                            state.EntityManager.SetComponentData(data.TargetBuilding, data.OriTransform);
                        }

                        DestroyPriorGhost(ref state, ref data);
                        state.EntityManager.DestroyEntity(entity);
                        break;

                    case PlacementCommandType.Build when data.State == PlacementStateType.Valid:
                        // Reduce resources
                        var newTransform = new LocalTransform
                        {
                            Position = customInputData.HitPosition,
                            Rotation = data.Rotation,
                            Scale = 1f
                        };
                        if (!data.IsMovementShow)
                        {
                            foreach (var cost in _costLookup[data.TargetBuilding])
                            {
                                var r = resourceData[(int)cost.Type];
                                r.Amount -= cost.Amount;
                                resourceData[(int)cost.Type] = r;
                            }

                            // Create building
                            var targetBuilding = state.EntityManager.Instantiate(data.TargetBuilding);
                            state.EntityManager.SetComponentData(targetBuilding, newTransform);
                            data.CommandType = PlacementCommandType.Drag; // Continue building
                            state.EntityManager.SetComponentData(entity, data);
                        }
                        else
                        {
                            state.EntityManager.SetComponentData(data.TargetBuilding, newTransform);
                            DestroyPriorGhost(ref state, ref data);
                            state.EntityManager.DestroyEntity(entity); // Exit ghost show
                        }

                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
        }

        private void DestroyPriorGhost(ref SystemState state, ref PlacementCommandData data)
        {
            state.EntityManager.DestroyEntity(data.GhostModelEntity);
            state.EntityManager.DestroyEntity(data.GhostTriggerEntity);
            data.GhostModelEntity = Entity.Null;
            data.GhostTriggerEntity = Entity.Null;
            data.CommandType = PlacementCommandType.Drag;
        }

        private void SwitchBuildingState(ref SystemState state, ref PlacementCommandData data,
            in PlacementStateType targetState, in PlacementSystemConfig config, bool force)
        {
            if (targetState == data.State && !force) return;
            data.State = targetState;
            var targetMaterial = targetState switch
            {
                PlacementStateType.Valid => SystemAPI.GetComponent<MaterialMeshInfo>(config.ValidPreset).Material,
                PlacementStateType.Overlapping => SystemAPI.GetComponent<MaterialMeshInfo>(config.OverlappingPreset)
                    .Material,
                PlacementStateType.NotEnoughResources => SystemAPI
                    .GetComponent<MaterialMeshInfo>(config.NotEnoughResourcesPreset)
                    .Material,
                PlacementStateType.NotConstructable => SystemAPI
                    .GetComponent<MaterialMeshInfo>(config.NotConstructablePreset).Material,
                _ => throw new ArgumentOutOfRangeException(nameof(targetState), targetState, null)
            };
            var buffer = SystemAPI.GetBuffer<LinkedEntityGroup>(data.GhostModelEntity);
            for (int i = 1; i < buffer.Length; i++)
            {
                ChangeMaterial(ref state, buffer[i].Value, targetMaterial);
            }
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void ChangeMaterial(ref SystemState state, Entity entity, int newMaterial)
        {
            var material = SystemAPI.GetComponentRW<MaterialMeshInfo>(entity);
            material.ValueRW.Material = newMaterial;
        }

        private void AlignTriggerBoxCollider(ref SystemState state, in PlacementCommandData data)
        {
            var targetCollider = SystemAPI.GetComponent<PhysicsCollider>(data.TargetBuilding);
            var ghostTriggerCollider = SystemAPI.GetComponentRW<PhysicsCollider>(data.GhostTriggerEntity);
            unsafe
            {
                var bxPtr = (BoxCollider*)targetCollider.ColliderPtr;
                var targetBox = bxPtr->Geometry;
                bxPtr = (BoxCollider*)ghostTriggerCollider.ValueRW.ColliderPtr;
                bxPtr->Geometry = targetBox;
            }
        }




        private Entity InstantiateChildrenWithNewParent(ref SystemState state, Entity oriParentEntity)
        {
            if (!SystemAPI.HasBuffer<LinkedEntityGroup>(oriParentEntity))
                return Entity.Null;

            var linkedEntities = SystemAPI.GetBuffer<LinkedEntityGroup>(oriParentEntity);
            if (linkedEntities.Length <= 1)
                return Entity.Null;
            using var originalChildren = new NativeList<Entity>(linkedEntities.Length - 1, Allocator.Temp);

            for (var i = 1; i < linkedEntities.Length; i++)
            {
                originalChildren.Add(linkedEntities[i].Value);
            }

            // Create new parent
            var newParentEntity = state.EntityManager.CreateEntity(typeof(LocalTransform));
            state.EntityManager.AddComponent<LocalToWorld>(newParentEntity);
            var buffer = state.EntityManager.AddBuffer<LinkedEntityGroup>(newParentEntity);
            buffer.Add(newParentEntity);

            // Get original parent world transform
            var bLtw = state.EntityManager.GetComponentData<LocalToWorld>(oriParentEntity);
            var bLtwInverse = math.inverse(bLtw.Value);
            
            foreach (var originalChild in originalChildren)
            {
                var newChild = state.EntityManager.Instantiate(originalChild);
                var childLtw = state.EntityManager.GetComponentData<LocalToWorld>(originalChild);
                var relativeToB = math.mul(bLtwInverse, childLtw.Value);
                // Calculate new transform
                var newLocalTransform = new LocalTransform
                {
                    Position = relativeToB.c3.xyz,
                    Rotation = new quaternion(relativeToB),
                    Scale = 1
                };
                state.EntityManager.SetComponentData(newChild, newLocalTransform);
                state.EntityManager.SetComponentData(newChild, new Parent { Value = newParentEntity });
                var newLinkedEntities = state.EntityManager.GetBuffer<LinkedEntityGroup>(newParentEntity);
                newLinkedEntities.Add(new LinkedEntityGroup { Value = newChild });
            }
            return newParentEntity;
        }
    }
}