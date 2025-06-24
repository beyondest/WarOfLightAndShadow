using SparFlame.Components.General;
using SparFlame.Components.SubGameplay;
using SparFlame.Components.VFX;
using SparFlame.Systems.SubGameplay.Interact;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Physics;
using Unity.Transforms;

namespace SparFlame.Systems.SubGameplay.StateMachine
{
    // This is to make sure only when surround has not targets, will go back to garrison
    [UpdateAfter(typeof(IdleStateMachine))]
    [UpdateAfter(typeof(InteractStateMachine))]
    [UpdateAfter(typeof(MovingStateMachine))]
    [UpdateBefore(typeof(StatSystem))]
    public partial struct GarrisonStateMachine : ISystem
    {
        private ComponentLookup<GarrisonAttr> _garrisonAttrLookup;
        private ComponentLookup<LocalTransform> _localTransformLookup;
        private ComponentLookup<BuildingAttr> _buildingAttrLookup;
        private ComponentLookup<SubGameplayGeneralAttr> _generalAttrLookup;
        private ComponentLookup<Selected> _selectedAttrLookup;

        private BufferLookup<AllowGarrisonUnit> _allowGarrisonUnitLookup;
        private BufferLookup<InsightTarget> _insightTargetLookup;
        private BufferLookup<GarrisonEntity> _garrisonEntityLookup;

        private ComponentLookup<GarrisonStateTag> _garrisonStateTagLookup;
        private ComponentLookup<OocTag> _oocTagLookup;
        private ComponentLookup<ConstructingData> _constructingTagLookup;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<EndSimulationEntityCommandBufferSystem.Singleton>();
            state.RequireForUpdate<SubGamingTag>();
            state.RequireForUpdate<GarrisonStateMachineConfig>();
            state.RequireForUpdate<GarrisonSystemConfig>();
            _localTransformLookup = state.GetComponentLookup<LocalTransform>();
            _garrisonAttrLookup = state.GetComponentLookup<GarrisonAttr>(true);
            _buildingAttrLookup = state.GetComponentLookup<BuildingAttr>(true);
            _allowGarrisonUnitLookup = state.GetBufferLookup<AllowGarrisonUnit>(true);
            _insightTargetLookup = state.GetBufferLookup<InsightTarget>(true);
            _generalAttrLookup = state.GetComponentLookup<SubGameplayGeneralAttr>(true);
            _garrisonStateTagLookup = state.GetComponentLookup<GarrisonStateTag>(true);
            _oocTagLookup = state.GetComponentLookup<OocTag>(true);
            _constructingTagLookup = state.GetComponentLookup<ConstructingData>(true);
            _garrisonEntityLookup = state.GetBufferLookup<GarrisonEntity>(true);
            _selectedAttrLookup = state.GetComponentLookup<Selected>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            _garrisonAttrLookup.Update(ref state);
            _localTransformLookup.Update(ref state);
            _buildingAttrLookup.Update(ref state);
            _allowGarrisonUnitLookup.Update(ref state);
            _insightTargetLookup.Update(ref state);
            _generalAttrLookup.Update(ref state);
            _garrisonStateTagLookup.Update(ref state);
            _oocTagLookup.Update(ref state);
            _constructingTagLookup.Update(ref state);
            _garrisonEntityLookup.Update(ref state);
            _selectedAttrLookup.Update(ref state);
            var config = SystemAPI.GetSingleton<GarrisonSystemConfig>();
            var ecbSingleton = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>();
            var ecb = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged).AsParallelWriter();
            var job = new EnterGarrisonStateJob
            {
                ECB = ecb,
                GarrisonAttrLookup = _garrisonAttrLookup,
                GarrisonEntityLookup = _garrisonEntityLookup,
                AllowGarrisonUnitLookup = _allowGarrisonUnitLookup,
                TransformLookup = _localTransformLookup,
                OocTagLookup = _oocTagLookup,
                ConstructingTagLookup = _constructingTagLookup,
                SelectedLookup = _selectedAttrLookup,
                Config = config
            }.ScheduleParallel(state.Dependency);
            state.Dependency = job;
            new InGarrisonStateJob
            {
                ECB = ecb,
                BuildingAttrLookup = _buildingAttrLookup,
                GarrisonAttrLookup = _garrisonAttrLookup,
                GarrisonStateTagLookup = _garrisonStateTagLookup,
                GeneralAttrLookup = _generalAttrLookup,
                InsightTargetLookup = _insightTargetLookup,
                TransformLookup = _localTransformLookup,
                OocTagLookup = _oocTagLookup,
                Config = config
            }.ScheduleParallel();
        }

        [BurstCompile]
        [WithNone(typeof(UnitDeadTag))]
        private partial struct InGarrisonStateJob : IJobEntity
        {
            public EntityCommandBuffer.ParallelWriter ECB;
            [NativeDisableParallelForRestriction] public ComponentLookup<LocalTransform> TransformLookup;
            [ReadOnly] public BufferLookup<InsightTarget> InsightTargetLookup;
            [ReadOnly] public ComponentLookup<SubGameplayGeneralAttr> GeneralAttrLookup;
            [ReadOnly] public GarrisonSystemConfig Config;
            [ReadOnly] public ComponentLookup<BuildingAttr> BuildingAttrLookup;
            [ReadOnly] public ComponentLookup<GarrisonAttr> GarrisonAttrLookup;
            [ReadOnly] public ComponentLookup<GarrisonStateTag> GarrisonStateTagLookup;
            [ReadOnly] public ComponentLookup<OocTag> OocTagLookup;

            private void Execute([ChunkIndexInQuery] int index, ref InGarrison inGarrison, ref BasicStateData stateData,
                ref MovableData movableData, ref PhysicsMass physicsMass,
                Entity selfEntity)
            {
                // Check if building is destroyed, then remove inGarrison buff and exit garrison state
                if (!BuildingAttrLookup.TryGetComponent(inGarrison.BuildingEntity, out var buildingAttr))
                {
                    // Garrison units need to get out and turn to idle, others remain
                    if (stateData.CurState == InteractState.Garrison)
                    {
                        GarrisonUtils.PosGetOut(ref inGarrison, ref TransformLookup.GetRefRW(selfEntity).ValueRW,
                            default, default, ref physicsMass,
                            Config, true);
                        stateData.TargetState = InteractState.Idle;
                        StateUtils.SwitchState(ref stateData, ECB, selfEntity, index);
                    }

                    ECB.RemoveComponent<InGarrison>(index, selfEntity);
                    
                    return;
                }

                // This state machine only apply logic to idle or garrison unit
                if (stateData.CurState != InteractState.Garrison && stateData.CurState != InteractState.Idle)
                {
                    return;
                }

                // Set position to hide these move back garrison units
                if (stateData.CurState == InteractState.Garrison && !inGarrison.InBuilding)
                {
                    GarrisonUtils.PosGetIn(ref inGarrison, ref TransformLookup.GetRefRW(selfEntity).ValueRW,
                        TransformLookup[inGarrison.BuildingEntity], ref physicsMass, Config);
                    return;
                }

                //   Generator
                if (buildingAttr.Type == BuildingType.Generators)
                {
                    if (!OocTagLookup.IsComponentEnabled(inGarrison.BuildingEntity)) return;
                    // Under attack
                    GarrisonUtils.PosGetOut(ref inGarrison, ref TransformLookup.GetRefRW(selfEntity).ValueRW,
                        TransformLookup[inGarrison.BuildingEntity], GarrisonAttrLookup[inGarrison.BuildingEntity],
                        ref physicsMass,
                        Config, false);
                    stateData.TargetState = InteractState.Idle;
                    StateUtils.SwitchState(ref stateData, ECB, selfEntity, index);
                    return;
                }

                // Fortification
                var selfSights = InsightTargetLookup[selfEntity];
                var buildingSights = InsightTargetLookup[inGarrison.BuildingEntity];
                foreach (var buildingTarget in buildingSights)
                {
                    ECB.AppendToBuffer(index, selfEntity,buildingTarget);
                }

                // No targets
                if (buildingSights.Length == 0 && selfSights.Length == 0)
                {
                    // Garrison into original building
                    if (stateData.CurState == InteractState.Idle)
                    {
                        StateUtils.GarrisonMoveBack( inGarrison, ref stateData, ref movableData,
                            TransformLookup[inGarrison.BuildingEntity].Position,
                            GeneralAttrLookup[inGarrison.BuildingEntity].BoxColliderSize,
                            Config.GarrisonRadiusSq,false,
                            selfEntity,index, ECB);
                    }
                    // Already in garrison state, do nothing
                    
                    return;
                }

                var target = InteractUtils.ChooseTarget(selfSights);
                var targetGarrison = GarrisonStateTagLookup.HasComponent(target) &&
                                     GarrisonStateTagLookup.IsComponentEnabled(target);
                var selfGarrison = GarrisonStateTagLookup.IsComponentEnabled(selfEntity);
                switch (targetGarrison, selfGarrison)
                {
                    case (true, true)
                        : // This should only happen for a cleric, because garrison unit should not detect garrison enemy
                    {
                        stateData.TargetState = InteractState.Healing;
                        stateData.TargetEntity = target;
                        stateData.Focus = false;
                        StateUtils.SwitchState(ref stateData, ECB, selfEntity, index);
                        return;
                    }
                    case (true, false):
                    {
                        // This should happen when self is healer and target is wounded
                        StateUtils.GarrisonMoveBack( inGarrison, ref stateData, ref movableData,
                            TransformLookup[inGarrison.BuildingEntity].Position,
                            GeneralAttrLookup[inGarrison.BuildingEntity].BoxColliderSize,
                            Config.GarrisonRadiusSq,false,
                            selfEntity,index, ECB);
                        return;
                    }
                    case (false, true):
                    {
                        GarrisonUtils.PosGetOut(ref inGarrison, ref TransformLookup.GetRefRW(selfEntity).ValueRW,
                            TransformLookup[inGarrison.BuildingEntity], GarrisonAttrLookup[inGarrison.BuildingEntity],
                            ref physicsMass,
                            Config, false);
                        stateData.TargetState = InteractState.Idle;
                        StateUtils.SwitchState(ref stateData, ECB, selfEntity, index);
                        return;
                    }
                    // Self outside building idle, target outside building, this should not happen because garrison state machine is dealt after idle state machine
                    case (false, false):
                    {
                        return;
                    }
                }
            }

           
        }

        [BurstCompile]
        [WithAll(typeof(GarrisonStateTag))]
        [WithNone(typeof(InGarrison))]
        [WithNone(typeof(UnitDeadTag))]
        private partial struct EnterGarrisonStateJob : IJobEntity
        {
            public EntityCommandBuffer.ParallelWriter ECB;
            [NativeDisableParallelForRestriction] public ComponentLookup<LocalTransform> TransformLookup;
            [ReadOnly] public BufferLookup<GarrisonEntity> GarrisonEntityLookup;
            [ReadOnly] public ComponentLookup<GarrisonAttr> GarrisonAttrLookup;
            [ReadOnly] public BufferLookup<AllowGarrisonUnit> AllowGarrisonUnitLookup;
            [ReadOnly] public ComponentLookup<OocTag> OocTagLookup;
            [ReadOnly] public ComponentLookup<ConstructingData> ConstructingTagLookup;
            [NativeDisableParallelForRestriction] public ComponentLookup<Selected> SelectedLookup;
            [ReadOnly] public GarrisonSystemConfig Config;


            private void Execute([ChunkIndexInQuery] int index, ref BasicStateData stateData,
                ref PhysicsMass physicsMass,
                in SubGameplayGeneralAttr subGameplayGeneralAttr,
                in UnitAttr unitAttr, in GarrisonStateTag tag, Entity selfEntity)
            {
                if (stateData.CurState != InteractState.Garrison) return;
                // Building is destroyed or not allowed to garrison, turn to idle
                if (!AllowGarrisonUnitLookup.HasBuffer(stateData.TargetEntity))
                {
                    stateData.TargetState = InteractState.Idle;
                    StateUtils.SwitchState(ref stateData, ECB, selfEntity, index);
                    return;
                }

                // If target building is under attack or constructing, turn to idle
                if (
                    OocTagLookup.IsComponentEnabled(stateData.TargetEntity) ||
                    ConstructingTagLookup.HasComponent(stateData.TargetEntity))
                {
                    stateData.TargetState = InteractState.Idle;
                    StateUtils.SwitchState(ref stateData, ECB, selfEntity, index);
                    return;
                }

                // Check if target building is full
                var garrisonAttr = GarrisonAttrLookup[stateData.TargetEntity];
                var entities = GarrisonEntityLookup[stateData.TargetEntity];
                if (entities.Length >= garrisonAttr.MaxGarrisonCount)
                {
                    stateData.TargetState = InteractState.Idle;
                    StateUtils.SwitchState(ref stateData, ECB, selfEntity, index);
                    return;
                }

                // Check if this unit is allowed to garrison into this building
                var allowGarrisonUnit = AllowGarrisonUnitLookup[stateData.TargetEntity];
                int i;
                for (i = 0; i < allowGarrisonUnit.Length; i++)
                {
                    if (allowGarrisonUnit[i].UnitType == unitAttr.Type)
                    {
                        if (allowGarrisonUnit[i].SubTypeIndex == -1 // -1 means all is ok
                            || allowGarrisonUnit[i].SubTypeIndex == unitAttr.SubTypeIndex)
                            break;
                    }
                }

                // This unit is not allowed to garrison into this building, make it idle
                if (i == allowGarrisonUnit.Length)
                {
                    stateData.TargetState = InteractState.Idle;
                    StateUtils.SwitchState(ref stateData, ECB, selfEntity, index);
                    return;
                }

                // Garrison this unit into building
                // hide it to specific position
                // Add InGarrison Bonus and logic
                // Disable selected tag
                var inGarrison = new InGarrison
                {
                    BuildingEntity = stateData.TargetEntity,
                    InBuilding = true
                };
                ref var selfTransform = ref TransformLookup.GetRefRW(selfEntity).ValueRW;
                GarrisonUtils.PosGetIn(ref inGarrison, ref selfTransform,
                    TransformLookup[inGarrison.BuildingEntity], ref physicsMass, Config);

                ECB.AddComponent(index, selfEntity, inGarrison);
                
                SelectedLookup.SetComponentEnabled(selfEntity, false);
                var killSelectedVfx = ECB.CreateEntity(index);
                ECB.AddComponent<SubGameplayEntityTag>(index, killSelectedVfx);
                ECB.AddComponent(index, killSelectedVfx, new VFXRequest
                {
                    VFXName = VFXName.UnitSelectionIndicator,
                    RequestType = VFXRequestType.Kill,
                    VFXTrackTarget = selfEntity,
                });
                
                var request = ECB.CreateEntity(index);
                ECB.AddComponent(index, request, new GarrisonInBuildingRequest
                {
                    BuildingEntity = stateData.TargetEntity,
                    Id = subGameplayGeneralAttr.ID,
                    UnitType = unitAttr.Type,
                    UnitEntity = selfEntity
                });
                ECB.AddComponent<SubGameplayEntityTag>(index,request);

                stateData.TargetEntity = Entity.Null;
            }
        }
    }
}