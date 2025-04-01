using System;
using SparFlame.GamePlaySystem.General;
using SparFlame.GamePlaySystem.Mouse;
using SparFlame.GamePlaySystem.Resource;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Physics.Stateful;
using Unity.Rendering;
using Unity.Transforms;

// ReSharper disable ForeachCanBePartlyConvertedToQueryUsingAnotherGetEnumerator

namespace SparFlame.GamePlaySystem.Building
{
    public partial struct PlacementSystem : ISystem
    {
        private ComponentLookup<MaterialMeshInfo> _materialLookup;
        private ComponentLookup<LocalTransform> _localTransformLookup;
        private ComponentLookup<Constructable> _constructableLookup;
        private BufferLookup<CostList> _costLookup;
        private BufferLookup<ResourceData> _resourceLookup;
        private BufferLookup<LinkedEntityGroup> _childLookup;
        private BufferLookup<StatefulTriggerEvent> _triggerEvents;


        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<EndSimulationEntityCommandBufferSystem.Singleton>();
            state.RequireForUpdate<NotPauseTag>();
            state.RequireForUpdate<CustomInputSystemData>();
            state.RequireForUpdate<PlacementSystemConfig>();

            _materialLookup = state.GetComponentLookup<MaterialMeshInfo>();
            _localTransformLookup = state.GetComponentLookup<LocalTransform>();
            _constructableLookup = state.GetComponentLookup<Constructable>(true);

            _childLookup = state.GetBufferLookup<LinkedEntityGroup>(true);
            _triggerEvents = state.GetBufferLookup<StatefulTriggerEvent>(true);
            _costLookup = state.GetBufferLookup<CostList>(true);
            _resourceLookup = state.GetBufferLookup<ResourceData>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            // TODO : Support for building movement
            // TODO : Add construction time and animation support
            _materialLookup.Update(ref state);
            _localTransformLookup.Update(ref state);
            _constructableLookup.Update(ref state);

            _childLookup.Update(ref state);
            _resourceLookup.Update(ref state);
            _triggerEvents.Update(ref state);
            _costLookup.Update(ref state);

            var ecb = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>();
            var config = SystemAPI.GetSingleton<PlacementSystemConfig>();
            var customInputData = SystemAPI.GetSingleton<CustomInputSystemData>();
            new PlacementJob
            {
                ConstructableLookup = _constructableLookup,
                LocalTransformLookup = _localTransformLookup,
                MaterialLookup = _materialLookup,
                ResourceLookup = _resourceLookup,
                ChildLookup = _childLookup,
                TriggerEvents = _triggerEvents,
                CostLookup = _costLookup,
                Config = config,
                CustomInputData = customInputData,
                ECB = ecb.CreateCommandBuffer(state.WorldUnmanaged).AsParallelWriter()
            }.ScheduleParallel();

        }

        


        [BurstCompile]
        public partial struct PlacementJob : IJobEntity
        {
            [ReadOnly] public ComponentLookup<Constructable> ConstructableLookup;
            [NativeDisableParallelForRestriction] public ComponentLookup<LocalTransform> LocalTransformLookup;
            [NativeDisableParallelForRestriction] public ComponentLookup<MaterialMeshInfo> MaterialLookup;
            [NativeDisableParallelForRestriction] public BufferLookup<ResourceData> ResourceLookup;

            [ReadOnly] public BufferLookup<LinkedEntityGroup> ChildLookup;
            [ReadOnly] public BufferLookup<StatefulTriggerEvent> TriggerEvents;
            [ReadOnly] public BufferLookup<CostList> CostLookup;


            [ReadOnly] public PlacementSystemConfig Config;
            [ReadOnly] public CustomInputSystemData CustomInputData;
            public EntityCommandBuffer.ParallelWriter ECB;

            private void Execute([ChunkIndexInQuery] int index, ref PlacementCommandData data, Entity entity)
            {
                DynamicBuffer<ResourceData> resourceData;
                if (data.Faction == FactionTag.Ally)
                {
                    ResourceLookup.TryGetBuffer(Config.AllyResourceEntity, out resourceData);
                }
                else
                {
                    ResourceLookup.TryGetBuffer(Config.EnemyResourceEntity, out resourceData);
                }

                switch (data.CommandType)
                {
                    case PlacementCommandType.Drag:
                        // Check if resource is available
                        foreach (var cost in CostLookup[data.TargetBuilding])
                        {
                            if (resourceData[(int)cost.Type].Amount < cost.Amount)
                            {
                                SwitchBuildingState(ref data, PlacementStateType.NotEnoughResources, false);
                            }
                        }
                        // Check if overlap with other colliders
                        var events = TriggerEvents[data.GhostEntity];
                        if (events.Length > 0)
                        {
                            SwitchBuildingState(ref data, PlacementStateType.Overlapping, false);
                        }
                        // Check if mouse hit on constructable area
                        if (!ConstructableLookup.TryGetComponent(CustomInputData.HitEntity, out var constructable) ||
                            constructable.Faction != data.Faction)
                        {
                            SwitchBuildingState(ref data, PlacementStateType.NotConstructable, false);
                        }
                        // Synchronize the position and rotation of ghost building and ghost trigger with the input position
                        ref var ghostTransform = ref LocalTransformLookup.GetRefRW(data.GhostEntity).ValueRW;
                        ref var triggerTransform = ref LocalTransformLookup.GetRefRW(data.GhostTriggerEntity).ValueRW;
                        ghostTransform.Position = CustomInputData.HitPosition;
                        triggerTransform.Position = CustomInputData.HitPosition;
                        ghostTransform.Rotation = data.Rotation;
                        triggerTransform.Rotation = data.Rotation;
                        return;
                    
                    case PlacementCommandType.Start:
                        // Check if switch building, then should destroy prior ghost preview
                        if (data.GhostEntity != Entity.Null)
                        {
                            DestroyPriorGhost(index, ref data);
                        }
                        // Create ghost preview
                        var child = ChildLookup[data.GhostEntity][Config.ModelChildIndex].Value;
                        data.GhostEntity = ECB.Instantiate(index, child);
                        data.GhostTriggerEntity = ECB.Instantiate(index, Config.GhostTriggerPrefab);
                        SwitchBuildingState(ref data, PlacementStateType.Valid, true);
                        data.CommandType = PlacementCommandType.Drag;
                        return;
                    
                    case PlacementCommandType.End:
                        DestroyPriorGhost(index, ref data);
                        ECB.DestroyEntity(index, entity);
                        
                        break;
                        

                    case PlacementCommandType.Build when data.State == PlacementStateType.Valid:
                        // Reduce resources
                        foreach (var cost in CostLookup[data.TargetBuilding])
                        {
                            var r = resourceData[(int)cost.Type];
                            r.Amount -= cost.Amount;
                            resourceData[(int)cost.Type] = r;
                        }

                        // Create building
                        var targetBuilding = ECB.Instantiate(index, data.TargetBuilding);
                        ref var transform = ref LocalTransformLookup.GetRefRW(targetBuilding).ValueRW;
                        transform.Position = CustomInputData.HitPosition;
                        transform.Rotation = data.Rotation;
                        data.CommandType = PlacementCommandType.Drag;
                        return;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }

            private void DestroyPriorGhost(int index, ref PlacementCommandData data)
            {
                ECB.DestroyEntity(index, data.GhostEntity);
                ECB.DestroyEntity(index, data.GhostTriggerEntity);
                data.GhostEntity = Entity.Null;
                data.GhostTriggerEntity = Entity.Null;
                data.CommandType = PlacementCommandType.Drag;
            }


            private void SwitchBuildingState(ref PlacementCommandData data,
                in PlacementStateType targetState, bool force)
            {
                if (targetState == data.State && !force) return;
                data.State = targetState;
                var material = MaterialLookup.GetRefRW(data.GhostEntity);
                var targetMaterial = targetState switch
                {
                    PlacementStateType.Valid => MaterialLookup[Config.ValidPreset].Material,
                    PlacementStateType.Overlapping => MaterialLookup[Config.OverlappingPreset].Material,
                    PlacementStateType.NotEnoughResources => MaterialLookup[Config.NotEnoughResourcesPreset]
                        .Material,
                    PlacementStateType.NotConstructable => MaterialLookup[Config.NotConstructablePreset].Material,
                    _ => throw new ArgumentOutOfRangeException(nameof(targetState), targetState, null)
                };
                material.ValueRW.Material = targetMaterial;
            }
            
            
        }
    }
}