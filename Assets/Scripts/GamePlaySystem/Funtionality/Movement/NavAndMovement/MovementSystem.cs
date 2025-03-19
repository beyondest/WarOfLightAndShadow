using System;
using Unity.Entities;
using Unity.Burst;
using Unity.Collections;
using Unity.Mathematics;
using Unity.Transforms;
using System.Runtime.CompilerServices;
using Unity.Physics;
using SparFlame.GamePlaySystem.General;

// ReSharper disable UseIndexFromEndExpression


namespace SparFlame.GamePlaySystem.Movement
{
    [BurstCompile]
    public partial struct MovementSystem : ISystem
    {
        private BufferLookup<WaypointBuffer> _waypointLookup;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<PhysicsWorldSingleton>();
            state.RequireForUpdate<EndSimulationEntityCommandBufferSystem.Singleton>();
            state.RequireForUpdate<NotPauseTag>();
            state.RequireForUpdate<MovementConfig>();
            _waypointLookup = state.GetBufferLookup<WaypointBuffer>(true);
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var config = SystemAPI.GetSingleton<MovementConfig>();
            var physicsWorld = SystemAPI.GetSingleton<PhysicsWorldSingleton>();
            _waypointLookup.Update(ref state);
            // PathCalculated is set to true only if calculation is done successfully
            new MoveJob
            {
                PhysicsWorld = physicsWorld,
                WayPointsLookup = _waypointLookup,
                DeltaTime = SystemAPI.Time.DeltaTime,
                WayPointDistanceSq = config.WayPointDistanceSq,
                MarchExtent = config.MarchExtent,
                RotationSpeed = config.RotationSpeed,
                ClickableLayerMask = config.ClickableLayerMask,
                MovementRayBelongsToLayerMask = config.MovementRayBelongsToLayerMask,
            }.ScheduleParallel();
        }
    }


    /// <summary>
    /// Move the movable entity to their target. According to the movement command type, will execute different logic
    /// </summary>
    [BurstCompile]
    [WithAll(typeof(MovingStateTag))]
    public partial struct MoveJob : IJobEntity
    {
        [ReadOnly] public PhysicsWorldSingleton PhysicsWorld;
        [ReadOnly] public BufferLookup<WaypointBuffer> WayPointsLookup;
        [ReadOnly] public float DeltaTime;
        [ReadOnly] public float WayPointDistanceSq;
        [ReadOnly] public float MarchExtent;
        [ReadOnly] public float RotationSpeed;
        [ReadOnly] public uint ClickableLayerMask;
        [ReadOnly] public uint MovementRayBelongsToLayerMask;

        private void Execute(
            ref NavAgentComponent navAgent, ref MovableData movableData, ref LocalTransform transform,
            ref Surroundings surroundings,
            Entity entity)
        {
            navAgent.TargetPosition = new float3(movableData.TargetCenterPos.x, 0f, movableData.TargetCenterPos.z);
            var targetCenterPos2D = new float2(movableData.TargetCenterPos.x, movableData.TargetCenterPos.z);
            var curPos2D = new float2(transform.Position.x, transform.Position.z);
            var curPosY0 = new float3(transform.Position.x, 0f, transform.Position.z);
            var interactiveRangeSq = movableData.InteractiveRangeSq;
            var shouldMove = false;

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
                    // Current pos in Interactive range. This should be checked before the last waypoint , cause interactive movement DO NOT NEED or SHOULD NOT reach the last waypoint
                    if (curDisSqPointToRect < interactiveRangeSq)
                    {
                        MovementUtils.ResetMovableData(ref movableData);
                        MovementUtils.ResetNavAgent(ref navAgent);
                        movableData.MovementState = MovementState.MovementComplete;
                    }
                    // Current pos not in Interactive range
                    else
                    {
                        // Enable Calculation
                        navAgent.EnableCalculation = true;
                        // First time command come
                        if (movableData.ForceCalculate)
                        {
                            navAgent.ForceCalculate = true;
                            movableData.ForceCalculate = false;
                            MovementUtils.ResetSurroundings(ref surroundings);
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
                            // If reach the last waypoint. Not using the index because moving takes time, even if the index is the last one, the object may not reach the last waypoint yet
                            if (math.distancesq(endPos2D, curPos2D) < WayPointDistanceSq)
                            {
                                MovementUtils.ResetMovableData(ref movableData);
                                MovementUtils.ResetNavAgent(ref navAgent);
                                movableData.MovementState = movableData.DetailInfo == DetailInfo.Reachable
                                    ? MovementState.MovementComplete
                                    : MovementState.MovementPartialComplete;
                            }
                            // Not reach the last waypoint. Try moving
                            else
                            {
                                if (navAgent.CurrentWaypoint + 1 < waypointBuffer.Length &&
                                    math.distancesq(waypointBuffer[navAgent.CurrentWaypoint].WayPoint, curPosY0) <
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
                // If march movement. Target position should be terrain
                case MovementCommandType.March:
                {
                    navAgent.Extents = new float3(MarchExtent, 1f, MarchExtent);
                    // March already arrived
                    if (math.distancesq(targetCenterPos2D, curPos2D) < WayPointDistanceSq)
                    {
                        MovementUtils.ResetMovableData(ref movableData);
                        MovementUtils.ResetNavAgent(ref navAgent);
                        movableData.MovementState = MovementState.MovementComplete;
                    }
                    // March not arrived yet
                    else
                    {
                        // Enable Calculation
                        navAgent.EnableCalculation = true;
                        // If this is the first time command arrives, then force update path
                        if (movableData.ForceCalculate)
                        {
                            navAgent.ForceCalculate = true;
                            movableData.ForceCalculate = false;
                            MovementUtils.ResetSurroundings(ref surroundings);
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
                            // If reach the last waypoint. Not using the index because moving takes time, even if the index is the last one, the object may not reach the last waypoint yet
                            if (math.distancesq(endPos2D, curPos2D) < WayPointDistanceSq)
                            {
                                MovementUtils.ResetMovableData(ref movableData);
                                MovementUtils.ResetNavAgent(ref navAgent);
                                movableData.MovementState = movableData.DetailInfo == DetailInfo.Reachable
                                    ? MovementState.MovementComplete
                                    : MovementState.MovementPartialComplete;
                            }
                            // Not reach the last waypoint. Try moving
                            else
                            {
                                if (navAgent.CurrentWaypoint + 1 < waypointBuffer.Length &&
                                    math.distancesq(waypointBuffer[navAgent.CurrentWaypoint].WayPoint, curPosY0) <
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
                    MovementUtils.ResetNavAgent(ref navAgent);
                    return;
                }
                default:
                    throw new ArgumentOutOfRangeException();
            }

            if (!shouldMove) return;

            var movePosY0 = waypointBuffer[navAgent.CurrentWaypoint].WayPoint;
            var idealDirection = movePosY0 - curPosY0;
            surroundings.MoveSuccess = true;
            // If < 0.1f normalize will fail
            if (math.length(idealDirection) > 0.1f)
            {
                idealDirection = math.normalize(idealDirection);
                // Try To Move Target towards waypoint. Only success if front is void
                surroundings.MoveSuccess = TryMove(ref transform, ref movableData,ref surroundings, 
                    navAgent, in idealDirection, curPosY0
                    );
                surroundings.IdealDirection = idealDirection;
            }
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
        }
        [BurstCompile]
        private bool TryMove(ref LocalTransform transform, ref MovableData movableData,
            ref Surroundings surroundings,
            in NavAgentComponent navAgent,
            in float3 idealFront, in float3 curPosY0
        )
        {
            var realFront = math.mul(transform.Rotation, new float3(0, 0, -1));
            var left = MovementUtils.GetLeftOrRight(realFront, true);
            var right = MovementUtils.GetLeftOrRight(realFront, false);
            var moveLength = DeltaTime * movableData.MoveSpeed;
            // If speed too slow, front overlap may adjust too much and unit will go in circle
            if (moveLength < 0.03) moveLength = 0.03f;
            
            var head = curPosY0 + realFront * movableData.SelfColliderShapeXz.y * 0.51f;
            var tail = curPosY0 - realFront * movableData.SelfColliderShapeXz.y * 0.51f;
            var frontObstacle = MovementUtils.ObstacleInDirection(ref PhysicsWorld, movableData.SelfColliderShapeXz.y,
                curPosY0,
                ClickableLayerMask, MovementRayBelongsToLayerMask,
                realFront,
                moveLength,
                out surroundings.FrontEntity);

            var leftOverlap = MovementUtils.ObstacleInDirection(ref PhysicsWorld, movableData.SelfColliderShapeXz.x,
                head,
                ClickableLayerMask,
                MovementRayBelongsToLayerMask,
                left,
                movableData.SelfColliderShapeXz.x * 0.1f, out surroundings.LeftEntity);
            var rightOverlap = MovementUtils.ObstacleInDirection(ref PhysicsWorld, movableData.SelfColliderShapeXz.x,
                head,
                ClickableLayerMask,
                MovementRayBelongsToLayerMask,
                right,
                movableData.SelfColliderShapeXz.x * 0.1f, out surroundings.RightEntity);
            
            var leftTailOverlap = MovementUtils.ObstacleInDirection(ref PhysicsWorld, movableData.SelfColliderShapeXz.x,
                tail,
                ClickableLayerMask,
                MovementRayBelongsToLayerMask,
                left,
                movableData.SelfColliderShapeXz.x * 0.1f, out surroundings.LeftTailEntity);
            var rightTailOverlap = MovementUtils.ObstacleInDirection(ref PhysicsWorld, movableData.SelfColliderShapeXz.x,
                tail,
                ClickableLayerMask,
                MovementRayBelongsToLayerMask,
                right,
                movableData.SelfColliderShapeXz.x * 0.1f, out surroundings.RightTailEntity);
            
            

            if (frontObstacle)
            {
                // Check front overlap
                if (MovementUtils.ObstacleInDirection(ref PhysicsWorld, movableData.SelfColliderShapeXz.y,
                        curPosY0,
                        ClickableLayerMask, MovementRayBelongsToLayerMask,
                        realFront,
                        0.03f, out _))
                {
                    transform.Position -= realFront * 0.2f * moveLength;
                }
            }

            if (leftOverlap && !rightOverlap)
            {
                transform.Position -= left * 0.1f * movableData.SelfColliderShapeXz.x;
            }

            if (rightOverlap && !leftOverlap)
            {
                transform.Position -= right * 0.1f * movableData.SelfColliderShapeXz.x;
            }
            
            var targetRotation = quaternion.LookRotationSafe(-idealFront, math.up());
            transform.Rotation = math.slerp(transform.Rotation.value, targetRotation, DeltaTime * RotationSpeed);
            if (!frontObstacle)
            {
                transform.Position += moveLength * idealFront;
                return true;
            }

            return false;
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