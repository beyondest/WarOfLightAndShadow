using System.Runtime.CompilerServices;
using Unity.Physics;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;


namespace SparFlame.GamePlaySystem.Movement
{
    public struct MovementUtils
    {
        /// <summary>
        /// Will cast 2 rays in one direction, one is left corner ray, the other is right corner ray.
        /// </summary>
        /// <param name="physicsWorld"></param>
        /// <param name="colliderDirectionSize">This is the collider radius of mover, not the one being detected</param>
        /// <param name="curPos"></param>
        /// <param name="collideWith"></param>
        /// <param name="belongs"></param>
        /// <param name="direction">Detect ray direction</param>
        /// <param name="detectLength">the ray cast length</param>
        /// <param name="hitEntity"></param>
        /// <returns>Only when 2 rays hit nothing, will return false.
        /// Otherwise, return true, and hitEntity will be the left hit one or right hit one,
        /// depends on which side hits</returns>
        public static bool ObstacleInDirection(ref PhysicsWorldSingleton physicsWorld, float colliderDirectionSize,
            float3 curPos, uint collideWith,
            uint belongs, float3 direction, float detectLength, out Entity hitEntity)
        {
            var rayOrigin = curPos + direction * colliderDirectionSize * 0.51f + new float3(0, 0.1f, 0);
            var rayEnd = rayOrigin + direction * detectLength;
            //Debug.DrawLine(rayOrigin,rayEnd,Color.red);

            var raycast = new RaycastInput
            {
                Start = rayOrigin,
                End = rayEnd,
                Filter = new CollisionFilter
                {
                    BelongsTo = belongs,
                    CollidesWith = collideWith,
                    GroupIndex = 0
                }
            };
            if (physicsWorld.PhysicsWorld.CollisionWorld.CastRay(raycast, out var raycastHit))
            {
                hitEntity = physicsWorld.PhysicsWorld.Bodies[raycastHit.RigidBodyIndex].Entity;
                return true;
            }
            

            hitEntity = Entity.Null;
            return false;
        }

        
        public static void ResetMovableData(ref MovableData movableData)
        {
            movableData.MovementState = MovementState.NotMoving;
            movableData.MovementCommandType = MovementCommandType.None;
            movableData.DetailInfo = DetailInfo.None;
        }

        public static void ResetNavAgent(ref NavAgentComponent navAgentComponent)
        {
            navAgentComponent.ForceCalculate = false;
            navAgentComponent.EnableCalculation = false;
            navAgentComponent.CalculationComplete = false;
        }

        public static void ResetSurroundings(ref Surroundings surroundings)
        {
            surroundings.MoveSuccess = true;
            surroundings.IdealDirection = float3.zero;
            surroundings.FrontEntity = Entity.Null;
            surroundings.LeftEntity = Entity.Null;
            surroundings.RightEntity = Entity.Null;
            surroundings.CompromiseTimes = 0;
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float3 GetLeftOrRight(float3 direction, bool isLeft)
        {
            return isLeft ? new float3(-direction.z, 0, direction.x) : new float3(direction.z, 0, -direction.x);
        }
        
        /// <summary>
        /// Used for judging if point2 is left or right side of the point1 direction to dirFrom1To2
        /// </summary>
        /// <param name="point1"></param>
        /// <param name="point2"></param>
        /// <param name="dirFrom1To2"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool GetSide(float3 point1, float3 point2, float3 dirFrom1To2)
        {
            var toPoint = point2 - point1;
            var cross = dirFrom1To2.x * toPoint.z - dirFrom1To2.z * toPoint.x;
            return cross > 0;
        }
        
        
        
        public static float3 GetLeftRight30(float3 forward, bool isLeft)
        {
            if (isLeft)
            {
                var leftRotation = quaternion.AxisAngle(math.up(), math.radians(30f));
                return math.mul(leftRotation, forward);
            }
            var rightRotation = quaternion.AxisAngle(math.up(), math.radians(-30f));
            return math.mul(rightRotation, forward);
        }
    }
}