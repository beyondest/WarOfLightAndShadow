using SparFlame.Components.General;
using SparFlame.Components.SubGameplay;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Transforms;
// ReSharper disable UseIndexFromEndExpression
namespace SparFlame.Systems.SubGameplay.Movement
{
    [BurstCompile]
    [WithAll(typeof(AITag))]
    [WithAll(typeof(MovingStateTag))]
    public partial struct AIMovementJob : IJobEntity
    {
        [ReadOnly] public PhysicsWorldSingleton PhysicsWorld;
        [ReadOnly] public float DeltaTime;
        [ReadOnly] public float ElapsedTime;
        [ReadOnly] public MovementConfig Config;
        [ReadOnly] public MovementDebug Debug;

        private void Execute(
            ref NavAgentComponent navAgent, ref MovableData movableData, ref LocalTransform transform,
            ref Surroundings surroundings, in BoxColliderSize boxColliderSize,
            in DynamicBuffer<WaypointBuffer> waypointBuffer, in InteractAbilityBonus bonus
        )
        {
            navAgent.targetPosition = new float3(movableData.TargetCenterPos.x, 0f, movableData.TargetCenterPos.z);
            var targetCenterPos2D = new float2(movableData.TargetCenterPos.x, movableData.TargetCenterPos.z);
            var curPos2D = new float2(transform.Position.x, transform.Position.z);
            var curPosY0 = new float3(transform.Position.x, 0f, transform.Position.z);
            var interactiveRangeSq = math.square(movableData.InteractRange + bonus.RangeBonus);
            var shouldMove = false;
            DetectSurrounding(ref surroundings,  transform,  movableData, boxColliderSize);

            switch (movableData.MovementCommandType)
            {
                // If Interactive movement
                case MovementCommandType.Interactive:
                {
                    navAgent.extents = new float3
                    {
                        x = movableData.TargetColliderShapeXZ.x,
                        y = 1f,
                        z = movableData.TargetColliderShapeXZ.y
                    };
                    var curDisSqPointToRect =
                        MovementUtils.DistanceSqPointToRect(targetCenterPos2D, movableData.TargetColliderShapeXZ,
                            curPos2D);
                    // Current pos in Interactive range. This should be checked before the last waypoint , cause interactive movement DO NOT NEED or SHOULD NOT reach the last waypoint
                    if (curDisSqPointToRect < interactiveRangeSq - Config.InteractRangeSqBias)
                    {
                        MovementUtils.ResetMovableData(ref movableData);
                        MovementUtils.ResetNavAgent(ref navAgent);
                        movableData.MovementState = MovementState.MovementComplete;
                    }
                    // Current pos not in Interactive range
                    else
                    {
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

                        // Calculation Complete
                        if (navAgent.calculationComplete)
                        {
                            // Calculate if target reachable
                            var endPos2D = new float2(waypointBuffer[waypointBuffer.Length - 1].position.x,
                                waypointBuffer[waypointBuffer.Length - 1].position.z);
                            var endDisSqPointToRect = MovementUtils.DistanceSqPointToRect(targetCenterPos2D,
                                movableData.TargetColliderShapeXZ, endPos2D);
                            movableData.DetailInfo =
                                endDisSqPointToRect < interactiveRangeSq - Config.InteractRangeSqBias
                                    ? DetailInfo.Reachable
                                    : DetailInfo.NotReachable;
                            // If reach the last waypoint. Not using the index because moving takes time, even if the index is the last one, the object may not reach the last waypoint yet
                            if (math.distancesq(endPos2D, curPos2D) < Config.WayPointDistanceSq)
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
                                if (navAgent.currentWaypoint + 1 < waypointBuffer.Length &&
                                    math.distancesq(waypointBuffer[navAgent.currentWaypoint].position, curPosY0) <
                                    Config.WayPointDistanceSq)
                                {
                                    navAgent.currentWaypoint += 1;
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
                    navAgent.extents = Config.AIMarchExtent;
                    // March already arrived
                    if (math.distancesq(targetCenterPos2D, curPos2D) < Config.WayPointDistanceSq)
                    {
                        MovementUtils.ResetMovableData(ref movableData);
                        MovementUtils.ResetNavAgent(ref navAgent);
                        movableData.MovementState = MovementState.MovementComplete;
                    }
                    // March not arrived yet
                    else
                    {
                        // Enable Calculation
                        navAgent.enableCalculation = true;
                        // If this is the first time command arrives, then force update path, wait until next frame to read result
                        if (movableData.ForceCalculate)
                        {
                            navAgent.forceCalculate = true;
                            movableData.ForceCalculate = false;
                            MovementUtils.ResetSurroundings(ref surroundings);
                            return;
                        }

                        // Calculation complete
                        if (navAgent.calculationComplete)
                        {
                            // Calculate if target reachable
                            var endPos2D = new float2(waypointBuffer[waypointBuffer.Length - 1].position.x,
                                waypointBuffer[waypointBuffer.Length - 1].position.z);
                            var endDisToTarget = math.distancesq(targetCenterPos2D, endPos2D);
                            movableData.DetailInfo = endDisToTarget < Config.WayPointDistanceSq
                                ? DetailInfo.Reachable
                                : DetailInfo.NotReachable;
                            // If reach the last waypoint. Not using the index because moving takes time, even if the index is the last one, the object may not reach the last waypoint yet
                            if (math.distancesq(endPos2D, curPos2D) < Config.WayPointDistanceSq)
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
                                if (navAgent.currentWaypoint + 1 < waypointBuffer.Length &&
                                    math.distancesq(waypointBuffer[navAgent.currentWaypoint].position, curPosY0) <
                                    Config.WayPointDistanceSq)
                                {
                                    navAgent.currentWaypoint += 1;
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
            }


            if (!shouldMove) return;
            var movePosY0 = waypointBuffer[navAgent.currentWaypoint].position;
            var idealDirection = movePosY0 - curPosY0;
            surroundings.MoveSuccess = true;
            // If < 0.1f normalize will fail
            if (math.length(idealDirection) > 0.1f)
            {
                idealDirection = math.normalize(idealDirection);
                // Try To Move Target towards waypoint. Only success if front is void
                TryMove(ref transform, ref movableData, ref surroundings, navAgent,
                    idealDirection, curPosY0 ,  bonus
                );
                // surroundings.IdealDirection = idealDirection;
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

        private void TryMove(ref LocalTransform transform,
            ref MovableData movableData,
            ref Surroundings surroundings,
            in NavAgentComponent navAgent,
            in float3 idealFront, in float3 curPosY0,
            in InteractAbilityBonus bonus
        )
        {
            
            var scale = Debug.enabled ? Debug.aiMovementScale : 1f;
            
            var moveLength = DeltaTime * (movableData.MoveSpeed + bonus.MoveSpeedBonus )* scale;
            // Record Pos for checking stuck
            if (ElapsedTime > surroundings.RecordPosTime)
            {
                surroundings.PrePos = transform.Position;
                surroundings.RecordPosTime = ElapsedTime + Config.RecordPosInterval;
            }

            surroundings.MoveSuccess =
                !(math.distancesq(surroundings.PrePos, transform.Position) < Config.WayPointDistanceSq);
            var targetRotation = quaternion.LookRotationSafe(-idealFront, math.up());
            targetRotation =  math.slerp(transform.Rotation.value, targetRotation, DeltaTime * Config.RotationSpeed);
            transform.Rotation = math.slerp(transform.Rotation.value, targetRotation, DeltaTime * Config.RotationSpeed);

         
            
            transform.Position += moveLength * idealFront;
        }

        private void DetectSurrounding(ref Surroundings surroundings, in LocalTransform transform,
            in MovableData movableData, in BoxColliderSize boxColliderSize)
        {
            var realFront = math.mul(transform.Rotation, new float3(0, 0, -1));
            var left = MovementUtils.GetLeftOrRight(realFront, true);
            var right = MovementUtils.GetLeftOrRight(realFront, false);
            var head = transform.Position + realFront * boxColliderSize.Box.z * Config.DetectFrontBiasRatio;
            MovementUtils.ObstacleInDirection(ref PhysicsWorld, 0f,
                head,
                Config.ObstacleLayerMask, Config.DetectRaycastBelongsTo,
                realFront,
                boxColliderSize.Box.z * Config.DetectLengthRatio,
                out surroundings.FrontEntity);

            MovementUtils.ObstacleInDirection(ref PhysicsWorld,  boxColliderSize.Box.x,
                head,
                Config.ObstacleLayerMask,
                Config.DetectRaycastBelongsTo,
                left,
                boxColliderSize.Box.x * Config.DetectLengthRatio, out surroundings.LeftEntity);
            MovementUtils.ObstacleInDirection(ref PhysicsWorld, boxColliderSize.Box.x,
                head,
                Config.ObstacleLayerMask,
                Config.DetectRaycastBelongsTo,
                right,
                boxColliderSize.Box.x * Config.DetectLengthRatio, out surroundings.RightEntity);
        }
    }
}