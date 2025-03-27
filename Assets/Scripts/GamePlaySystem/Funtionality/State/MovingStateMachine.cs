using SparFlame.GamePlaySystem.General;
using SparFlame.GamePlaySystem.Interact;
using SparFlame.GamePlaySystem.Movement;
using SparFlame.GamePlaySystem.UnitSelection;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Transforms;

// ReSharper disable ReplaceWithSingleAssignment.False

namespace SparFlame.GamePlaySystem.State
{
    [BurstCompile]
    [UpdateAfter(typeof(SightUpdateListSystem))]
    [UpdateAfter(typeof(BuffSystem))]
    // [UpdateBefore(typeof(AutoGiveWaySystem))]
    [UpdateBefore(typeof(StatSystem))]
    public partial struct MovingStateMachine : ISystem
    {
        private ComponentLookup<InteractableAttr> _interactableLookup;
        private ComponentLookup<Selected> _selectedLookup;
        private ComponentLookup<LocalTransform> _localTransformLookup;
        private ComponentLookup<MovableData> _movableLookup;
        private ComponentLookup<BasicStateData> _unitBasicStateLookup;
        private ComponentLookup<AttackAbility> _attackabilityLookup;
        private ComponentLookup<HealAbility> _healabilityLookup;
        private ComponentLookup<HarvestAbility> _harvestabilityLookup;

        private ComponentLookup<StatData> _statLookup;
        // private ComponentLookup<AutoGiveWayData> _autoGiveWayLookup;
        // private ComponentLookup<SqueezeData> _squeezeLookup;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<SightSystemConfig>();
            state.RequireForUpdate<EndSimulationEntityCommandBufferSystem.Singleton>();
            state.RequireForUpdate<MovingStateMachineConfig>();
            state.RequireForUpdate<NotPauseTag>();
            _interactableLookup = state.GetComponentLookup<InteractableAttr>(true);
            _selectedLookup = state.GetComponentLookup<Selected>(true);

            _localTransformLookup = state.GetComponentLookup<LocalTransform>();
            _movableLookup = state.GetComponentLookup<MovableData>();
            _unitBasicStateLookup = state.GetComponentLookup<BasicStateData>();
            _attackabilityLookup = state.GetComponentLookup<AttackAbility>();
            _healabilityLookup = state.GetComponentLookup<HealAbility>();
            _harvestabilityLookup = state.GetComponentLookup<HarvestAbility>();
            _statLookup = state.GetComponentLookup<StatData>();
            // _autoGiveWayLookup = state.GetComponentLookup<AutoGiveWayData>(true);
            // _squeezeLookup = state.GetComponentLookup<SqueezeData>(true);
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var config = SystemAPI.GetSingleton<MovingStateMachineConfig>();
            var sightConfig = SystemAPI.GetSingleton<SightSystemConfig>();
            var ecb = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>();
            _harvestabilityLookup.Update(ref state);
            _healabilityLookup.Update(ref state);
            _attackabilityLookup.Update(ref state);
            _interactableLookup.Update(ref state);
            _selectedLookup.Update(ref state);
            _localTransformLookup.Update(ref state);
            _movableLookup.Update(ref state);
            _unitBasicStateLookup.Update(ref state);
            _attackabilityLookup.Update(ref state);
            _healabilityLookup.Update(ref state);
            _harvestabilityLookup.Update(ref state);
            _statLookup.Update(ref state);
            // _squeezeLookup.Update(ref state);
            // _autoGiveWayLookup.Update(ref state);
            new CheckMovingState
            {
                ECB = ecb.CreateCommandBuffer(state.WorldUnmanaged).AsParallelWriter(),
                InteractLookUp = _interactableLookup,
                Selected = _selectedLookup,
                TransLookup = _localTransformLookup,
                MovableLookup = _movableLookup,
                StateLookup = _unitBasicStateLookup,
                AttackLookup = _attackabilityLookup,
                HealLookup = _healabilityLookup,
                HarvestLookup = _harvestabilityLookup,
                StatLookup = _statLookup,
                // DeltaTime = SystemAPI.Time.DeltaTime,
                Config = config,
                SightSystemConfig = sightConfig
                // AutoGiveWayLookup = _autoGiveWayLookup,
                // SqueezeLookup = _squeezeLookup,
                // ChooseSideTimes = config.ChooseSideTimes,
                // MaxAllowedCompromiseTimesForSqueeze = config.MaxAllowedCompromiseTimesForSqueeze,
                // SqueezeRatio = config.SqueezeRatio,
            }.ScheduleParallel();
        }


        [BurstCompile]
        [WithAll(typeof(MovingStateTag))]
        public partial struct CheckMovingState : IJobEntity
        {
            public EntityCommandBuffer.ParallelWriter ECB;
            [ReadOnly] public ComponentLookup<InteractableAttr> InteractLookUp;

            [ReadOnly] public ComponentLookup<Selected> Selected;

            // [ReadOnly] public ComponentLookup<AutoGiveWayData> AutoGiveWayLookup;
            // [ReadOnly] public ComponentLookup<SqueezeData> SqueezeLookup;
            [ReadOnly] public ComponentLookup<AttackAbility> AttackLookup;
            [ReadOnly] public ComponentLookup<HealAbility> HealLookup;
            [ReadOnly] public ComponentLookup<HarvestAbility> HarvestLookup;
            [ReadOnly] public ComponentLookup<StatData> StatLookup;

            // Resolve self stuck will modify transform and lookup random transform for squeeze direction
            [NativeDisableParallelForRestriction] public ComponentLookup<LocalTransform> TransLookup;

            // Change self moving state when taunted or complete and lookup random movable data for collider size
            [NativeDisableParallelForRestriction] public ComponentLookup<MovableData> MovableLookup;

            // Change self basic state and lookup random basic state for judging
            [NativeDisableParallelForRestriction] public ComponentLookup<BasicStateData> StateLookup;

            // [ReadOnly] public float DeltaTime;

            [ReadOnly] public MovingStateMachineConfig Config;
            [ReadOnly] public SightSystemConfig SightSystemConfig;
            // [ReadOnly] public int MaxAllowedCompromiseTimesForSqueeze;
            // [ReadOnly] public int ChooseSideTimes;
            // [ReadOnly] public float SqueezeRatio;

            private void Execute([ChunkIndexInQuery] int index, ref Surroundings surroundings,
                ref DynamicBuffer<InsightTarget> targets,
                Entity entity)
            {
                ref var stateData = ref StateLookup.GetRefRW(entity).ValueRW;
                ref var movableData = ref MovableLookup.GetRefRW(entity).ValueRW;
                ref var transform = ref TransLookup.GetRefRW(entity).ValueRW;
                var selfFaction = InteractLookUp[entity].FactionTag;
                // This should check in every state machine, because switch state tag only happens in next frame dur to ecb playback
                if (stateData.CurState != UnitState.Moving) return;

                // Debug.Log($"stateData : {stateData.TargetEntity}");
                if (CheckTaunted(ref surroundings, ref movableData, ref stateData,
                        ref targets, entity, index))
                    return;
                // Check if reached the last waypoint
                if (CheckIfCompleteMoving(ref surroundings, ref movableData, ref stateData, entity, index))
                    return;

                // Not complete the moving. If stuck, should try resolve stuck first. If not stuck or stuck resolved , return true
                var ifStuckResolved = TryResolveStuck(ref surroundings, ref targets, in movableData, ref stateData,
                    ref transform,
                    entity, index);


                // Not complete the moving. May change target if stuck is not resolved or some other things happen
                CheckShouldChangeTarget(ref stateData, ref movableData,
                    in targets, in ifStuckResolved, in selfFaction,
                    entity, index);
            }


            private void CheckShouldChangeTarget(ref BasicStateData stateData,
                ref MovableData movableData,
                in DynamicBuffer<InsightTarget> targets,
                in bool ifStuckResolved,
                in FactionTag selfFactionTag,
                Entity entity,
                int index
            )
            {
                var shouldChangeTarget = false;
                InteractableAttr targetInteractAttr;
                // Not focus march, see target, should choose target
                if (
                    !stateData.Focus
                    && movableData.MovementCommandType == MovementCommandType.March
                    && !targets.IsEmpty)
                {
                    shouldChangeTarget = true;
                }

                // Interactive move, check current target valid
                if (movableData.MovementCommandType == MovementCommandType.Interactive)
                {
                    // If current target valid, do nothing
                    if (InteractLookUp.TryGetComponent(stateData.TargetEntity, out targetInteractAttr))
                    {
                        var targetStat = StatLookup[stateData.TargetEntity];
                        if (InteractUtils.IsTargetValid(in targetInteractAttr, in selfFactionTag, in targetStat,
                                HealLookup.HasComponent(entity), HarvestLookup.HasComponent(entity)))
                        {
                            return;
                        }
                    }

                    // Current target invalid, check if turn to idle
                    if (targets.IsEmpty)
                    {
                        MovementUtils.ResetMovableData(ref movableData);
                        stateData.TargetEntity = Entity.Null;
                        stateData.TargetState = UnitState.Idle;
                        StateUtils.SwitchState(ref stateData, ECB, entity, index);
                        return;
                    }

                    shouldChangeTarget = true;
                }

                if (!shouldChangeTarget) return;

                // Interact move to target. Because it is in moving state already, so don't need to switch state;
                // Because moving state machine Update after update target list system, choose target should always be valid
                stateData.TargetEntity = InteractUtils.ChooseTarget(in targets);
                targetInteractAttr = InteractLookUp[stateData.TargetEntity];
                var targetPos = TransLookup[stateData.TargetEntity].Position;
                var targetColliderSize = targetInteractAttr.BoxColliderSize;
                float rangSq;
                if (targetInteractAttr.FactionTag == selfFactionTag)
                {
                    stateData.TargetState = UnitState.Healing;
                    rangSq = HealLookup[entity].RangeSq;
                }
                else if (targetInteractAttr.BaseTag == BaseTag.Resources)
                {
                    stateData.TargetState = UnitState.Harvesting;
                    rangSq = HarvestLookup[entity].RangeSq;
                }
                else
                {
                    stateData.TargetState = UnitState.Attacking;
                    rangSq = AttackLookup[entity].RangeSq;
                }

                MovementUtils.SetMoveTarget(ref movableData, targetPos, targetColliderSize,
                    MovementCommandType.Interactive, rangSq);
            }


            private bool TryResolveStuck(ref Surroundings surroundings, ref DynamicBuffer<InsightTarget> targets,
                in MovableData movableData,
                ref BasicStateData stateData, ref LocalTransform transform, Entity entity, int index)
            {
                // Calculation not complete, can do nothing now
                if (movableData.DetailInfo == DetailInfo.CalculationNotComplete)
                    return false;

                // Stuck times too much
                if (surroundings.MoveSuccess
                    || surroundings.CompromiseTimes <= Config.MaxAllowedCompromiseTimesForStuck) return false;


                // If stuck by enemy building, remove it
                if (InteractLookUp.TryGetComponent(surroundings.FrontEntity, out var iDataFront)
                    && iDataFront is { BaseTag: BaseTag.Buildings, FactionTag: FactionTag.Enemy })
                {
                    InteractUtils.MemoryTarget(ref targets, stateData.TargetEntity,
                        SightSystemConfig.MemoryTargetAfterStuckByBuilding);
                    stateData.TargetEntity = surroundings.FrontEntity;
                    stateData.TargetState = UnitState.Attacking;
                    stateData.Focus = true;
                    StateUtils.SwitchState(ref stateData, ECB, entity, index);
                    return true;
                }

                // Focus unit cannot auto switch target even get stuck, unless it stuck by building
                if (stateData.Focus) return false;

                // If front is not enemy building and get stuck and left or right is enemy unit, attack it. Front cannot be enemy unit or it will get taunted
                var leftIsEnemy = surroundings.LeftEntity != Entity.Null
                                  && InteractLookUp.TryGetComponent(surroundings.LeftEntity, out var iDataLeft)
                                  && iDataLeft is { BaseTag: BaseTag.Units, FactionTag: FactionTag.Enemy };
                var rightIsEnemy = surroundings.RightEntity != Entity.Null
                                   && InteractLookUp.TryGetComponent(surroundings.RightEntity, out var iDataRight)
                                   && iDataRight is { BaseTag: BaseTag.Units, FactionTag: FactionTag.Enemy };
                if (leftIsEnemy || rightIsEnemy)
                {
                    stateData.TargetEntity = leftIsEnemy
                        ? surroundings.LeftEntity
                        : surroundings.RightEntity;
                    stateData.TargetState = UnitState.Attacking;
                    StateUtils.SwitchState(ref stateData, ECB, entity, index);
                    return true;
                }

                return false;
            }


            private bool CheckIfCompleteMoving(ref Surroundings surroundings, ref MovableData movableData,
                ref BasicStateData stateData, Entity entity, int index)
            {
                // If itself moving job is completed
                if (movableData.MovementState is MovementState.MovementComplete
                    or MovementState.MovementPartialComplete)
                {
                    MovementUtils.ResetMovableData(ref movableData);
                    MovementUtils.ResetSurroundings(ref surroundings);
                    if (stateData.TargetState == UnitState.Idle) stateData.TargetEntity = Entity.Null;
                    StateUtils.SwitchState(ref stateData, ECB, entity, index);
                    return true;
                }

                // Check if surrounded ally unit reached. 
                var isSelected = Selected.IsComponentEnabled(entity);
                if (CheckIfSurroundReach(ref surroundings, isSelected))
                {
                    stateData.TargetState = UnitState.Idle;
                    stateData.TargetEntity = Entity.Null;
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
                return IsObstacleSelectedAllyIdle(surroundings.FrontEntity, selected)
                       || IsObstacleSelectedAllyIdle(surroundings.LeftEntity, selected)
                       || IsObstacleSelectedAllyIdle(surroundings.RightEntity, selected);
            }

            private bool IsObstacleSelectedAllyIdle(Entity entity, bool selected)
            {
                return
                    entity != Entity.Null
                    && InteractLookUp.TryGetComponent(entity, out var iData)
                    && Selected.HasComponent(entity)
                    && Selected.IsComponentEnabled(entity) == selected
                    && iData is { BaseTag: BaseTag.Units, FactionTag: FactionTag.Ally }
                    && StateLookup.TryGetComponent(entity, out var stateData)
                    && stateData.CurState == UnitState.Idle;
            }

            private bool CheckTaunted(ref Surroundings surroundings, ref MovableData movableData,
                ref BasicStateData stateData, ref DynamicBuffer<InsightTarget> targets,Entity entity, int index)
            {
                if (surroundings.MoveSuccess) return false;
                if (!InteractLookUp.TryGetComponent(surroundings.FrontEntity, out var iData)) return false;
                if (iData is not { FactionTag: FactionTag.Enemy, BaseTag: BaseTag.Units }) return false;
                if (stateData.Focus)
                {
                    InteractUtils.MemoryTarget(ref targets, stateData.TargetEntity,
                        SightSystemConfig.MemoryTargetWhenFocus);
                }
                // Turn to Attacking State
                stateData.TargetState = UnitState.Attacking;
                stateData.TargetEntity = surroundings.FrontEntity;
                surroundings.MoveSuccess = true;
                surroundings.FrontEntity = surroundings.LeftEntity = surroundings.RightEntity = Entity.Null;
                MovementUtils.ResetMovableData(ref movableData);
                StateUtils.SwitchState(ref stateData, ECB, entity, index);
                return true;
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
        || stateData.CurState != UnitState.Idle)
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