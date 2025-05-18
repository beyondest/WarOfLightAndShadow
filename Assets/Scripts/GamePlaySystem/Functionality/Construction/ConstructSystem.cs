using System;
using System.Runtime.CompilerServices;
using SparFlame.GamePlaySystem.General;
using SparFlame.GamePlaySystem.CustomInput;
using SparFlame.GamePlaySystem.Movement;
using SparFlame.GamePlaySystem.Resource;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Physics.Stateful;
using Unity.Rendering;
using Unity.Transforms;
using BoxCollider = Unity.Physics.BoxCollider;

// ReSharper disable ForeachCanBePartlyConvertedToQueryUsingAnotherGetEnumerator
namespace SparFlame.GamePlaySystem.Building
{
    [UpdateBefore(typeof(TransformSystemGroup))]
    public partial struct ConstructSystem : ISystem
    {
        private ComponentLookup<OccupiedTag> _constructableLookup;

        private BufferLookup<CostList> _costLookup;

        private EntityQuery _playerBaseQuery;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<PlayerFactionData>();
            state.RequireForUpdate<EnemyResourceDataTag>();
            state.RequireForUpdate<AllyResourceDataTag>();
            state.RequireForUpdate<EndSimulationEntityCommandBufferSystem.Singleton>();
            state.RequireForUpdate<GameStatusData>();
            state.RequireForUpdate<InputMouseData>();
            state.RequireForUpdate<ConstructSystemPrefabRef>();
            state.RequireForUpdate<CrystalAffectRadiusSq>();
            state.RequireForUpdate<ConstructCommandData>();
            _constructableLookup = state.GetComponentLookup<OccupiedTag>(true);
            _costLookup = state.GetBufferLookup<CostList>(true);

            _playerBaseQuery = SystemAPI.QueryBuilder().WithAll<LocalTransform>().WithAll<PlayerTag>()
                .WithAll<CoreCrystalTag>().Build();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            // TODO : Add construction time and animation support
            // TODO : Change construction only use for one team, turn command data to singleton
            // TODO : All player buildings can only be built in sight, not in fow
            var gameStatusData = SystemAPI.GetSingleton<GameStatusData>().Value;
            ref var data = ref SystemAPI.GetSingletonRW<ConstructCommandData>().ValueRW;

            if (gameStatusData == GameStatus.Init)
            {
                data.Faction = SystemAPI.GetSingleton<PlayerFactionData>().Value;
                data.CommandType = ConstructCommandType.None;
                return;
            }
            _constructableLookup.Update(ref state);
            _costLookup.Update(ref state);

            if (_playerBaseQuery.IsEmpty || data.CommandType == ConstructCommandType.None) return;

            var config = SystemAPI.GetSingleton<ConstructSystemPrefabRef>();
            var affectRadiusSq = SystemAPI.GetSingleton<CrystalAffectRadiusSq>().Value;
            var customInputData = SystemAPI.GetSingleton<InputMouseData>();
            var allyResourceData =
                SystemAPI.GetBuffer<ResourceTypeToAvailableAmount>(SystemAPI.GetSingletonEntity<AllyResourceDataTag>());
            var enemyResourceData =
                SystemAPI.GetBuffer<ResourceTypeToAvailableAmount>(SystemAPI
                    .GetSingletonEntity<EnemyResourceDataTag>());

            var ecb = new EntityCommandBuffer(Allocator.Temp);
            var playerBaseTrans = _playerBaseQuery.ToComponentDataArray<LocalTransform>(Allocator.Temp);
            
            CheckConstructionCommand(ref state, allyResourceData, enemyResourceData, config,
                customInputData, ecb, affectRadiusSq, playerBaseTrans);
            playerBaseTrans.Dispose();
            ecb.Playback(state.EntityManager);
            ecb.Dispose();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool CheckIfInCrystalRange(float3 position, in NativeArray<LocalTransform> trans,
            float radiusSq)
        {
            var inRadius = false;
            foreach (var transform in trans)
            {
                if (math.distancesq(position, transform.Position) < radiusSq)
                {
                    inRadius = true;
                    break;
                }
            }

            return inRadius;
        }

        private void CheckConstructionCommand(ref SystemState state,
            DynamicBuffer<ResourceTypeToAvailableAmount> allyResourceData,
            DynamicBuffer<ResourceTypeToAvailableAmount> enemyResourceData,
            ConstructSystemPrefabRef prefabRef,
            InputMouseData customInputData, EntityCommandBuffer ecb,
            float affectRadiusSq,
            in NativeArray<LocalTransform> playerBaseTrans)
        {
            ref var data = ref SystemAPI.GetSingletonRW<ConstructCommandData>().ValueRW;
            var resourceData = data.Faction == FactionTag.Ally ? allyResourceData : enemyResourceData;
            var buildingAttr = SystemAPI.GetComponent<BuildingAttr>(data.TargetBuilding);
            var isCrystal = buildingAttr is
                { Type: BuildingType.Ornaments, SubTypeIndex: (int)OrnamentType.Crystal };

            
            
            switch (data.CommandType)
            {
                case ConstructCommandType.Drag:
                    var valid = true;

                    // Check if resource is available
                    if (!data.IsMovementShow)
                    {
                        foreach (var cost in _costLookup[data.TargetBuilding])
                        {
                            if (resourceData[(int)cost.Type].Amount < cost.Amount)
                            {
                                SwitchBuildingState(ref state, ref data, PlacementStateType.NotEnoughResources,
                                    in prefabRef, false);
                                valid = false;
                            }
                        }
                    }

                    // Check if overlap with other colliders
                    var events = SystemAPI.GetBuffer<StatefulTriggerEvent>(data.GhostTriggerEntity);
                    if (events.Length > 0)
                    {
                        SwitchBuildingState(ref state, ref data, PlacementStateType.Overlapping, in prefabRef, false);
                        valid = false;
                    }

                    // Check if mouse hit on constructable area; crystal can turn neutral area to cur faction
                    if (!_constructableLookup.TryGetComponent(customInputData.HitEntity, out var constructable) ||
                        (!isCrystal && constructable.Faction != data.Faction)
                        || (isCrystal && constructable.Faction == ~data.Faction)
                       )
                    {
                        SwitchBuildingState(ref state, ref data, PlacementStateType.NotConstructable, in prefabRef,
                            false);
                        valid = false;
                    }

                    // Only is constructable alongside the crystal
                    /*if (!CheckIfInCrystalRange(customInputData.HitPosition, playerBaseTrans, affectRadiusSq))
                    {
                        
                    }*/

                    if (valid)
                        SwitchBuildingState(ref state, ref data, PlacementStateType.Valid, in prefabRef, false);

                    // Synchronize the position and rotation of ghost building and ghost trigger with the input position
                    ref var ghostTransform =
                        ref SystemAPI.GetComponentRW<LocalTransform>(data.GhostModelEntity).ValueRW;
                    ref var triggerTransform =
                        ref SystemAPI.GetComponentRW<LocalTransform>(data.GhostTriggerEntity).ValueRW;

                    // Get Target Transform
                    var targetTransform = ghostTransform;
                    float rotateAngle;
                    if (math.abs(data.RotationAngle).Equals(15f))
                    {
                        var curDeg = ConstructUtils.GetCurrentYDeg(targetTransform.Rotation);
                        rotateAngle = ConstructUtils.SnapToNearest15(curDeg, data.RotationAngle);
                    }
                    else
                    {
                        rotateAngle = data.RotationAngle;
                    }

                    var rotationDelta = quaternion.RotateY(math.radians(rotateAngle));
                    targetTransform.Position = customInputData.HitPosition;
                    targetTransform.Scale = 1;
                    targetTransform.Rotation =
                        math.normalizesafe(math.mul(targetTransform.Rotation, rotationDelta));
                    ghostTransform = targetTransform;
                    triggerTransform = targetTransform;
                    break;

                case ConstructCommandType.Start:
                    // Check if switch building, then should destroy prior ghost preview
                    if (data.GhostModelEntity != Entity.Null)
                    {
                        DestroyPriorGhost(ref state, ref data);
                    }

                    // Create ghost preview
                    data.GhostModelEntity = InstantiateChildrenWithNewParent(ref state, data.TargetBuilding);
                    data.GhostTriggerEntity = state.EntityManager.Instantiate(prefabRef.GhostTriggerPrefab);
                    state.EntityManager.AddComponent<GameplayEntityTag>(data.GhostModelEntity);
                    state.EntityManager.AddComponent<GameplayEntityTag>(data.GhostTriggerEntity);
                    SwitchBuildingState(ref state, ref data, PlacementStateType.Valid, in prefabRef, true);
                    AlignTriggerBoxCollider(ref state, in data);
                    data.CommandType = ConstructCommandType.Drag;
                    break;

                case ConstructCommandType.End:
                    if (data.IsMovementShow) // Not move to new place, should return to original location
                    {
                        state.EntityManager.SetComponentData(data.TargetBuilding, data.OriTransform);
                    }

                    DestroyPriorGhost(ref state, ref data);
                    data.CommandType = ConstructCommandType.None;
                    break;

                case ConstructCommandType.Build when data.State == PlacementStateType.Valid:
                    var newTransform = SystemAPI.GetComponent<LocalTransform>(data.GhostModelEntity);

                    if (!data.IsMovementShow)
                    {
                        // Check if crystal, then turn this plane to cur faction
                        if (isCrystal)
                        {
                            var request = ecb.CreateEntity();
                            ecb.AddComponent(request, new ChangeOccupiedTagRequest
                            {
                                CrystalFaction = data.Faction,
                                CrystalPos = customInputData.HitPosition,
                                IsDestroyed = false
                            });
                            ecb.AddComponent<GameplayEntityTag>(request);
                        }

                        // Reduce resources
                        foreach (var cost in _costLookup[data.TargetBuilding])
                        {
                            var r = resourceData[(int)cost.Type];
                            r.Amount -= cost.Amount;
                            resourceData[(int)cost.Type] = r;
                        }

                        // Create building
                        var targetBuilding = state.EntityManager.Instantiate(data.TargetBuilding);
                        state.EntityManager.AddComponent<GameplayEntityTag>(targetBuilding);

                        state.EntityManager.SetComponentData(targetBuilding, newTransform);
                        data.CommandType = ConstructCommandType.Drag; // Continue building
                    }
                    else
                    {
                        // Move building to new place
                        state.EntityManager.SetComponentData(data.TargetBuilding, newTransform);
                        DestroyPriorGhost(ref state, ref data);
                        data.CommandType = ConstructCommandType.None;
                        var syncVolumeRequest = state.EntityManager.CreateEntity();
                        state.EntityManager.AddComponent<BuildingSyncVolumeRequest>(syncVolumeRequest);
                        state.EntityManager.SetComponentData(syncVolumeRequest, new BuildingSyncVolumeRequest
                        {
                            FromEntity = data.TargetBuilding,
                        });
                        state.EntityManager.AddComponent<GameplayEntityTag>(syncVolumeRequest);

                    }

                    break;
                case ConstructCommandType.None:
                    // Do nothing when not enter ghost show mode
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }


        private void DestroyPriorGhost(ref SystemState state, ref ConstructCommandData data)
        {
            state.EntityManager.DestroyEntity(data.GhostModelEntity);
            state.EntityManager.DestroyEntity(data.GhostTriggerEntity);
            data.GhostModelEntity = Entity.Null;
            data.GhostTriggerEntity = Entity.Null;
            data.CommandType = ConstructCommandType.Drag;
        }

        private void SwitchBuildingState(ref SystemState state, ref ConstructCommandData data,
            in PlacementStateType targetState, in ConstructSystemPrefabRef prefabRef, bool force)
        {
            if (targetState == data.State && !force) return;
            data.State = targetState;
            var targetMaterial = targetState switch
            {
                PlacementStateType.Valid => SystemAPI.GetComponent<MaterialMeshInfo>(prefabRef.ValidPreset).Material,
                PlacementStateType.Overlapping => SystemAPI.GetComponent<MaterialMeshInfo>(prefabRef.OverlappingPreset)
                    .Material,
                PlacementStateType.NotEnoughResources => SystemAPI
                    .GetComponent<MaterialMeshInfo>(prefabRef.NotEnoughResourcesPreset)
                    .Material,
                PlacementStateType.NotConstructable => SystemAPI
                    .GetComponent<MaterialMeshInfo>(prefabRef.NotConstructablePreset).Material,
                _ => throw new ArgumentOutOfRangeException(nameof(targetState), targetState, null)
            };
            // for (int i = 1; i < buffer.Length; i++)
            // {
            ChangeMaterialRecursively(ref state, data.GhostModelEntity, targetMaterial);
            // }
        }


        private void ChangeMaterialRecursively(ref SystemState state, Entity entity, int newMaterial)
        {
            if (SystemAPI.HasComponent<MaterialMeshInfo>(entity))
            {
                var material = SystemAPI.GetComponentRW<MaterialMeshInfo>(entity);
                material.ValueRW.Material = newMaterial;
            }

            if (!SystemAPI.HasBuffer<LinkedEntityGroup>(entity)) return;
            var buffer = SystemAPI.GetBuffer<LinkedEntityGroup>(entity);
            for (int i = 1; i < buffer.Length; i++)
            {
                ChangeMaterialRecursively(ref state, buffer[i].Value, newMaterial);
            }
        }

        private void AlignTriggerBoxCollider(ref SystemState state, in ConstructCommandData data)
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
            var newParentEntity = state.EntityManager.CreateEntity();
            state.EntityManager.AddComponent<GameplayEntityTag>(newParentEntity);

            state.EntityManager.AddComponent<LocalTransform>(newParentEntity);
            state.EntityManager.AddComponent<LocalToWorld>(newParentEntity);
            var buffer = state.EntityManager.AddBuffer<LinkedEntityGroup>(newParentEntity);
            buffer.Add(newParentEntity);

            // Get original parent world transform
            var bLtw = state.EntityManager.GetComponentData<LocalToWorld>(oriParentEntity);
            var bLtwInverse = math.inverse(bLtw.Value);

            foreach (var originalChild in originalChildren)
            {
                var newChild = state.EntityManager.Instantiate(originalChild);
                state.EntityManager.AddComponent<GameplayEntityTag>(newChild);

                var childLtw = state.EntityManager.GetComponentData<LocalToWorld>(originalChild);
                var relativeToB = math.mul(bLtwInverse, childLtw.Value);

                // extract position
                float3 position = relativeToB.c3.xyz;

                // extract scale
                float3 scale;
                scale.x = math.length(relativeToB.c0.xyz);
                scale.y = math.length(relativeToB.c1.xyz);
                scale.z = math.length(relativeToB.c2.xyz);

                // normalize basis vectors to remove scale from rotation
                float3x3 rotationMatrix = new float3x3(
                    relativeToB.c0.xyz / scale.x,
                    relativeToB.c1.xyz / scale.y,
                    relativeToB.c2.xyz / scale.z
                );
                quaternion rotation = new quaternion(rotationMatrix);

                // Calculate new transform
                var newLocalTransform = new LocalTransform
                {
                    Position = position,
                    Rotation = rotation,
                    Scale = math.cmax(scale)
                };

                state.EntityManager.SetComponentData(newChild, newLocalTransform);
                state.EntityManager.SetComponentData(newChild, new Parent { Value = newParentEntity });
                var newLinkedEntities = state.EntityManager.GetBuffer<LinkedEntityGroup>(newParentEntity);
                newLinkedEntities.Add(new LinkedEntityGroup { Value = newChild });
            }

            return newParentEntity;
        }


        /*private Entity InstantiateChildrenWithNewParent(ref SystemState state, Entity oriParentEntity)
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
            var newParentEntity = state.EntityManager.CreateEntity();
            state.EntityManager.AddComponent<LocalTransform>(newParentEntity);
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
        }*/
    }
}