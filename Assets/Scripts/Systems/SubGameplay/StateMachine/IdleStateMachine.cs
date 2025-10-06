using SparFlame.Components.General;
using SparFlame.Components.SubGameplay;
using SparFlame.Core.Utils;
using SparFlame.Systems.SubGameplay.Interact;
using SparFlame.Systems.SubGameplay.Movement.FakeCollision;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace SparFlame.Systems.SubGameplay.StateMachine
{
    // [UpdateAfter(typeof(BuffManageSystem))]
    // [UpdateAfter(typeof(SightUpdateListSystem))]
    [BurstCompile]
    public partial struct IdleStateMachine : ISystem
    {
        private ComponentLookup<SubGameplayGeneralAttr> _generalAttrLookup;
        private ComponentLookup<LocalTransform> _transformLookup;
        private ComponentLookup<Selected> _selectedLookup;
        private ComponentLookup<FakeCollisionTriggerData> _triggerLookup;
        private ComponentLookup<MovingStateTag> _movingStateLookup;
        private ComponentLookup<AutoGiveWayTag> _autoGiveWayTagLookup;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<SightSystemConfig>();
            state.RequireForUpdate<GameTimeData>();
            state.RequireForUpdate<AutoGiveWayConfig>();
            state.RequireForUpdate<EndSimulationEntityCommandBufferSystem.Singleton>();
            state.RequireForUpdate<SubGamingTag>();
            _generalAttrLookup = state.GetComponentLookup<SubGameplayGeneralAttr>(true);
            _transformLookup = state.GetComponentLookup<LocalTransform>(true);
            _selectedLookup = state.GetComponentLookup<Selected>(true);
            _triggerLookup = state.GetComponentLookup<FakeCollisionTriggerData>(true);
            _movingStateLookup = state.GetComponentLookup<MovingStateTag>(true);
            _autoGiveWayTagLookup = state.GetComponentLookup<AutoGiveWayTag>(true);
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var ecbSingleton = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>();
            _generalAttrLookup.Update(ref state);
            _transformLookup.Update(ref state);
            _selectedLookup.Update(ref state);
            _triggerLookup.Update(ref state);
            _movingStateLookup.Update(ref state);
            _autoGiveWayTagLookup.Update(ref state);
            var ecbP = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged).AsParallelWriter();
            var sightConfig = SystemAPI.GetSingleton<SightSystemConfig>();
            state.Dependency =  new UnitIdleStateJob
            {
                ECB = ecbP,
                DeltaTime = SystemAPI.GetSingleton<GameTimeData>().DeltaTime,
                GeneralAttrLookup = _generalAttrLookup,
                LocalTransformLookup = _transformLookup,
                AutoGiveWayConfig = SystemAPI.GetSingleton<AutoGiveWayConfig>(),
                SelectedLookup = _selectedLookup,
                TriggerDataLookup = _triggerLookup,
                MovingStateTagLookup = _movingStateLookup,
                AutoGiveWayTagLookup = _autoGiveWayTagLookup,
                SightConfig = sightConfig,
            }.ScheduleParallel(state.Dependency);
             state.Dependency= new BuildingIdleStateJob
            {
                ECB = ecbP,
                GeneralAttrLookup = _generalAttrLookup,
                SightConfig = sightConfig
            }.ScheduleParallel(state.Dependency);
        }


        [BurstCompile]
        [WithAll(typeof(BuildingAttr))]
        [WithAll(typeof(IdleStateTag))]
        [WithNone(typeof(ConstructingTimer))]
        public partial struct BuildingIdleStateJob : IJobEntity
        {
            public EntityCommandBuffer.ParallelWriter ECB;
            [ReadOnly] public ComponentLookup<SubGameplayGeneralAttr> GeneralAttrLookup;
            [ReadOnly] public SightSystemConfig SightConfig;

            private void Execute([ChunkIndexInQuery] int index, ref BasicStateData stateData,
                in DynamicBuffer<InsightTarget> targets,
                Entity entity)
            {
                // This should check in every state machine, because switch state tag only happens in next frame dur to ecb playback
                if (stateData.CurState != InteractState.Idle) return;

                stateData.TargetEntity = Entity.Null;
                stateData.TargetState = InteractState.Idle;
                stateData.Focus = false;

                if (!targets.IsEmpty)
                {
                    stateData.TargetEntity = InteractUtils.ChooseTarget(targets, SightConfig.MaxCompareTargetCount);
                    var targetGeneralAttr = GeneralAttrLookup[stateData.TargetEntity];
                    var selfGeneralAttr = GeneralAttrLookup[entity];

                    if (selfGeneralAttr.Faction == targetGeneralAttr.Faction)
                        stateData.TargetState = InteractState.Healing;
                    if (targetGeneralAttr.BaseTag == BaseTag.Resources)
                        stateData.TargetState = InteractState.Harvesting;
                    if (selfGeneralAttr.Faction == ~targetGeneralAttr.Faction)
                        stateData.TargetState = InteractState.Attacking;
                    StateUtils.SwitchState(ref stateData, ECB, entity, index);
                }
            }
        }

        [BurstCompile]
        [WithNone(typeof(UnitDeadTag))]
        [WithAll(typeof(IdleStateTag))]
        public partial struct UnitIdleStateJob : IJobEntity
        {
            public EntityCommandBuffer.ParallelWriter ECB;
            [ReadOnly] public float DeltaTime;
            [ReadOnly] public ComponentLookup<SubGameplayGeneralAttr> GeneralAttrLookup;
            [ReadOnly] public ComponentLookup<LocalTransform> LocalTransformLookup;
            [ReadOnly] public ComponentLookup<Selected> SelectedLookup;
            [ReadOnly] public ComponentLookup<FakeCollisionTriggerData> TriggerDataLookup;
            [ReadOnly] public AutoGiveWayConfig AutoGiveWayConfig;
            [ReadOnly] public ComponentLookup<MovingStateTag> MovingStateTagLookup;
            [ReadOnly] public ComponentLookup<AutoGiveWayTag> AutoGiveWayTagLookup;
            [ReadOnly] public SightSystemConfig SightConfig;

            private void Execute([ChunkIndexInQuery] int index, ref BasicStateData stateData,
                in DynamicBuffer<InsightTarget> targets,
                in Separation separation, ref AutoGiveWayData autoGiveWayData, in BoxColliderSize boxColliderSize,
                ref MovableData movableData, in DynamicBuffer<FakeColliderTarget> fakeColliderTargets,
                Entity selfEntity)
            {
                // This should check in every state machine, because switch state tag only happens in next frame dur to ecb playback
                if (stateData.CurState != InteractState.Idle) return;

                stateData.TargetEntity = Entity.Null;
                stateData.TargetState = InteractState.Idle;
                stateData.Focus = false;

                if (!targets.IsEmpty)
                {
                    ECB.SetComponentEnabled<AutoGiveWayTag>(index, selfEntity, false);
                    ECB.SetComponentEnabled<FormationMovingTag>(index, selfEntity, false);

                    stateData.TargetEntity = InteractUtils.ChooseTarget(targets, SightConfig.MaxCompareTargetCount);
                    var targetGeneralAttr = GeneralAttrLookup[stateData.TargetEntity];
                    var selfGeneralAttr = GeneralAttrLookup[selfEntity];

                    if (selfGeneralAttr.Faction == targetGeneralAttr.Faction)
                        stateData.TargetState = InteractState.Healing;
                    if (targetGeneralAttr.BaseTag == BaseTag.Resources)
                        stateData.TargetState = InteractState.Harvesting;
                    if (selfGeneralAttr.Faction == ~targetGeneralAttr.Faction)
                        stateData.TargetState = InteractState.Attacking;
                    StateUtils.SwitchState(ref stateData, ECB, selfEntity, index);
                    return;
                }

                if (AutoGiveWayConfig.Disable) return;
                if (SelectedLookup.HasComponent(selfEntity) && SelectedLookup.IsComponentEnabled(selfEntity))
                {
                    autoGiveWayData.State = AutoGiveWayState.None;
                    autoGiveWayData.AccumulatedTime = 0f;
                    return;
                }

                var transform = LocalTransformLookup[selfEntity];
                switch (autoGiveWayData.State)
                {
                    case AutoGiveWayState.None:
                        var l = math.lengthsq(separation.Value);
                        if (l > 0.001f && l < AutoGiveWayConfig.MaxSeparationValueNotToGiveWay)
                        {
                            var leftTargets = 0;
                            var rightTargets = 0;
                            var front = math.normalizesafe(separation.Value);
                            var left = MovementUtils.GetLeftOrRight(front, true);
                            foreach (var target in fakeColliderTargets)
                            {
                                if (!TriggerDataLookup.TryGetComponent(target.Target, out var triggerData)) continue;
                                var belongsTo = triggerData.BelongsTo;
                                // Only when surroundings have unit that is in moving state, not auto give way moving, and selected, will 
                                // it be considered as a valid target for auto give way
                                if (SelectedLookup.HasComponent(belongsTo) &&
                                    SelectedLookup.IsComponentEnabled(belongsTo)
                                    && MovingStateTagLookup.HasComponent(belongsTo)
                                    && MovingStateTagLookup.IsComponentEnabled(belongsTo)
                                    && !AutoGiveWayTagLookup.IsComponentEnabled(belongsTo))
                                {
                                    var pos = LocalTransformLookup[belongsTo].Position;
                                    var direction = pos - transform.Position;
                                    var dot = math.dot(direction, left);
                                    switch (dot)
                                    {
                                        case > 0:
                                            leftTargets++;
                                            break;
                                        case < 0:
                                            rightTargets++;
                                            break;
                                    }
                                }
                            }

                            if (leftTargets == 0 && rightTargets == 0) return;
                            autoGiveWayData.OriPosition = transform.Position;
                            autoGiveWayData.AccumulatedTime = 0f;
                            var bestDirection = leftTargets < rightTargets ? left : -left;
                            var targetPos = transform.Position +
                                            bestDirection * AutoGiveWayConfig.MoveDistanceRatioOfSelfRadius *
                                            boxColliderSize.Radius;
                            autoGiveWayData.State = AutoGiveWayState.GoTo;
                            ECB.SetComponentEnabled<AutoGiveWayTag>(index, selfEntity, true);
                            StateUtils.MarchToPosition(ref stateData, ref movableData, targetPos, ECB, index,
                                selfEntity,
                                false);
                        }

                        break;
                    case AutoGiveWayState.GoTo:
                        autoGiveWayData.AccumulatedTime += DeltaTime;
                        if (autoGiveWayData.AccumulatedTime <= AutoGiveWayConfig.WaitSeconds) break;

                        var backDirection = autoGiveWayData.OriPosition - transform.Position;
                        var backHasTarget = false;
                        foreach (var target in fakeColliderTargets)
                        {
                            if (!LocalTransformLookup.TryGetComponent(target.Target, out var targetTransform)) continue;
                            var selfToTarget = targetTransform.Position - transform.Position;
                            if (math.dot(selfToTarget, backDirection) > 0)
                            {
                                backHasTarget = true;
                                break;
                            }
                        }

                        if (AutoGiveWayConfig.CheckBackHasTarget && backHasTarget)
                        {
                            autoGiveWayData.State = AutoGiveWayState.None;
                            autoGiveWayData.AccumulatedTime = 0f;
                            ECB.SetComponentEnabled<AutoGiveWayTag>(index, selfEntity, false);
                        }
                        else
                        {
                            if (!AutoGiveWayConfig.NeverGoBack)
                            {
                                autoGiveWayData.State = AutoGiveWayState.GoBack;
                                StateUtils.MarchToPosition(ref stateData, ref movableData, autoGiveWayData.OriPosition,
                                    ECB,
                                    index, selfEntity, false);
                            }
                            else
                            {
                                autoGiveWayData.State = AutoGiveWayState.None;
                                autoGiveWayData.AccumulatedTime = 0f;
                                ECB.SetComponentEnabled<AutoGiveWayTag>(index, selfEntity, false);
                            }
                        }

                        break;
                    case AutoGiveWayState.GoBack:
                        autoGiveWayData.State = AutoGiveWayState.None;
                        autoGiveWayData.AccumulatedTime = 0f;
                        ECB.SetComponentEnabled<AutoGiveWayTag>(index, selfEntity, false);
                        break;
                    default:
                        BurstSafe.UnexpectedEnum(autoGiveWayData.State);
                        break;
                }
            }
        }
    }
}