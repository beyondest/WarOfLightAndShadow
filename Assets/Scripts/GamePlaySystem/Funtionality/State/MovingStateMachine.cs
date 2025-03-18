using SparFlame.GamePlaySystem.General;
using SparFlame.GamePlaySystem.Movement;
using SparFlame.GamePlaySystem.Units;
using SparFlame.GamePlaySystem.UnitSelection;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Transforms;

namespace SparFlame.GamePlaySystem.State
{
    public partial struct MovingStateMachine : ISystem
    {
        private ComponentLookup<InteractableAttr> _interactableLookup;
        private ComponentLookup<Selected> _selectedLookup;
        private ComponentLookup<AutoGiveWayData> _autoGiveWayLookup;
        private ComponentLookup<LocalTransform> _localTransformLookup;
        private ComponentLookup<MovableData> _movableLookup;
        private ComponentLookup<UnitBasicStateData> _unitBasicStateLookup;
        
        
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<EndSimulationEntityCommandBufferSystem.Singleton>();
            state.RequireForUpdate<MovingStateMachineConfig>();
            state.RequireForUpdate<NotPauseTag>();
            _interactableLookup = state.GetComponentLookup<InteractableAttr>(true);
            _selectedLookup = state.GetComponentLookup<Selected>(true);
            _autoGiveWayLookup = state.GetComponentLookup<AutoGiveWayData>(true);
            _localTransformLookup = state.GetComponentLookup<LocalTransform>(true);
            _movableLookup = state.GetComponentLookup<MovableData>();
            _unitBasicStateLookup = state.GetComponentLookup<UnitBasicStateData>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var config = SystemAPI.GetSingleton<MovingStateMachineConfig>();
            var ecb = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>();
            
            _interactableLookup.Update(ref state);
            _selectedLookup.Update(ref state);
            _autoGiveWayLookup.Update(ref state);
            _localTransformLookup.Update(ref state);
            _movableLookup.Update(ref state);
            _unitBasicStateLookup.Update(ref state);
            new CheckMovingState
            {
                ECB = ecb.CreateCommandBuffer(state.WorldUnmanaged).AsParallelWriter(),
                InteractLookUp = _interactableLookup,
                Selected = _selectedLookup,
                AutoGiveWayLookup = _autoGiveWayLookup,
                TransLookup = _localTransformLookup,
                MovableLookup = _movableLookup,
                StateLookup = _unitBasicStateLookup,
                MaxAllowedCompromiseTimes = config.MaxAllowedCompromiseTimes
            }.ScheduleParallel();
        }
        

        [BurstCompile]
        [WithAll(typeof(MovingStateTag))]
        public partial struct CheckMovingState : IJobEntity
        {
            public EntityCommandBuffer.ParallelWriter ECB;
            [ReadOnly] public ComponentLookup<InteractableAttr> InteractLookUp;
            [ReadOnly] public ComponentLookup<Selected> Selected;
            [ReadOnly] public ComponentLookup<AutoGiveWayData> AutoGiveWayLookup;
            [ReadOnly] public ComponentLookup<LocalTransform> TransLookup;
            [NativeDisableParallelForRestriction] public ComponentLookup<MovableData> MovableLookup;
            [NativeDisableParallelForRestriction] public ComponentLookup<UnitBasicStateData> StateLookup;
            [ReadOnly] public int MaxAllowedCompromiseTimes;

            private void Execute([ChunkIndexInQuery] int index, ref Surroundings surroundings,
                Entity entity)
            {
                if (!StateLookup.HasComponent(entity)) return;
                if (!TransLookup.HasComponent(entity)) return;
                ref var stateData = ref StateLookup.GetRefRW(entity).ValueRW;
                ref var movableData = ref MovableLookup.GetRefRW(entity).ValueRW;
                var transform = TransLookup.GetRefRO(entity).ValueRO;
                if (stateData.CurState != UnitState.Moving) return;

                // Front enemy will taunt moving unit. This is the highest priority
                if (CheckTaunted(ref surroundings, ref movableData, ref stateData, entity, index))
                    return;

                // Check if reached the last waypoint
                if (CheckIfCompleteMoving(ref surroundings, ref movableData, ref stateData, entity, index))
                    return;

                // Not complete the moving and stuck by enemy building. Should focus on that enemy building until it is destroyed.
                if (TryResolveStuck(ref surroundings, ref movableData, ref stateData, in transform, entity, index))
                    return;

                // Focus unit cannot auto change target
                if(stateData.Focus)return;
                
                // Not complete and should switch to other target. As long as focus == false, this may happen in many cases
                CheckShouldChangeTarget();
            }

            private bool TryResolveStuck(ref Surroundings surroundings, ref MovableData movableData,
                ref UnitBasicStateData stateData, in LocalTransform transform, Entity entity,int index)
            {
                // Stuck or get circled
                if (!(surroundings.MoveResult == TryMoveResult.FrontLeftRightObstacle
                      || surroundings.CompromiseTimes > MaxAllowedCompromiseTimes)) return false;

                // Try to let the unselected ally unit auto give way
                if (TryTellAutoGiveWay(ref surroundings, in transform, entity,index))
                    return true;

                // If front is enemy building and get stuck, remove it
                if (InteractLookUp.TryGetComponent(surroundings.FrontEntity, out var iDataFront)
                    && iDataFront is { BaseTag: BaseTag.Buildings, FactionTag: FactionTag.Enemy })
                {
                    stateData.TargetEntity = surroundings.FrontEntity;
                    stateData.TargetState = UnitState.Attacking;
                    stateData.Focus = true;
                    StateUtils.SwitchState(ref stateData, ECB, entity,index);
                    return true;
                }

                // Focus unit cannot auto switch target even get stuck
                if(stateData.Focus)return false;
                
                // If front is not enemy building and get stuck and left or right is enemy unit, attack it. Front cannot be enemy unit or it will get taunted
                if (
                    (InteractLookUp.TryGetComponent(surroundings.LeftEntity, out var iDataLeft)
                     && iDataLeft is { BaseTag: BaseTag.Units, FactionTag: FactionTag.Enemy })
                    || (InteractLookUp.TryGetComponent(surroundings.RightEntity, out var iDataRight)
                        && iDataRight is { BaseTag: BaseTag.Units, FactionTag: FactionTag.Enemy })
                )
                {
                    stateData.TargetEntity = iDataLeft is {BaseTag: BaseTag.Units, FactionTag: FactionTag.Enemy}?
                        surroundings.LeftEntity : surroundings.RightEntity;
                    stateData.TargetState = UnitState.Attacking;
                    StateUtils.SwitchState(ref stateData, ECB, entity,index);
                }
                return false;
            }

            private void CheckShouldChangeTarget()
            {
            }

            private bool CheckIfCompleteMoving(ref Surroundings surroundings, ref MovableData movableData,
                ref UnitBasicStateData stateData, Entity entity, int index)
            {
                // If itself moving job is completed
                if (movableData.MovementState is MovementState.MovementComplete
                    or MovementState.MovementPartialComplete)
                {
                    MovementUtils.ResetMovableData(ref movableData);
                    StateUtils.SwitchState(ref stateData, ECB, entity, index);
                    return true;
                }

                // Check if surrounded ally unit reached. 
                var isSelected = Selected.IsComponentEnabled(entity);
                if (!CheckIfSurroundReach(ref surroundings, isSelected)) return false;
                stateData.TargetState = UnitState.Idle;
                MovementUtils.ResetMovableData(ref movableData);
                StateUtils.SwitchState(ref stateData, ECB, entity, index);
                return true;
            }

            private bool CheckIfSurroundReach(ref Surroundings surroundings, bool selected)
            {
                switch (surroundings.MoveResult)
                {
                    case TryMoveResult.FrontLeftRightObstacle:
                    {
                        if (IsObstacleSelectedAllyIdle(surroundings.FrontEntity, selected)
                            || IsObstacleSelectedAllyIdle(surroundings.LeftEntity, selected)
                            || IsObstacleSelectedAllyIdle(surroundings.RightEntity, selected))
                        {
                            return true;
                        }

                        break;
                    }
                    case TryMoveResult.FrontObstacle:
                    {
                        // Only when unit compromises too much times then it will be considered as stuck
                        if (surroundings.CompromiseTimes < MaxAllowedCompromiseTimes) return false;
                        if (IsObstacleSelectedAllyIdle(surroundings.FrontEntity, selected))
                        {
                            return true;
                        }

                        break;
                    }
                    case TryMoveResult.FrontLeftObstacle:
                    {
                        if (surroundings.CompromiseTimes < MaxAllowedCompromiseTimes) return false;
                        if (IsObstacleSelectedAllyIdle(surroundings.FrontEntity, selected)
                            || IsObstacleSelectedAllyIdle(surroundings.LeftEntity, selected))
                        {
                            return true;
                        }

                        break;
                    }
                    case TryMoveResult.Success:
                    default:
                        return false;
                }

                return false;
            }

            private bool IsObstacleSelectedAllyIdle(Entity entity, bool selected)
            {
                return
                    InteractLookUp.TryGetComponent(entity, out var iData)
                    && Selected.IsComponentEnabled(entity) == selected
                    && iData is { BaseTag: BaseTag.Units, FactionTag: FactionTag.Ally }
                    && StateLookup.TryGetComponent(entity, out var stateData)
                    && stateData.CurState == UnitState.Idle;
            }

            private bool TryTellAutoGiveWay(ref Surroundings surroundings, in LocalTransform transform,
                Entity entity, int index)
            {
                // Only selected units can let others give way
                if (!Selected.IsComponentEnabled(entity)) return false;

                // Only not selected ally unit can auto give way
                if (!InteractLookUp.TryGetComponent(surroundings.FrontEntity, out var iData)
                    || iData is not { BaseTag: BaseTag.Units, FactionTag: FactionTag.Ally }
                    || Selected.IsComponentEnabled(surroundings.FrontEntity))
                    return false;

                // Only let one unit give way once, until it finishes auto give way.
                if (AutoGiveWayLookup.HasComponent(surroundings.FrontEntity))
                    return true;

                var frontPos = TransLookup.GetRefRO(surroundings.FrontEntity);
                var moveVector = MovementUtils.GetLeftOrRight(surroundings.FrontDirection,
                    MovementUtils.GetSide(transform.Position, frontPos.ValueRO.Position, surroundings.FrontDirection));
                var frontRadius = MovableLookup.GetRefRO(surroundings.FrontEntity).ValueRO.SelfColliderRadius;

                ECB.AddComponent(index,surroundings.FrontEntity, new AutoGiveWayData
                {
                    ElapsedTime = 0f,
                    MoveVector = moveVector * frontRadius * 2,
                    IfGoBack = false
                });
                return true;
            }


            private bool CheckTaunted(ref Surroundings surroundings, ref MovableData movableData,
                ref UnitBasicStateData stateData, Entity entity, int index)
            {
                if (surroundings.MoveResult == TryMoveResult.Success) return false;
                if (!InteractLookUp.TryGetComponent(surroundings.FrontEntity, out var iData)) return false;
                if (iData is not { FactionTag: FactionTag.Enemy, BaseTag: BaseTag.Units }) return false;

                // Transfer to Attacking State
                stateData.TargetState = UnitState.Attacking;
                stateData.TargetEntity = surroundings.FrontEntity;
                surroundings.MoveResult = TryMoveResult.Success;
                surroundings.FrontEntity = surroundings.LeftEntity = surroundings.RightEntity = Entity.Null;
                MovementUtils.ResetMovableData(ref movableData);
                StateUtils.SwitchState(ref stateData, ECB, entity,index);
                return true;
            }
        }
    }
}