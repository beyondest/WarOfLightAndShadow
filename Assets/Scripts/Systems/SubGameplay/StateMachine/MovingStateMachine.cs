using SparFlame.Components.General;
using SparFlame.Components.MainGameplay;
using SparFlame.Components.SubGameplay;
using SparFlame.Systems.SubGameplay.Interact;
using SparFlame.Systems.SubGameplay.Movement;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

// ReSharper disable ReplaceWithSingleAssignment.False

namespace SparFlame.Systems.SubGameplay.StateMachine
{
    [BurstCompile]
    [UpdateAfter(typeof(MovementSystem))]
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

            
            _localTransformLookup = state.GetComponentLookup<LocalTransform>();
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
            }.ScheduleParallel();
        }


        [BurstCompile]
        [WithAll(typeof(MovingStateTag))]
        [WithNone(typeof(UnitDeadTag))]
        public partial struct CheckMovingState : IJobEntity
        {
            public EntityCommandBuffer.ParallelWriter ECB;
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


            // Resolve self stuck will modify transform and lookup random transform for squeeze direction
            [NativeDisableParallelForRestriction] public ComponentLookup<LocalTransform> TransLookup;

            // Change self moving state when taunted or complete and lookup random movable data for collider size
            [NativeDisableParallelForRestriction] public ComponentLookup<MovableData> MovableLookup;

            // Change self basic state and lookup random basic state for judging
            [NativeDisableParallelForRestriction] public ComponentLookup<BasicStateData> StateLookup;

            // [ReadOnly] public float DeltaTime;

            [ReadOnly] public MovingStateMachineConfig Config;
            [ReadOnly] public GarrisonSystemConfig GarrisonSystemConfig;
            [ReadOnly] public SightSystemConfig SightSystemConfig;

            private void Execute([ChunkIndexInQuery] int index, ref Surroundings surroundings,
                ref DynamicBuffer<InsightTarget> targets,
                Entity selfEntity)
            {
                ref var stateData = ref StateLookup.GetRefRW(selfEntity).ValueRW;
                ref var movableData = ref MovableLookup.GetRefRW(selfEntity).ValueRW;
                ref var transform = ref TransLookup.GetRefRW(selfEntity).ValueRW;
                var selfFaction = GeneralLookup[selfEntity].Faction;
                // This should check in every state machine, because switch state tag only happens in next frame dur to ecb playback
                if (stateData.CurState != InteractState.Moving) return;

                // if (CheckTaunted(ref surroundings, ref movableData, ref stateData,
                //         ref targets, selfEntity, index))
                //     return;
                // Check if reached the last waypoint
                if (CheckIfCompleteMoving(ref surroundings, ref movableData, ref stateData, selfEntity, index))
                    return;

                // Not complete the moving. If stuck, should try resolve stuck first. If not stuck or stuck resolved , return true
                var ifResolveStuckByChangeState = TryResolveStuck(ref surroundings, ref targets, in movableData,
                    ref stateData,
                    ref transform,
                    selfEntity, index);
                if (ifResolveStuckByChangeState) return;

                // Not complete the moving. May change target if some other things happen
                if (CheckShouldChangeAndIfChangeTarget(ref stateData, ref movableData,
                        ref targets, in selfFaction, transform,
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
                if(DarkShieldTauntedBuffLookup.TryGetComponent(selfEntity, out var tauntedBuff)
                   && DarkShieldTauntedBuffLookup.IsComponentEnabled(selfEntity)
                   && TransLookup.TryGetComponent(tauntedBuff.TauntedBy, out var targetTrans)
                   && BoxColliderSizeLookup.TryGetComponent(tauntedBuff.TauntedBy, out var boxColliderSize))
                {
                    if (stateData.TargetEntity == tauntedBuff.TauntedBy) return false;
                    // If taunted, should change target to taunted target
                    stateData.TargetEntity = tauntedBuff.TauntedBy;
                    stateData.TargetState = InteractState.Attacking;
                    MovementUtils.SetMoveTarget(ref movableData, targetTrans.Position, boxColliderSize.Box,
                        MovementCommandType.Interactive, AttackLookup[selfEntity].Range);
                    return true;
                }
                
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
                if (movableData.MovementCommandType == MovementCommandType.Interactive && stateData.TargetState != InteractState.Garrison)
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
                            holdOnUnitOutOfDefendRange = Config.MaxDisSqUnitToHoldOnPosition < math.distancesq(selfTransform.Position,
                                holdOnPosition.Position);
                        }
                        var canHeal = HealLookup.HasComponent(selfEntity);
                        var canHarvest = HarvestLookup.HasComponent(selfEntity);
                        var canAttack = AttackLookup.HasComponent(selfEntity);
                        // Normal check
                        if (InteractUtils.IsTargetValid(in targetSubGameplayGeneralAttr, in selfFactionTag, in targetStat,
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
                                BoxColliderSizeLookup[inGarrison.BuildingEntity].Box,
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
                stateData.TargetEntity = InteractUtils.ChooseTarget(in targets);
                targetSubGameplayGeneralAttr = GeneralLookup[stateData.TargetEntity];
                var targetPos = TransLookup[stateData.TargetEntity].Position;
                var targetColliderSize = BoxColliderSizeLookup[stateData.TargetEntity].Box;
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
            /// <param name="surroundings"></param>
            /// <param name="targets"></param>
            /// <param name="movableData"></param>
            /// <param name="stateData"></param>
            /// <param name="transform"></param>
            /// <param name="selfEntity"></param>
            /// <param name="index"></param>
            /// <returns></returns>
            private bool TryResolveStuck(ref Surroundings surroundings, ref DynamicBuffer<InsightTarget> targets,
                in MovableData movableData,
                ref BasicStateData stateData, ref LocalTransform transform, Entity selfEntity, int index)
            {
                if (movableData.DetailInfo == DetailInfo.CalculationNotComplete)
                {
                    // Calculation not complete and not stuck too many times, wait for calculation
                    if (surroundings.CompromiseTimes <= Config.MaxAllowedCompromiseTimesForStuck)
                        return false;
                    // Calculation not complete for too many times, consider wrong target
                    else
                    {
                        stateData.TargetState = InteractState.Idle;
                        StateUtils.SwitchState(ref stateData, ECB, selfEntity, index);
                        return true;
                        // Debug.Log("Calculation not complete, stuck for too many times");
                        // return true;
                    }
                }

                // Stuck times too much
                if (surroundings.MoveSuccess
                    || surroundings.CompromiseTimes <= Config.MaxAllowedCompromiseTimesForStuck) return false;

                var selfCanAttack = AttackLookup.HasComponent(selfEntity);
                // If stuck by enemy building, remove it
                if (GeneralLookup.TryGetComponent(surroundings.FrontEntity, out var generalAttr)
                    && generalAttr is { BaseTag: BaseTag.Buildings, Faction: FactionTag.Dark })
                {
                    // No attack ability unit cannot remove enemy building forward, and should turn to idle
                    if (!selfCanAttack)
                    {
                        stateData.TargetState = InteractState.Idle;
                        StateUtils.SwitchState(ref stateData, ECB, selfEntity, index);
                        return true;
                    }

                    InteractUtils.MemoryTarget(ref targets, stateData.TargetEntity,
                        SightSystemConfig.MemoryTargetAfterStuckByBuilding);
                    stateData.TargetEntity = surroundings.FrontEntity;
                    stateData.TargetState = InteractState.Attacking;
                    stateData.Focus = true;
                    StateUtils.SwitchState(ref stateData, ECB, selfEntity, index);
                    return true;
                }

                // Focus unit cannot auto switch target even get stuck, unless it stuck by building
                if (stateData.Focus || !selfCanAttack) return false;

                // If front is not enemy building and get stuck and left or right is enemy unit, attack it. Front cannot be enemy unit or it will get taunted
                var leftIsEnemy = surroundings.LeftEntity != Entity.Null
                                  && GeneralLookup.TryGetComponent(surroundings.LeftEntity, out var iDataLeft)
                                  && iDataLeft is { BaseTag: BaseTag.Units, Faction: FactionTag.Dark };
                var rightIsEnemy = surroundings.RightEntity != Entity.Null
                                   && GeneralLookup.TryGetComponent(surroundings.RightEntity, out var iDataRight)
                                   && iDataRight is { BaseTag: BaseTag.Units, Faction: FactionTag.Dark };
                if (leftIsEnemy || rightIsEnemy)
                {
                    stateData.TargetEntity = leftIsEnemy
                        ? surroundings.LeftEntity
                        : surroundings.RightEntity;
                    stateData.TargetState = InteractState.Attacking;
                    StateUtils.SwitchState(ref stateData, ECB, selfEntity, index);
                    return true;
                }

                return false;
            }


            private bool CheckIfCompleteMoving(ref Surroundings surroundings, ref MovableData movableData,
                ref BasicStateData stateData, Entity entity, int index)
            {
                // if (movableData.ForceCalculate)
                //     return false; // This is the first time command, do not affected by units surrounded
                // If itself moving job is completed
                if (movableData.MovementState is MovementState.MovementComplete
                    or MovementState.MovementPartialComplete)
                {
                    MovementUtils.ResetMovableData(ref movableData);
                    MovementUtils.ResetSurroundings(ref surroundings);
                    if (stateData.TargetState == InteractState.Idle) stateData.TargetEntity = Entity.Null;
                    StateUtils.SwitchState(ref stateData, ECB, entity, index);
                    stateData.TargetState = InteractState.Idle;
                    return true;
                }

                // Check if surrounded ally unit reached. This only work when target state is idle and surrounded unit is in
                // same selection state of this one
                var isSelected = Selected.IsComponentEnabled(entity);
                // if (isSelected) return false;
                if (CheckIfSurroundReach(ref surroundings, isSelected) && stateData.TargetState == InteractState.Idle)
                {
                    stateData.TargetState = InteractState.Idle;
                    MovementUtils.ResetMovableData(ref movableData);
                    MovementUtils.ResetSurroundings(ref surroundings);
                    StateUtils.SwitchState(ref stateData, ECB, entity, index);
                    return true;
                }

                return false;
            }

            private bool CheckIfSurroundReach(ref Surroundings surroundings, bool selected)
            {
                if (surroundings.MoveSuccess) return false;
                var result = IsObstacleSelectedAllyIdle(surroundings.FrontEntity, selected)
                             || IsObstacleSelectedAllyIdle(surroundings.LeftEntity, selected)
                             || IsObstacleSelectedAllyIdle(surroundings.RightEntity, selected);

                return result;
            }

            private bool IsObstacleSelectedAllyIdle(Entity entity, bool selected)
            {
                return
                    entity != Entity.Null
                    && GeneralLookup.TryGetComponent(entity, out var iData)
                    && Selected.HasComponent(entity)
                    && Selected.IsComponentEnabled(entity) == selected
                    && iData is { BaseTag: BaseTag.Units, Faction: FactionTag.Light }
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