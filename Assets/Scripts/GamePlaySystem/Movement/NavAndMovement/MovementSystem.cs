using SparFlame.GamePlaySystem.General;
using Unity.Entities;
using Unity.Burst;
using Unity.Collections;
using Unity.Mathematics;
using Unity.Transforms;
using System.Runtime.CompilerServices;

// ReSharper disable UseIndexFromEndExpression


namespace SparFlame.GamePlaySystem.Movement
{
    
    public partial struct MovementSystem : ISystem
    {
        private BufferLookup<WaypointBuffer> _waypointLookup;

        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<EndSimulationEntityCommandBufferSystem.Singleton>();
            state.RequireForUpdate<NotPauseTag>();
            state.RequireForUpdate<MovementConfig>();
            _waypointLookup = state.GetBufferLookup<WaypointBuffer>(true);
        }


        public void OnUpdate(ref SystemState state)
        {
            var config = SystemAPI.GetSingleton<MovementConfig>();
            _waypointLookup.Update(ref state);

            // PathCalculated is set to true only if calculation is done successfully
            new MoveJob
            {
                WayPointsLookup = _waypointLookup,
                DeltaTime = SystemAPI.Time.DeltaTime,
                WayPointDistanceSq = config.WayPointDistanceSq,
                MarchExtent = config.MarchExtent,
            }.ScheduleParallel();
        }
    }


    /// <summary>
    /// Move the movable entity to their target. According to the movement command type, will execute different logic
    /// </summary>
    [BurstCompile]
    [WithAll(typeof(HaveTarget))]
    public partial struct MoveJob : IJobEntity
    {
        [ReadOnly] public BufferLookup<WaypointBuffer> WayPointsLookup;
        [ReadOnly] public float DeltaTime;
        [ReadOnly] public float WayPointDistanceSq;
        [ReadOnly] public float MarchExtent;

        private void Execute(ref MovableData movableData,
            ref NavAgentComponent navAgent, ref LocalTransform transform,
            Entity entity)
        {
            navAgent.TargetPosition = new float3(movableData.TargetCenterPos.x, 0f, movableData.TargetCenterPos.z);
            var targetCenterPos2D = new float2(movableData.TargetCenterPos.x, movableData.TargetCenterPos.z);
            var curPos2D = new float2(transform.Position.x, transform.Position.z);
            var curPos = new float3(transform.Position.x, 0f, transform.Position.z);
            var interactiveRangeSq = movableData.InteractiveRangeSq;
            var shouldMove = false;
            //navAgent.EnableCalculation = true;
            // This line should always be false, cause agent has buffer 
            if (!WayPointsLookup.TryGetBuffer(entity, out var waypointBuffer)) return;
            
            switch (movableData.MovementCommandType)
            {
                // If Interactive movement
                case MovementCommandType.Interactive:
                {
                    navAgent.Extents = new float3
                    {
                        x = movableData.TargetColliderShapeXZ.x,
                        y = 1f,
                        z = movableData.TargetColliderShapeXZ.y
                    };
                    var curDisSqPointToRect =
                        DistanceSqPointToRect(targetCenterPos2D, movableData.TargetColliderShapeXZ, curPos2D);
                    // Current pos in Interactive range. This should be checked before the last waypoint,
                    // cause interactive movement DO NOT NEED or SHOULD NOT reach the last waypoint
                    if (curDisSqPointToRect < interactiveRangeSq)
                    {
                        movableData.MovementState = MovementState.MovementComplete;
                        movableData.DetailInfo = DetailInfo.None;
                        navAgent.EnableCalculation = false;
                        navAgent.CalculationComplete = false;
                        movableData.MovementCommandType = MovementCommandType.None;
                    }
                    // Current pos not in Interactive range
                    else
                    {
                        // Enable Calculation
                        navAgent.EnableCalculation = true;
                        if (movableData.ForceCalculate)
                        {
                            navAgent.ForceCalculate = true;
                            movableData.ForceCalculate = false;
                        }
                        // Calculation Complete
                        if (navAgent.CalculationComplete)
                        {
                            // Calculate if target reachable
                            var endPos2D = new float2(waypointBuffer[waypointBuffer.Length - 1].WayPoint.x,
                                waypointBuffer[waypointBuffer.Length - 1].WayPoint.z);
                            var endDisSqPointToRect = DistanceSqPointToRect(targetCenterPos2D,
                                movableData.TargetColliderShapeXZ, endPos2D);
                            movableData.DetailInfo = endDisSqPointToRect < interactiveRangeSq
                                ? DetailInfo.Reachable
                                : DetailInfo.NotReachable;
                            // If reach the last waypoint. Not using the index because moving takes time,
                            // Even if the index is the last one, the object may not reach the last waypoint yet
                            if (math.distancesq(endPos2D, curPos2D) < WayPointDistanceSq)
                            {
                                movableData.MovementState = movableData.DetailInfo == DetailInfo.Reachable
                                    ? MovementState.MovementComplete
                                    : MovementState.MovementPartialComplete;
                                navAgent.EnableCalculation = false;
                                navAgent.CalculationComplete = false;
                                movableData.MovementCommandType = MovementCommandType.None;
                            }
                            // Not reach the last waypoint. Try moving
                            else
                            {
                                if (navAgent.CurrentWaypoint + 1 < waypointBuffer.Length && 
                                    math.distancesq(waypointBuffer[navAgent.CurrentWaypoint].WayPoint, curPos) < WayPointDistanceSq)
                                {
                                    navAgent.CurrentWaypoint += 1;
                                }

                                movableData.MovementState = MovementState.IsMoving;
                                shouldMove = true;
                            }
                        }
                        // Calculation Not Complete
                        else
                        {
                            movableData.MovementState = MovementState.NotMoving;
                            movableData.DetailInfo = DetailInfo.CalculationNotComplete;
                        }
                    }

                    break;
                }
                // If march movement. Target position should be void
                case MovementCommandType.March:
                {
                    navAgent.Extents = new float3(MarchExtent, 1f, MarchExtent);
                    // March already arrived
                    if (math.distancesq(targetCenterPos2D, curPos2D) < WayPointDistanceSq)
                    {
                        movableData.MovementState = MovementState.MovementComplete;
                        movableData.DetailInfo = DetailInfo.None;
                        navAgent.EnableCalculation = false;
                        navAgent.CalculationComplete = false;
                        movableData.MovementCommandType = MovementCommandType.None;
                    }
                    // March not arrived yet
                    else
                    {
                        // Enable Calculation
                        navAgent.EnableCalculation = true;
                        if (movableData.ForceCalculate)
                        {
                            navAgent.ForceCalculate = true;
                            movableData.ForceCalculate = false;
                        }
                        // Calculation complete
                        if (navAgent.CalculationComplete)
                        {
                            // Calculate if target reachable
                            var endPos2D = new float2(waypointBuffer[waypointBuffer.Length - 1].WayPoint.x,
                                waypointBuffer[waypointBuffer.Length - 1].WayPoint.z);
                            var endDisToTarget = math.distancesq(targetCenterPos2D, endPos2D);
                            movableData.DetailInfo = endDisToTarget < WayPointDistanceSq
                                ? DetailInfo.Reachable
                                : DetailInfo.NotReachable;
                            // If reach the last waypoint. Not using the index because moving takes time,
                            // Even if the index is the last one, the object may not reach the last waypoint yet
                            if (math.distancesq(endPos2D, curPos2D) < WayPointDistanceSq)
                            {
                                movableData.MovementState = movableData.DetailInfo == DetailInfo.Reachable
                                    ? MovementState.MovementComplete
                                    : MovementState.MovementPartialComplete;
                                navAgent.EnableCalculation = false;
                                navAgent.CalculationComplete = false;
                                movableData.MovementCommandType = MovementCommandType.None;
                            }
                            // Not reach the last waypoint. Try moving
                            else
                            {
                                if (navAgent.CurrentWaypoint + 1 < waypointBuffer.Length &&
                                    math.distancesq(waypointBuffer[navAgent.CurrentWaypoint].WayPoint, curPos) <
                                    WayPointDistanceSq)
                                {
                                    navAgent.CurrentWaypoint += 1;
                                }

                                movableData.MovementState = MovementState.IsMoving;
                                shouldMove = true;
                            }
                        }
                        // Calculation Not Complete
                        else
                        {
                            movableData.MovementState = MovementState.NotMoving;
                            movableData.DetailInfo = DetailInfo.CalculationNotComplete;
                        }
                    }
                    break;
                }
                // No command
                case MovementCommandType.None:
                {
                    break;
                }
                default:
                    return;
            }

            if (!shouldMove) return;

            var movePos = waypointBuffer[navAgent.CurrentWaypoint].WayPoint;
            var direction = movePos - curPos;
            // This line is crucial because math.normalize will return NAN sometimes without this line
            if(math.length(direction) < 0.1f)return;
            var angle = math.degrees(math.atan2(direction.z, direction.x));
            transform.Rotation = math.slerp(
                transform.Rotation,
                quaternion.Euler(new float3(0, angle, 0)),
                DeltaTime);
            transform.Position +=
                math.normalize(direction) * DeltaTime * movableData.MoveSpeed;
        }


        /// <summary>
        /// This method calculates the min distance between pos and a rect with centerPos and size
        /// </summary>
        /// <param name="centerPos"></param>
        /// <param name="size"></param>
        /// <param name="pos"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static float DistanceSqPointToRect(float2 centerPos, float2 size, float2 pos)
        {
            var halfSize = size * 0.5f;
            var min = centerPos - halfSize;
            var max = centerPos + halfSize;

            // this clamp method is what you know in scalar, and also works in vector
            var clampedPos = math.clamp(pos, min, max);
            return math.distancesq(pos, clampedPos);
        }
    }
}