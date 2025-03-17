using SparFlame.GamePlaySystem.General;
using Unity.Entities;
using Unity.Burst;
using Unity.Collections;
using Unity.Mathematics;
using Unity.Transforms;
using System.Runtime.CompilerServices;
using Unity.Physics;
using UnityEngine;

// ReSharper disable UseIndexFromEndExpression


namespace SparFlame.GamePlaySystem.Movement
{
    // [BurstCompile]
    public partial struct MovementSystem : ISystem
    {
        private BufferLookup<WaypointBuffer> _waypointLookup;
        private ComponentLookup<MovableData> _movableLookup;
        private ComponentLookup<InteractableAttr> _basicAttrLookup;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<PhysicsWorldSingleton>();
            state.RequireForUpdate<EndSimulationEntityCommandBufferSystem.Singleton>();
            state.RequireForUpdate<NotPauseTag>();
            state.RequireForUpdate<MovementConfig>();
            _waypointLookup = state.GetBufferLookup<WaypointBuffer>(true);
            _movableLookup = state.GetComponentLookup<MovableData>();
            _basicAttrLookup = state.GetComponentLookup<InteractableAttr>(true);
        }

        // [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var config = SystemAPI.GetSingleton<MovementConfig>();
            var physicsWorld = SystemAPI.GetSingleton<PhysicsWorldSingleton>();
            _waypointLookup.Update(ref state);
            _movableLookup.Update(ref state);
            _basicAttrLookup.Update(ref state);
            // PathCalculated is set to true only if calculation is done successfully
            new MoveJob
            {
                PhysicsWorld = physicsWorld,
                WayPointsLookup = _waypointLookup,
                MovableLookup = _movableLookup,
                BasicAttrLookup = _basicAttrLookup,
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
    // [BurstCompile]
    [WithAll(typeof(HaveTarget))]
    public partial struct MoveJob : IJobEntity
    {
        [ReadOnly] public PhysicsWorldSingleton PhysicsWorld;
        [ReadOnly] public BufferLookup<WaypointBuffer> WayPointsLookup;
        [NativeDisableParallelForRestriction] public ComponentLookup<MovableData> MovableLookup;
        [ReadOnly] public ComponentLookup<InteractableAttr> BasicAttrLookup;
        [ReadOnly] public float DeltaTime;
        [ReadOnly] public float WayPointDistanceSq;
        [ReadOnly] public float MarchExtent;
        [ReadOnly] public float RotationSpeed;
        [ReadOnly] public uint ClickableLayerMask;
        [ReadOnly] public uint MovementRayBelongsToLayerMask;

        private void Execute(
            ref NavAgentComponent navAgent, ref LocalTransform transform, in PhysicsCollider collider,
            Entity entity)
        {
            var refMoveRW = MovableLookup.GetRefRW(entity);
            ref var movableData = ref refMoveRW.ValueRW;
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
            // Move Target towards waypoint

            var moveResult = TryMove(ref transform, ref movableData, ref PhysicsWorld, navAgent, movePos, curPos,
                DeltaTime,
                RotationSpeed,
                ClickableLayerMask, MovementRayBelongsToLayerMask, out var frontObstacleEntity);
            if (moveResult == TryMoveResult.Success) return;
            // Not Move successfully, and analyze the obstacle 
            
            var basicAttr = BasicAttrLookup[frontObstacleEntity];
            //when focus attack move on a target, while another enemy is on the way, will go into this situation
            if (basicAttr.FactionTag == FactionTag.Enemy)
            {
                movableData.OnTheWayTargetEntity = frontObstacleEntity;
                movableData.MovementState = MovementState.MovementPartialComplete;
                navAgent.EnableCalculation = false;
                //navAgent.CalculationComplete = false;
                movableData.MovementCommandType = MovementCommandType.None;
            }

            // var frontMovable = MovableLookup[frontObstacleEntity];
            //
            // if (frontMovable.MovementState == MovementState.IsMoving) return;
            // // When is blocked by not moving ally, should transfer the state
            // movableData.MovementState = frontMovable.MovementState;
            // navAgent.EnableCalculation = false;
            // navAgent.CalculationComplete = false;
            // movableData.MovementCommandType = MovementCommandType.None;
        }

        // [BurstCompile]
        private static TryMoveResult TryMove(ref LocalTransform transform, ref MovableData movableData,
            ref PhysicsWorldSingleton physicsWorld,
            in NavAgentComponent navAgent,
            in float3 movePos, in float3 curPos, in float deltaTime,
            in float rotationSpeed,
            uint clickableLayerMask,
            uint movementRayBelongsToLayerMask,
            out Entity frontObstacleEntity
        )
        {
            frontObstacleEntity = Entity.Null;
            var returnValue = TryMoveResult.Success;
            // Move Target towards waypoint
            var direction = movePos - curPos;
            var shouldRecalculate = false;
            // This line is crucial because math.normalize will return NAN sometimes without this line
            if (math.length(direction) < 0.1f)
            {
                return returnValue;
            }

            direction = math.normalize(direction);
            var moveDelta = deltaTime * movableData.MoveSpeed;
            //Check front, left, right direction, if there are obstacles in three directions, return false
            if (ObstacleInDirection(ref physicsWorld, movableData.ColliderRadius, curPos, clickableLayerMask,
                    movementRayBelongsToLayerMask, direction, moveDelta, out var hitEntityFront))
            {
                direction = GetLeftOrRight(direction, true);
                returnValue = TryMoveResult.FrontObstacle;
                frontObstacleEntity = hitEntityFront;
                if (ObstacleInDirection(ref physicsWorld, movableData.ColliderRadius, curPos, clickableLayerMask,
                        movementRayBelongsToLayerMask, direction, moveDelta,
                        out _))
                {
                    direction = GetLeftOrRight(direction, false);
                    returnValue = TryMoveResult.FrontLeftObstacle;
                    if (ObstacleInDirection(ref physicsWorld, movableData.ColliderRadius, curPos, clickableLayerMask,
                            movementRayBelongsToLayerMask, direction, moveDelta,
                            out _))
                    {
                        returnValue = TryMoveResult.FrontLeftRightObstacle;
                        return returnValue;
                    }
                    else
                    {
                        shouldRecalculate = true;
                    }
                }
                else
                {
                    shouldRecalculate = true;
                }
            }

            var targetRotation = quaternion.LookRotationSafe(-direction, math.up());
            transform.Rotation = math.slerp(transform.Rotation.value, targetRotation, deltaTime * rotationSpeed);
            transform.Position += moveDelta * direction;
            // Change the path to left and right
            if (shouldRecalculate) movableData.ForceCalculate = true;

            return returnValue;
        }

        // [BurstCompile]
        private static bool ObstacleInDirection(ref PhysicsWorldSingleton physicsWorld, float colliderRadius,
            float3 curPos, uint clickableLayerMask,
            uint movementRayBelongsToLayerMask, float3 direction, float3 castLength, out Entity entity)
        {
            var rayOrigin = curPos + direction * colliderRadius + new float3(0, 0.1f, 0);
            var rayEnd = rayOrigin + direction * castLength;
            var raycastInput = new RaycastInput
            {
                Start = rayOrigin,
                End = rayEnd,
                Filter = new CollisionFilter
                {
                    BelongsTo = movementRayBelongsToLayerMask,
                    CollidesWith = clickableLayerMask,
                    GroupIndex = 0
                }
            };
            if (physicsWorld.PhysicsWorld.CollisionWorld.CastRay(raycastInput, out var raycastHit))
            {
                entity = physicsWorld.PhysicsWorld.Bodies[raycastHit.RigidBodyIndex].Entity;
                return true;
            }
            else
            {
                entity = Entity.Null;
                return false;
            }
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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static float3 GetLeftOrRight(float3 direction, bool isLeft)
        {
            return isLeft ? new float3(-direction.z, 0, direction.x) : new float3(direction.z, 0, -direction.x);
        }
    }
}