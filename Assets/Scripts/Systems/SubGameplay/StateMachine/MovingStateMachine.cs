using SparFlame.Components.General;
using SparFlame.Components.MainGameplay;
using SparFlame.Components.SubGameplay;
using SparFlame.Systems.SubGameplay.Interact;
using SparFlame.Systems.SubGameplay.Movement;
using SparFlame.Systems.SubGameplay.Movement.FakeCollision;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

// ReSharper disable ReplaceWithSingleAssignment.False

namespace SparFlame.Systems.SubGameplay.StateMachine
{
    [BurstCompile]
    [UpdateAfter(typeof(SeekTargetSystem))]
    [UpdateAfter(typeof(SightUpdateListSystem))]
    [UpdateAfter(typeof(BuffManageSystem))]
    public partial struct MovingStateMachine : ISystem
    {
        private ComponentLookup<SubGameplayGeneralAttr> _generalAttrLookup;
        private ComponentLookup<BoxColliderSize> _boxColliderSizeLookup;
        private ComponentLookup<Selected> _selectedLookup;
        private ComponentLookup<LocalTransform> _localTransformLookup;
        private ComponentLookup<MovableData> _movableLookup;
        private ComponentLookup<BasicStateData> _unitBasicStateLookup;
        private ComponentLookup<AttackAbility> _attackabilityLookup;
        private ComponentLookup<HealAbility> _healabilityLookup;
        private ComponentLookup<HarvestAbility> _harvestabilityLookup;
        private ComponentLookup<RegeneratingTag> _regeneratingTag;
        private ComponentLookup<StatData> _statLookup;
        private ComponentLookup<AITag> _aiTagLookup;
        private ComponentLookup<InGarrison> _inGarrisonLookup;
        private ComponentLookup<DarkShieldTauntedBuff> _darkShieldTauntedBuffLookup;
        private ComponentLookup<HoldOnPosition> _holdOnPositionLookup;
        private ComponentLookup<FakeCollisionTriggerData> _fakeCollisionDataLookup;
        private ComponentLookup<FormationMovingTag> _formationMovingTagLookup;
        private ComponentLookup<AutoGiveWayTag> _autoGiveWayTagLookup;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<GarrisonSystemConfig>();
            state.RequireForUpdate<SightSystemConfig>();
            state.RequireForUpdate<EndSimulationEntityCommandBufferSystem.Singleton>();
            state.RequireForUpdate<MovingStateMachineConfig>();
            state.RequireForUpdate<SubGamingTag>();
            _generalAttrLookup = state.GetComponentLookup<SubGameplayGeneralAttr>(true);
            _boxColliderSizeLookup = state.GetComponentLookup<BoxColliderSize>(true);
            _selectedLookup = state.GetComponentLookup<Selected>(true);
            _aiTagLookup = state.GetComponentLookup<AITag>(true);
            _statLookup = state.GetComponentLookup<StatData>(true);
            _attackabilityLookup = state.GetComponentLookup<AttackAbility>(true);
            _healabilityLookup = state.GetComponentLookup<HealAbility>(true);
            _harvestabilityLookup = state.GetComponentLookup<HarvestAbility>(true);
            _regeneratingTag = state.GetComponentLookup<RegeneratingTag>(true);
            _inGarrisonLookup = state.GetComponentLookup<InGarrison>(true);
            _darkShieldTauntedBuffLookup = state.GetComponentLookup<DarkShieldTauntedBuff>(true);
            _holdOnPositionLookup = state.GetComponentLookup<HoldOnPosition>(true);
            _fakeCollisionDataLookup = state.GetComponentLookup<FakeCollisionTriggerData>(true);
            _localTransformLookup = state.GetComponentLookup<LocalTransform>(true);
            _formationMovingTagLookup = state.GetComponentLookup<FormationMovingTag>(true);
            _autoGiveWayTagLookup = state.GetComponentLookup<AutoGiveWayTag>(true);
            _movableLookup = state.GetComponentLookup<MovableData>();
            _unitBasicStateLookup = state.GetComponentLookup<BasicStateData>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var config = SystemAPI.GetSingleton<MovingStateMachineConfig>();
            var sightConfig = SystemAPI.GetSingleton<SightSystemConfig>();
            var garrisonSystemConfig = SystemAPI.GetSingleton<GarrisonSystemConfig>();
            var ecb = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>();
            _harvestabilityLookup.Update(ref state);
            _healabilityLookup.Update(ref state);
            _attackabilityLookup.Update(ref state);
            _generalAttrLookup.Update(ref state);
            _boxColliderSizeLookup.Update(ref state);
            _selectedLookup.Update(ref state);
            _localTransformLookup.Update(ref state);
            _movableLookup.Update(ref state);
            _unitBasicStateLookup.Update(ref state);
            _attackabilityLookup.Update(ref state);
            _healabilityLookup.Update(ref state);
            _harvestabilityLookup.Update(ref state);
            _statLookup.Update(ref state);
            _regeneratingTag.Update(ref state);
            _aiTagLookup.Update(ref state);
            _inGarrisonLookup.Update(ref state);
            _darkShieldTauntedBuffLookup.Update(ref state);
            _holdOnPositionLookup.Update(ref state);
            _fakeCollisionDataLookup.Update(ref state);
            _formationMovingTagLookup.Update(ref state);
            _autoGiveWayTagLookup.Update(ref state);
            // _squeezeLookup.Update(ref state);
            // _autoGiveWayLookup.Update(ref state);
            new CheckMovingState
            {
                ECB = ecb.CreateCommandBuffer(state.WorldUnmanaged).AsParallelWriter(),
                GeneralLookup = _generalAttrLookup,
                BoxColliderSizeLookup = _boxColliderSizeLookup,
                Selected = _selectedLookup,
                TransLookup = _localTransformLookup,
                MovableLookup = _movableLookup,
                StateLookup = _unitBasicStateLookup,
                AttackLookup = _attackabilityLookup,
                HealLookup = _healabilityLookup,
                HarvestLookup = _harvestabilityLookup,
                StatLookup = _statLookup,
                AITagLookup = _aiTagLookup,
                InGarrisonLookup = _inGarrisonLookup,
                Config = config,
                RegeneratingTagLookup = _regeneratingTag,
                SightSystemConfig = sightConfig,
                GarrisonSystemConfig = garrisonSystemConfig,
                DarkShieldTauntedBuffLookup = _darkShieldTauntedBuffLookup,
                HoldOnPositionLookUp = _holdOnPositionLookup,
                FakeCollisionDataLookup = _fakeCollisionDataLookup,
                FormationMovingTagLookup = _formationMovingTagLookup,
                AutoGiveWayTagLookup = _autoGiveWayTagLookup,
            }.ScheduleParallel();
        }


        [BurstCompile]
        [WithAll(typeof(MovingStateTag))]
        [WithNone(typeof(UnitDeadTag))]
        public partial struct CheckMovingState : IJobEntity
        {
            public EntityCommandBuffer.ParallelWriter ECB;

            [ReadOnly] public MovingStateMachineConfig Config;
            [ReadOnly] public GarrisonSystemConfig GarrisonSystemConfig;
            [ReadOnly] public SightSystemConfig SightSystemConfig;


            [ReadOnly] public ComponentLookup<SubGameplayGeneralAttr> GeneralLookup;
            [ReadOnly] public ComponentLookup<BoxColliderSize> BoxColliderSizeLookup;
            [ReadOnly] public ComponentLookup<Selected> Selected;
            [ReadOnly] public ComponentLookup<AttackAbility> AttackLookup;
            [ReadOnly] public ComponentLookup<HealAbility> HealLookup;
            [ReadOnly] public ComponentLookup<HarvestAbility> HarvestLookup;
            [ReadOnly] public ComponentLookup<StatData> StatLookup;
            [ReadOnly] public ComponentLookup<RegeneratingTag> RegeneratingTagLookup;
            [ReadOnly] public ComponentLookup<AITag> AITagLookup;
            [ReadOnly] public ComponentLookup<InGarrison> InGarrisonLookup;
            [ReadOnly] public ComponentLookup<DarkShieldTauntedBuff> DarkShieldTauntedBuffLookup;
            [ReadOnly] public ComponentLookup<HoldOnPosition> HoldOnPositionLookUp;
            [ReadOnly] public ComponentLookup<FormationMovingTag> FormationMovingTagLookup;
            [ReadOnly] public ComponentLookup<FakeCollisionTriggerData> FakeCollisionDataLookup;
            [ReadOnly] public ComponentLookup<LocalTransform> TransLookup;
            [ReadOnly] public ComponentLookup<AutoGiveWayTag> AutoGiveWayTagLookup;
            // Change self moving state when taunted or complete and lookup random movable data for collider size
            [NativeDisableParallelForRestriction] public ComponentLookup<MovableData> MovableLookup;

            // Change self basic state and lookup random basic state for judging
            [NativeDisableParallelForRestriction] public ComponentLookup<BasicStateData> StateLookup;


            private void Execute([ChunkIndexInQuery] int index, ref Surroundings surroundings,
                ref NavAgentComponent navAgentComponent,
                ref DynamicBuffer<InsightTarget> targets, in DynamicBuffer<FakeColliderTarget> colliderTargets,
                Entity selfEntity)
            {
                ref var stateData = ref StateLookup.GetRefRW(selfEntity).ValueRW;
                ref var movableData = ref MovableLookup.GetRefRW(selfEntity).ValueRW;
                var selfTrans = TransLookup[selfEntity];
                var selfGeneralAttr = GeneralLookup[selfEntity];
                // This should check in every state machine, because switch state tag only happens in next frame dur to ecb playback
                if (stateData.CurState != InteractState.Moving) return;

                // if (CheckTaunted(ref surroundings, ref movableData, ref stateData,
                //         ref targets, selfEntity, index))
                //     return;
                // Check if reached the last waypoint
                if (CheckIfCompleteMoving(ref surroundings, ref movableData,
                        ref navAgentComponent,
                        colliderTargets, selfGeneralAttr.Faction, ref stateData, selfEntity, index))
                    return;

                // Not complete the moving. If stuck, should try resolve stuck first. If not stuck or stuck resolved , return true
                TryResolveStuck(in selfTrans, ref surroundings, ref targets, colliderTargets,
                    movableData, selfGeneralAttr, ref stateData,
                    selfEntity, index, out var ifStuck, out var ifStuckResolvedBySwitchState);
                if (ifStuck && ifStuckResolvedBySwitchState) return;

                // Not complete the moving. May change target if some other things happen
                if (CheckShouldChangeAndIfChangeTarget(ref stateData, ref movableData,
                        ref targets, in selfGeneralAttr.Faction, selfTrans,
                        selfEntity, index)) return;

                CheckUpdateTargetPos(ref stateData, ref movableData);
            }

            private bool CheckUpdateTargetPos(ref BasicStateData stateData, ref MovableData movableData)
            {
                if (stateData.TargetEntity == Entity.Null) return false;
                var targetPos = TransLookup[stateData.TargetEntity].Position;
                movableData.TargetCenterPos = targetPos;
                return true;
            }


            private bool CheckShouldChangeAndIfChangeTarget(ref BasicStateData stateData,
                ref MovableData movableData,
                ref DynamicBuffer<InsightTarget> targets,
                in FactionTag selfFactionTag,
                in LocalTransform selfTransform,
                Entity selfEntity,
                int index
            )
            {
                var shouldChangeTarget = false;
                /*if (InGarrisonLookup.TryGetComponent(stateData.TargetEntity, out var targetInGarrison) &&
                    stateData.TargetState == InteractState.Attacking &&
                    BoxColliderSizeLookup.TryGetComponent(targetInGarrison.BuildingEntity, out var tarBox))
                {
                    stateData.TargetEntity = targetInGarrison.BuildingEntity;
                    stateData.TargetState = InteractState.Attacking;
                    var tarPos = TransLookup[stateData.TargetEntity].Position;
                    MovementUtils.SetMoveTarget(ref movableData, tarPos, tarBox.SeparationBox,
                        MovementCommandType.Interactive, AttackLookup[selfEntity].Range);
                    return true;
                }*/

                // if (DarkShieldTauntedBuffLookup.TryGetComponent(selfEntity, out var tauntedBuff)
                //     && DarkShieldTauntedBuffLookup.IsComponentEnabled(selfEntity)
                //     && TransLookup.TryGetComponent(tauntedBuff.TauntedBy, out var targetTrans)
                //     && BoxColliderSizeLookup.TryGetComponent(tauntedBuff.TauntedBy, out var boxColliderSize))
                // {
                //     if (stateData.TargetEntity == tauntedBuff.TauntedBy) return false;
                //     // If taunted, should change target to taunted target
                //     stateData.TargetEntity = tauntedBuff.TauntedBy;
                //     stateData.TargetState = InteractState.Attacking;
                //     stateData.Focus = true;
                //     MovementUtils.SetMoveTarget(ref movableData, targetTrans.Position, boxColliderSize.SeparationBox,
                //         MovementCommandType.Interactive, AttackLookup[selfEntity].Range);
                //     return true;
                // }

                var isAi = AITagLookup.HasComponent(selfEntity);

                SubGameplayGeneralAttr targetSubGameplayGeneralAttr;
                // March move check, only work for AI debug, actually AI will not march?
                if (isAi && !stateData.Focus
                         && movableData.MovementCommandType == MovementCommandType.March
                         && !targets.IsEmpty)
                {
                    shouldChangeTarget = true;
                }

                // Interactive move check
                if (movableData.MovementCommandType == MovementCommandType.Interactive &&
                    stateData.TargetState != InteractState.Garrison)
                {
                    // Check target not destroy
                    if (GeneralLookup.TryGetComponent(stateData.TargetEntity, out targetSubGameplayGeneralAttr))
                    {
                        var targetStat = StatLookup[stateData.TargetEntity];
                        // Garrison is valid if building hp > 0
                        if (stateData.TargetState == InteractState.Garrison && targetStat.curValue > 0)
                        {
                            return false;
                        }

                        var garrisonUnitOutOfDefendRange =
                            InGarrisonLookup.TryGetComponent(selfEntity, out var inGarrison)
                            && TransLookup.TryGetComponent(inGarrison.BuildingEntity,
                                out var buildingTrans)
                            && Config.MaxDisSqUnitToBuildingForGarrison <
                            math.distancesq(selfTransform.Position, buildingTrans.Position);
                        var holdOnUnitOutOfDefendRange = false;
                        if (HoldOnPositionLookUp.TryGetComponent(selfEntity, out var holdOnPosition))
                        {
                            holdOnUnitOutOfDefendRange = Config.MaxDisSqUnitToHoldOnPosition < math.distancesq(
                                selfTransform.Position,
                                holdOnPosition.Position);
                        }

                        var canHeal = HealLookup.HasComponent(selfEntity);
                        var canHarvest = HarvestLookup.HasComponent(selfEntity);
                        var canAttack = AttackLookup.HasComponent(selfEntity);
                        // Normal check
                        if (InteractUtils.IsTargetValid(in targetSubGameplayGeneralAttr, in selfFactionTag,
                                in targetStat,
                                canHeal, canHarvest, canAttack
                                ,
                                !RegeneratingTagLookup.HasComponent(stateData.TargetEntity)))
                        {
                            // Ai follow drop aggro check
                            var tarPos = TransLookup[stateData.TargetEntity].Position;
                            var targetFromSelfDisSq = math.distancesq(tarPos, selfTransform.Position);
                            var aiTagShouldNotFollow = isAi
                                                       && !stateData
                                                           .Focus // This is for the time enemy need to attack very far away building and debugging need
                                                       && Config.MaxDistanceSqFollowForAITag < targetFromSelfDisSq;
                            // AI should not follow so far target or hold on unit should not follow so far target
                            if (aiTagShouldNotFollow || holdOnUnitOutOfDefendRange)
                            {
                                int i;
                                for (i = 0; i < targets.Length; i++)
                                {
                                    var target = targets[i];
                                    if (target.Entity == stateData.TargetEntity) break;
                                }

                                if (i < targets.Length)
                                {
                                    targets.RemoveAt(i);
                                }
                            }

                            if (!aiTagShouldNotFollow && !garrisonUnitOutOfDefendRange && !holdOnUnitOutOfDefendRange)
                                return false;
                        }

                        // Garrison unit Drop aggro， if Ai, should focus go back and regenerating hp
                        if (garrisonUnitOutOfDefendRange)
                        {
                            StateUtils.GarrisonMoveBack(inGarrison, ref stateData, ref movableData,
                                TransLookup[inGarrison.BuildingEntity].Position,
                                BoxColliderSizeLookup[inGarrison.BuildingEntity].SeparationBox,
                                GarrisonSystemConfig.GarrisonRadiusSq, isAi,
                                selfEntity, index, ECB);
                            return true;
                            // if(isAi)
                            //     ECB.AddComponent<GarrisonAiHpRegeneratingTag>(index, selfEntity);
                        }

                        // Hold on unit out of defend range and no target in sight, march back to hold on position
                        if (holdOnUnitOutOfDefendRange && targets.IsEmpty)
                        {
                            StateUtils.MarchToPosition(ref stateData, ref movableData, holdOnPosition.Position,
                                ECB, index, selfEntity, false);
                            return true;
                        }
                    }

                    // Current target invalid, check if turn to idle
                    if (targets.IsEmpty)
                    {
                        MovementUtils.ResetMovableData(ref movableData);
                        stateData.TargetState = InteractState.Idle;
                        StateUtils.SwitchState(ref stateData, ECB, selfEntity, index);
                        return true;
                    }

                    shouldChangeTarget = true;
                }

                if (!shouldChangeTarget) return false;
                // Interact move to target. Because it is in moving state already, so don't need to switch state;
                // Because moving state machine Update after update target list system, choose target should always be valid
                ECB.SetComponentEnabled<FormationMovingTag>(index, selfEntity, false);
                ECB.SetComponentEnabled<AutoGiveWayTag>(index, selfEntity, false);

                stateData.TargetEntity = InteractUtils.ChooseTarget(in targets);
                targetSubGameplayGeneralAttr = GeneralLookup[stateData.TargetEntity];
                var targetPos = TransLookup[stateData.TargetEntity].Position;
                var targetColliderSize = BoxColliderSizeLookup[stateData.TargetEntity].SeparationBox;
                float range;
                if (targetSubGameplayGeneralAttr.Faction == selfFactionTag)
                {
                    stateData.TargetState = InteractState.Healing;
                    range = HealLookup[selfEntity].Range;
                }
                else if (targetSubGameplayGeneralAttr.BaseTag == BaseTag.Resources)
                {
                    stateData.TargetState = InteractState.Harvesting;
                    range = HarvestLookup[selfEntity].Range;
                }
                else
                {
                    stateData.TargetState = InteractState.Attacking;
                    range = AttackLookup[selfEntity].Range;
                }

                MovementUtils.SetMoveTarget(ref movableData, targetPos, targetColliderSize,
                    MovementCommandType.Interactive, range);
                return true;
            }


            /// <summary>
            /// Will return true when stuck is resolved by changing state
            /// </summary>
            /// <param name="selfTrans"></param>
            /// <param name="surroundings"></param>
            /// <param name="targets"></param>
            /// <param name="colliderTargets"></param>
            /// <param name="movableData"></param>
            /// <param name="selfGeneralAttr"></param>
            /// <param name="stateData"></param>
            /// <param name="selfEntity"></param>
            /// <param name="index"></param>
            /// <param name="ifStuck"></param>
            /// <param name="ifStuckResolvedBySwitchState"></param>
            /// <returns>If stuck is resolved or not</returns>
            private void TryResolveStuck(in LocalTransform selfTrans, ref Surroundings surroundings,
                ref DynamicBuffer<InsightTarget> targets,
                in DynamicBuffer<FakeColliderTarget> colliderTargets,
                in MovableData movableData, in SubGameplayGeneralAttr selfGeneralAttr,
                ref BasicStateData stateData, Entity selfEntity, int index,
                out bool ifStuck, out bool ifStuckResolvedBySwitchState)
            {
                ifStuck = false;
                ifStuckResolvedBySwitchState = false;
                if (movableData.DetailInfo == DetailInfo.CalculationNotComplete)
                {
                    // Calculation not complete and not stuck too many times, wait for calculation
                    if (surroundings.CompromiseTimes <= Config.MaxAllowedCompromiseTimesForStuck)
                        return;
                    // Calculation not complete for too many times, consider wrong target
                    stateData.TargetState = InteractState.Idle;
                    StateUtils.SwitchState(ref stateData, ECB, selfEntity, index);
                    var hintRequest = ECB.CreateEntity(index);
                    ECB.AddComponent<SubGameplayEntityTag>(index, hintRequest);
                    ECB.AddComponent(index, hintRequest, new HintRequest
                    {
                        Name = HintName.TargetNotReachable
                    });
                    return;
                }

                // Stuck times too much
                if (surroundings.MoveSuccess
                    || surroundings.CompromiseTimes <= Config.MaxAllowedCompromiseTimesForStuck) return;
                var selfCanAttack = AttackLookup.HasComponent(selfEntity);

                ifStuck = true;
                if (stateData.Focus || !selfCanAttack)
                {
                    ifStuckResolvedBySwitchState = false; // Let avoidance system to handle stuck
                    return;
                }

                var closestTarget = Entity.Null;
                var minDisSq = float.MaxValue;

                foreach (var target in colliderTargets)
                {
                    if (!FakeCollisionDataLookup.TryGetComponent(target.Target, out var data)
                        || !GeneralLookup.TryGetComponent(data.BelongsTo, out var generalAttr))
                        continue;
                    var relationShip =
                        FactionUtils.GetRelationshipSimple(selfGeneralAttr.Faction, generalAttr.Faction);
                    if (relationShip == Relationship.Hostile)
                    {
                        var position = TransLookup[data.BelongsTo].Position;
                        var disSq = math.distancesq(selfTrans.Position, position);
                        if (disSq < minDisSq)
                        {
                            minDisSq = disSq;
                            closestTarget = data.BelongsTo;
                        }
                    }
                }

                if (closestTarget != Entity.Null)
                {
                    ifStuckResolvedBySwitchState = true;
                    // InteractUtils.MemoryTarget(ref targets, stateData.TargetEntity,
                    //     SightSystemConfig.MemoryTargetAfterStuckByBuilding);
                    stateData.TargetEntity = closestTarget;
                    stateData.TargetState = InteractState.Attacking;
                    stateData.Focus = true;
                    StateUtils.SwitchState(ref stateData, ECB, selfEntity, index);
                    MovementUtils.ResetSurroundings(ref surroundings);
                    return;
                }

                ifStuckResolvedBySwitchState = false;
            }


            private bool CheckIfCompleteMoving(ref Surroundings surroundings, ref MovableData movableData,
                ref NavAgentComponent navAgentComponent,
                in DynamicBuffer<FakeColliderTarget> colliderTargets,
                in FactionTag selfFaction,
                ref BasicStateData stateData, Entity entity, int index)
            {
                // if (movableData.ForceCalculate)
                //     return false; // This is the first time command, do not affected by units surrounded
                // If itself moving job is completed
                if (movableData.MovementState is MovementState.MovementComplete
                    or MovementState.MovementPartialComplete)
                {
                    ECB.SetComponentEnabled<FormationMovingTag>(index, entity, false);
                    MovementUtils.ResetMovableData(ref movableData);
                    MovementUtils.ResetSurroundings(ref surroundings);
                    ECB.SetBuffer<WaypointBuffer>(index, entity);
                    MovementUtils.ResetNavAgent(ref navAgentComponent);
                    if (stateData.TargetState == InteractState.Idle) stateData.TargetEntity = Entity.Null;
                    StateUtils.SwitchState(ref stateData, ECB, entity, index);
                    stateData.TargetState = InteractState.Idle;
                    return true;
                }
                // Auto give way unit does not check surroundings reach
                if (AutoGiveWayTagLookup.IsComponentEnabled(entity)) return false;
                // Check if surrounded ally unit reached. This only work when target state is idle and surrounded unit is in
                // same selection state of this one
                var isSelected = Selected.IsComponentEnabled(entity);
                // if (isSelected) return false;
                if (AITagLookup.HasComponent(entity)) return false;
                if (!FormationMovingTagLookup.IsComponentEnabled(entity) &&
                    CheckIfSurroundReach(ref surroundings, colliderTargets, isSelected, selfFaction) &&
                    stateData.TargetState == InteractState.Idle)
                {
                    stateData.TargetState = InteractState.Idle;
                    MovementUtils.ResetMovableData(ref movableData);
                    ECB.SetBuffer<WaypointBuffer>(index, entity);
                    MovementUtils.ResetNavAgent(ref navAgentComponent);
                    MovementUtils.ResetSurroundings(ref surroundings);
                    StateUtils.SwitchState(ref stateData, ECB, entity, index);
                    return true;
                }

                return false;
            }

            private bool CheckIfSurroundReach(ref Surroundings surroundings,
                in DynamicBuffer<FakeColliderTarget> targets, bool selected,
                FactionTag selfFaction)
            {
                // if (surroundings.MoveSuccess) return false;
                foreach (var target in targets)
                {
                    if (!FakeCollisionDataLookup.TryGetComponent(target.Target, out var data)) continue;
                    var trulyTarget = data.BelongsTo;
                    if (IsObstacleSelectedAllyIdle(trulyTarget, selected, selfFaction)) return true;
                }

                return false;
                // var result = IsObstacleSelectedAllyIdle(surroundings.FrontEntity, selected, selfFaction)
                //              || IsObstacleSelectedAllyIdle(surroundings.LeftEntity, selected, selfFaction)
                //              || IsObstacleSelectedAllyIdle(surroundings.RightEntity, selected, selfFaction);
                //
                // return result;
            }

            private bool IsObstacleSelectedAllyIdle(Entity entity, bool selected, FactionTag selfFaction)
            {
                return
                    entity != Entity.Null
                    && GeneralLookup.TryGetComponent(entity, out var iData)
                    && Selected.HasComponent(entity)
                    && Selected.IsComponentEnabled(entity) == selected
                    && iData.BaseTag == BaseTag.Units && iData.Faction == selfFaction
                    && StateLookup.TryGetComponent(entity, out var stateData)
                    && stateData.CurState == InteractState.Idle;
            }


            // // Use surrounding information to try to find another way
            // if (surroundings.CompromiseTimes > Config.MaxAllowedCompromiseTimesForStuck &&
            //     surroundings.CompromiseTimes < Config.MaxAllowedCompromiseTimesForAnotherWay)
            // {
            //     if (surroundings.LeftEntity == Entity.Null || surroundings.RightEntity == Entity.Null)
            //     {
            //         // var isLeft = surroundings.LeftEntity == Entity.Null;
            //         // var isRight = surroundings.RightEntity == Entity.Null;
            //         var realFront = math.mul(transform.Rotation, new float3(0, 0, -1));
            //         // var dir = MovementUtils.GetLeftOrRight(realFront, !surroundings.ChooseRight ? isLeft : !isRight);
            //         var dir = MovementUtils.GetLeftOrRight(realFront, surroundings.LeftEntity == Entity.Null);
            //         transform.Position += dir * movableData.MoveSpeed * DeltaTime * Config.AdjustRatio;
            //         // transform.Position -= realFront * movableData.MoveSpeed * DeltaTime * 0.5f;
            //         // switch (surroundings.ChooseRight)
            //         // {
            //         //     case true when !isRight:
            //         //     case false when !isLeft:
            //         //         surroundings.SlideTimes += 1;
            //         //         break;
            //         // }
            //         //
            //         // if (surroundings.SlideTimes > ChooseSideTimes)
            //         // {
            //         //     surroundings.ChooseRight = !surroundings.ChooseRight;
            //         //     surroundings.SlideTimes = 0;
            //         // }
            //     }
            //     return true;
            // }


            /*Deprecated : TrySqueeze
             private bool TrySqueeze(ref Surroundings surroundings, in LocalTransform transform, int index)
            {
                if (InteractLookUp.TryGetComponent(surroundings.FrontEntity, out var iData)
                    && iData is { BaseTag: BaseTag.Units, FactionTag: FactionTag.Ally }
                    && !SqueezeLookup.HasComponent(surroundings.FrontEntity)
                   )
                {
                    var frontPos = TransLookup.GetRefRO(surroundings.FrontEntity);
                    var moveVector = MovementUtils.GetLeftOrRight(surroundings.IdealDirection,
                        MovementUtils.GetSide(transform.Position, frontPos.ValueRO.Position,
                            surroundings.IdealDirection));
                    var frontColliderShapeXz =
                        MovableLookup.GetRefRO(surroundings.FrontEntity).ValueRO.SelfColliderShapeXz;
                    ECB.AddComponent(index, surroundings.FrontEntity, new SqueezeData
                    {
                        MoveVector = moveVector * math.max(frontColliderShapeXz.x, frontColliderShapeXz.y) *
                                     SqueezeRatio,
                    });
                    return true;
                }

                return false;
            }
            */

            /*Deprecated : TryTellAutoGiveWay
 private bool TryTellAutoGiveWay(ref Surroundings surroundings, in LocalTransform transform,
    Entity entity, int index)
{
    // Only selected units can let others give way
    if (!Selected.IsComponentEnabled(entity)) return false;

    // Only not selected ally unit can auto give way
    if (!InteractLookUp.TryGetComponent(surroundings.FrontEntity, out var iData)
        || iData is not { BaseTag: BaseTag.Units, FactionTag: FactionTag.Ally }
        || Selected.IsComponentEnabled(surroundings.FrontEntity)
        || !StateLookup.TryGetComponent(surroundings.FrontEntity, out var stateData)
        || stateData.CurState != InteractState.Idle)
        return false;

    // Only let one unit give way once, until it finishes auto give way.
    if (AutoGiveWayLookup.HasComponent(surroundings.FrontEntity))
        return true;

    var frontPos = TransLookup.GetRefRO(surroundings.FrontEntity);
    var moveVector = MovementUtils.GetLeftOrRight(surroundings.IdealDirection,
        MovementUtils.GetSide(transform.Position, frontPos.ValueRO.Position, surroundings.IdealDirection));
    var frontColliderShapeXz = MovableLookup.GetRefRO(surroundings.FrontEntity).ValueRO.SelfColliderShapeXz;

    ECB.AddComponent(index, surroundings.FrontEntity, new AutoGiveWayData
    {
        ElapsedTime = 0f,
        MoveVector = moveVector * math.max(frontColliderShapeXz.x, frontColliderShapeXz.y) * 2,
        IfGoBack = false
    });
    return true;
}*/
        }
    }
}


/*Deprecated : Try to tell auto give way or squeeze
              if (TryTellAutoGiveWay(ref surroundings, in transform, entity, index))
                  return true;
              if (TrySqueeze(ref surroundings, in transform, index))
                  return true;
              if (surroundings.CompromiseTimes < MaxAllowedCompromiseTimesForSqueeze) return true;
              // If Squeeze failed, try to find another way
              if (surroundings.CompromiseTimes < 2 * MaxAllowedCompromiseTimesForStuck &&
                  (surroundings.LeftEntity == Entity.Null
                   ||
                   surroundings.RightEntity == Entity.Null))
              {
                  var isLeft = surroundings.LeftEntity == Entity.Null;
                  var isRight = surroundings.RightEntity == Entity.Null;
                  var realFront = math.mul(transform.Rotation, new float3(0, 0, -1));
                  var dir = MovementUtils.GetLeftOrRight(realFront, !surroundings.ChooseRight ? isLeft : !isRight);
                  transform.Position +=
                      dir * math.max(movableData.SelfColliderShapeXz.x, movableData.SelfColliderShapeXz.y) * 0.2f;
                  switch (surroundings.ChooseRight)
                  {
                      case true when !isRight:
                      case false when !isLeft:
                          surroundings.SlideTimes += 1;
                          break;
                  }

                  if (surroundings.SlideTimes > ChooseSideTimes)
                  {
                      surroundings.ChooseRight = !surroundings.ChooseRight;
                      surroundings.SlideTimes = 0;
                  }

                  return true;
              }*/