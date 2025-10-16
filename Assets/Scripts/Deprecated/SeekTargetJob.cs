// using SparFlame.Components.SubGameplay;
// using SparFlame.Core.Utils;
// using Unity.Burst;
// using Unity.Collections;
// using Unity.Entities;
// using Unity.Mathematics;
// using Unity.Physics;
// using Unity.Transforms;
// using UnityEngine;
//
// // ReSharper disable UseIndexFromEndExpression
// namespace SparFlame.Systems.SubGameplay.Movement
// {
//     [BurstCompile]
//     [WithAll(typeof(MovingStateTag))]
//     public partial struct SeekTargetJob : IJobEntity
//     {
//         [ReadOnly] public PhysicsWorldSingleton PhysicsWorld;
//         [ReadOnly] public float ElapsedTime;
//         [ReadOnly] public MovementConfig Config;
//
//         private void Execute(
//             ref NavAgentComponent navAgent, 
//             ref MovableData movableData, in LocalTransform transform,
//             ref Surroundings surroundings, in BoxColliderSize boxColliderSize,
//             in DynamicBuffer<WaypointBuffer> waypointBuffer, InteractAbilityBonus bonus,
//             ref SeekTarget seekTarget
//         )
//         {
//             seekTarget.Direction = float3.zero;
//             navAgent.targetPosition = new float3(movableData.TargetCenterPos.x, movableData.TargetCenterPos.y,
//                 movableData.TargetCenterPos.z);
//             var interactiveRangeSq = math.square(movableData.InteractRange + bonus.RangeBonus);
//             var shouldMove = false;
//             var curPos2D = new float2(transform.Position.x, transform.Position.z);
//
//             surroundings.MoveSuccess = true;
//             if (ElapsedTime > surroundings.RecordPosTime)
//             {
//                 surroundings.PrePos = transform.Position;
//                 surroundings.RecordPosTime = ElapsedTime + Config.RecordPosInterval;
//             }
//             surroundings.MoveSuccess =
//                 !(math.distancesq(surroundings.PrePos, transform.Position) < Config.WayPointDistanceSq);
//             // Count the times it chooses another way
//             if (!surroundings.MoveSuccess)
//             {
//                 surroundings.CompromiseTimes += 1;
//                 movableData.MovementState = MovementState.NotMoving;
//                 movableData.DetailInfo = DetailInfo.Stuck;
//             }
//             else
//             {
//                 MovementUtils.ResetSurroundings(ref surroundings);
//             }
//             
//             // DetectSurrounding(ref surroundings, transform, movableData, boxColliderSize);
//             switch (movableData.MovementCommandType)
//             {
//                 // If Interactive movement
//                 case MovementCommandType.Interactive:
//                 {
//                     navAgent.extents = movableData.TargetColliderShape;
//                     var curDisSqPointToRect =
//                         MovementUtils.DistanceSqPointToBox(movableData.TargetCenterPos, movableData.TargetColliderShape,
//                             transform.Position);
//                     // When compare target position with self position use 3D pos, when compare waypoint position with self position use 2D pos
//                     // Current pos in Interactive range. This should be checked before the last waypoint , cause interactive movement DO NOT NEED or SHOULD NOT reach the last waypoint
//                     if (curDisSqPointToRect < interactiveRangeSq - Config.InteractRangeSqBias)
//                     {
//                         MovementUtils.ResetMovableData(ref movableData);
//                         MovementUtils.ResetNavAgent(ref navAgent);
//                         movableData.MovementState = MovementState.MovementComplete;
//                     }
//                     // Current pos not in Interactive range
//                     else
//                     {
//                         // Enable Calculation
//                         navAgent.enableCalculation = true;
//                         // First time command come, begin calculation and wait until next frame to read calculation result
//                         if (movableData.ForceCalculate)
//                         {
//                             navAgent.forceCalculate = true;
//                             movableData.ForceCalculate = false;
//                             MovementUtils.ResetSurroundings(ref surroundings);
//                             return;
//                         }
//
//                         // Calculation Complete
//                         if (navAgent.calculationComplete  && waypointBuffer.Length > 0)
//                         {
//                             // Calculate if target reachable
//                             var endPos = waypointBuffer[waypointBuffer.Length - 1].position;
//                             // var endDisSqPointToRect = MovementUtils.DistanceSqPointToBox(movableData.TargetCenterPos,
//                             //     movableData.TargetColliderShape, endPos);
//                             // movableData.DetailInfo =
//                             //     endDisSqPointToRect < interactiveRangeSq - Config.InteractRangeSqBias
//                             //         ? DetailInfo.Reachable
//                             //         : DetailInfo.NotReachable;
//                             var endPos2D = new float2(endPos.x, endPos.z);
//                             // If reach the last waypoint. Not using the index because moving takes time, even if the index is the last one, the object may not reach the last waypoint yet
//                             if (math.distancesq(endPos2D, curPos2D) < Config.WayPointDistanceSq)
//                             {
//                                 MovementUtils.ResetMovableData(ref movableData);
//                                 MovementUtils.ResetNavAgent(ref navAgent);
//                                 movableData.MovementState = movableData.DetailInfo == DetailInfo.Reachable
//                                     ? MovementState.MovementComplete
//                                     : MovementState.MovementPartialComplete;
//                             }
//                             // Not reach the last waypoint. Try moving
//                             else
//                             {
//                                 var waypointPos2D = new float2(waypointBuffer[navAgent.currentWaypoint].position.x,
//                                     waypointBuffer[navAgent.currentWaypoint].position.z);
//                                 if (navAgent.currentWaypoint + 1 < waypointBuffer.Length &&
//                                     math.distancesq(waypointPos2D, curPos2D) <
//                                     Config.WayPointDistanceSq)
//                                 {
//                                     navAgent.currentWaypoint += 1;
//                                 }
//
//                                 movableData.MovementState = MovementState.IsMoving;
//                                 shouldMove = true;
//                             }
//                         }
//                         // Calculation Not Complete
//                         else
//                         {
//                             movableData.MovementState = MovementState.NotMoving;
//                             movableData.DetailInfo = DetailInfo.CalculationNotComplete;
//                         }
//                     }
//                     break;
//                 }
//                 // If march movement. Target position should be terrain
//                 case MovementCommandType.March:
//                 {
//                     navAgent.extents = Config.MarchExtent;
//                     // March already arrived
//                     if (math.distancesq(movableData.TargetCenterPos, transform.Position) < Config.WayPointDistanceSq)
//                     {
//                         MovementUtils.ResetMovableData(ref movableData);
//                         MovementUtils.ResetNavAgent(ref navAgent);
//                         movableData.MovementState = MovementState.MovementComplete;
//                     }
//                     // March not arrived yet
//                     else
//                     {
//                         // Enable Calculation
//                         navAgent.enableCalculation = true;
//                         // If this is the first time command arrives, then force update path, wait until next frame to read result
//                         if (movableData.ForceCalculate)
//                         {
//                             navAgent.forceCalculate = true;
//                             movableData.ForceCalculate = false;
//                             MovementUtils.ResetSurroundings(ref surroundings);
//                             return;
//                         }
//
//                         // Calculation complete
//                         if (navAgent.calculationComplete && waypointBuffer.Length > 0)
//                         {
//                             // Calculate if target reachable
//                             // var endPosition = waypointBuffer[waypointBuffer.Length - 1].position;
//                             // var endDisToTarget = math.distancesq(movableData.TargetCenterPos, endPosition);
//                             // movableData.DetailInfo = endDisToTarget < Config.WayPointDistanceSq
//                             //     ? DetailInfo.Reachable
//                             //     : DetailInfo.NotReachable;
//
//                             var endPos2D = new float2(waypointBuffer[waypointBuffer.Length - 1].position.x,
//                                 waypointBuffer[waypointBuffer.Length - 1].position.z);
//                             // If reach the last waypoint. Not using the index because moving takes time, even if the index is the last one, the object may not reach the last waypoint yet
//                             if (math.distancesq(endPos2D, curPos2D) < Config.WayPointDistanceSq)
//                             {
//                                 MovementUtils.ResetMovableData(ref movableData);
//                                 MovementUtils.ResetNavAgent(ref navAgent);
//                                 movableData.MovementState = movableData.DetailInfo == DetailInfo.Reachable
//                                     ? MovementState.MovementComplete
//                                     : MovementState.MovementPartialComplete;
//                             }
//                             // Not reach the last waypoint. Try moving
//                             else
//                             {
//                                 var waypointPos2D = new float2(waypointBuffer[navAgent.currentWaypoint].position.x,
//                                     waypointBuffer[navAgent.currentWaypoint].position.z);
//                                 if (navAgent.currentWaypoint + 1 < waypointBuffer.Length &&
//                                     math.distancesq(waypointPos2D, curPos2D) <
//                                     Config.WayPointDistanceSq)
//                                 {
//                                     navAgent.currentWaypoint += 1;
//                                 }
//
//                                 movableData.MovementState = MovementState.IsMoving;
//                                 shouldMove = true;
//                             }
//                         }
//                         // Calculation Not Complete
//                         else
//                         {
//                             movableData.MovementState = MovementState.NotMoving;
//                             movableData.DetailInfo = DetailInfo.CalculationNotComplete;
//                         }
//                     }
//
//                     break;
//                 }
//                 // No command
//                 case MovementCommandType.None:
//                 {
//                     MovementUtils.ResetNavAgent(ref navAgent);
//                     return;
//                 }
//                 default:
//                     BurstSafe.UnexpectedEnum(movableData.MovementCommandType);
//                     break;
//             }
//
//
//             if (!shouldMove) return;
//             var idealDirection = waypointBuffer[navAgent.currentWaypoint].position - transform.Position;
//            
//             // If < 0.1f normalize will fail
//             if (math.length(idealDirection) > 0.1f)
//             {
//                 idealDirection = math.normalize(idealDirection);
//                 seekTarget.Direction = idealDirection;
//                 // Record Pos for checking stuck
//             }
//          
//         }
//
//
//         #region Deprecated
//
//         
//
//         private void DetectSurrounding(ref Surroundings surroundings, in LocalTransform transform,
//             in MovableData movableData, in BoxColliderSize boxColliderSize)
//         {
//             var realFront = math.mul(transform.Rotation, new float3(0, 0, 1));
//             var left = MovementUtils.GetLeftOrRight(realFront, true);
//             var right = MovementUtils.GetLeftOrRight(realFront, false);
//             var head = transform.Position + realFront * boxColliderSize.SeparationBox.z * Config.SurroundingDetectRayStartBiasRatio;
//             // var leftSide = transform.Position + left * boxColliderSize.SeparationBox.x * Config.SurroundingDetectRayStartBiasRatio;
//             // var rightSide = transform.Position + right * boxColliderSize.SeparationBox.x * Config.SurroundingDetectRayStartBiasRatio;
//             MovementUtils.ObstacleInDirection(ref PhysicsWorld,
//                 head,
//                 Config.ObstacleLayerMask, Config.DetectRaycastBelongsTo,
//                 realFront,
//                  Config.SurroundingRayDetectLength,
//                 out surroundings.FrontEntity);
//
//             MovementUtils.ObstacleInDirection(ref PhysicsWorld,
//                 head,
//                 Config.ObstacleLayerMask,
//                 Config.DetectRaycastBelongsTo,
//                 left,
//                 Config.SurroundingRayDetectLength, out surroundings.LeftEntity);
//
//             MovementUtils.ObstacleInDirection(ref PhysicsWorld,
//                 head,
//                 Config.ObstacleLayerMask,
//                 Config.DetectRaycastBelongsTo,
//                 right,
//                  Config.SurroundingRayDetectLength, out surroundings.RightEntity);
//         }
//         #endregion
//
//     }
// }