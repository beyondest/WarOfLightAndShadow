using Unity.Entities;
using SparFlame.Components.SubGameplay;
using SparFlame.Core.Utils;
using Unity.Burst;
using Unity.Collections;
using Unity.Mathematics;
using Unity.Transforms;


// ReSharper disable UseIndexFromEndExpression
namespace SparFlame.Systems.SubGameplay.Movement
{
    [BurstCompile]
    public partial struct SeekTargetJobPlus : IJobEntity
    {
        [ReadOnly] public float ElapsedTime;
        [ReadOnly] public MovementConfig Config;
        [ReadOnly] public ComponentLookup<MovingStateTag> MovingStateLookup;
        private void Execute(
            ref NavAgentComponent navAgent,
            ref MovableData movableData, in LocalTransform transform,
            ref Surroundings surroundings, in BoxColliderSize boxColliderSize,
            in DynamicBuffer<WaypointBuffer> waypointBuffer, InteractAbilityBonus bonus,
            ref SeekTarget seekTarget, Entity selfEntity
        )
        {
            seekTarget.Direction = float3.zero;
            // Not in moving state
            if (!MovingStateLookup.HasComponent(selfEntity) || !MovingStateLookup.IsComponentEnabled(selfEntity))
            {
                return;
            }
            navAgent.targetPosition = new float3(movableData.TargetCenterPos.x, movableData.TargetCenterPos.y,
                movableData.TargetCenterPos.z);
            var interactiveRangeSq = math.square(movableData.InteractRange + bonus.RangeBonus);
            var curPos2D = new float2(transform.Position.x, transform.Position.z);

            surroundings.MoveSuccess = true;
            if (ElapsedTime > surroundings.RecordPosTime)
            {
                surroundings.PrePos = transform.Position;
                surroundings.RecordPosTime = ElapsedTime + Config.RecordPosInterval;
            }
            surroundings.MoveSuccess =
                !(math.distancesq(surroundings.PrePos, transform.Position) < Config.WayPointDistanceSq);
            
            // Count the times it chooses another way
            if (!surroundings.MoveSuccess)
            {
                surroundings.CompromiseTimes += 1;
                movableData.MovementState = MovementState.NotMoving;
                movableData.DetailInfo = DetailInfo.Stuck;
            }
            else
            {
                MovementUtils.ResetSurroundings(ref surroundings);
            }

            var isTargetReached = false;
            switch (movableData.MovementCommandType)
            {
                case MovementCommandType.None:
                    MovementUtils.ResetNavAgent(ref navAgent);
                    return;
                case MovementCommandType.Interactive:
                    navAgent.extents = movableData.TargetColliderShape;
                    var curDisSqPointToRect = MovementUtils.DistanceSqPointToBox(movableData.TargetCenterPos,
                        movableData.TargetColliderShape,
                        transform.Position);
                    isTargetReached = curDisSqPointToRect < interactiveRangeSq - Config.InteractRangeSqBias;
                    break;
                case MovementCommandType.March:
                    navAgent.extents = Config.MarchExtent;
                    isTargetReached = math.distancesq(movableData.TargetCenterPos, transform.Position) <
                                      Config.WayPointDistanceSq;
                    break;
                default:
                    BurstSafe.UnexpectedEnum(movableData.MovementCommandType);
                    break;
            }

            if (isTargetReached)
            {
                MovementUtils.ResetMovableData(ref movableData);
                MovementUtils.ResetNavAgent(ref navAgent);
                movableData.MovementState = MovementState.MovementComplete;
                return;
            }

            // Enable Calculation
            navAgent.enableCalculation = true;
            // First time command come, begin calculation and wait until next frame to read calculation result
            if (movableData.ForceCalculate)
            {
                navAgent.forceCalculate = true;
                movableData.ForceCalculate = false;
                MovementUtils.ResetSurroundings(ref surroundings);
                return;
            }

            // Calculation not complete
            if (!navAgent.calculationComplete || waypointBuffer.Length == 0)
            {
                movableData.MovementState = MovementState.NotMoving;
                movableData.DetailInfo = DetailInfo.CalculationNotComplete;
                return;
            }
            var endPos = waypointBuffer[waypointBuffer.Length - 1].position;
            var endPos2D = new float2(endPos.x, endPos.z);
            // Moving complete
            if (math.distancesq(endPos2D, curPos2D) < Config.WayPointDistanceSq)
            {
                MovementUtils.ResetMovableData(ref movableData);
                MovementUtils.ResetNavAgent(ref navAgent);
                movableData.MovementState = movableData.DetailInfo == DetailInfo.Reachable
                    ? MovementState.MovementComplete
                    : MovementState.MovementPartialComplete;
                return;
            }

            // Not reach the last waypoint. Try moving
            var waypointPos2D = new float2(waypointBuffer[navAgent.currentWaypoint].position.x,
                waypointBuffer[navAgent.currentWaypoint].position.z);
            if (navAgent.currentWaypoint + 1 < waypointBuffer.Length &&
                math.distancesq(waypointPos2D, curPos2D) <
                Config.WayPointDistanceSq)
            {
                navAgent.currentWaypoint += 1;
            }

            movableData.MovementState = MovementState.IsMoving;


            var idealDirection = waypointBuffer[navAgent.currentWaypoint].position - transform.Position;

            // If < 0.1f normalize will fail
            if (math.length(idealDirection) > 0.1f)
            {
                idealDirection = math.normalize(idealDirection);
                seekTarget.Direction = idealDirection;
                // Record Pos for checking stuck
            }
        }


    }
}