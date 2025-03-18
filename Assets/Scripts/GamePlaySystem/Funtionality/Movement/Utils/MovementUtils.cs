using System.Runtime.CompilerServices;
using Unity.Physics;
using Unity.Entities;
using Unity.Mathematics;


namespace SparFlame.GamePlaySystem.Movement
{
    public struct MovementUtils
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="physicsWorld"></param>
        /// <param name="colliderRadius">This is the collider radius of mover, not the one being detected</param>
        /// <param name="curPos"></param>
        /// <param name="collideWith"></param>
        /// <param name="belongs"></param>
        /// <param name="direction">Detect ray direction</param>
        /// <param name="detectLength">the ray cast length</param>
        /// <param name="hitEntity"></param>
        /// <returns></returns>
        public static bool ObstacleInDirection(ref PhysicsWorldSingleton physicsWorld, float colliderRadius,
            float3 curPos, uint collideWith,
            uint belongs, float3 direction, float detectLength, out Entity hitEntity)
        {
            var rayOrigin = curPos + direction * colliderRadius + new float3(0, 0.1f, 0);
            var rayEnd = rayOrigin + direction * detectLength;
            var raycastInput = new RaycastInput
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
            if (physicsWorld.PhysicsWorld.CollisionWorld.CastRay(raycastInput, out var raycastHit))
            {
                hitEntity = physicsWorld.PhysicsWorld.Bodies[raycastHit.RigidBodyIndex].Entity;
                return true;
            }
            else
            {
                hitEntity = Entity.Null;
                return false;
            }
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
            return cross > 0 ? true : false;
        }
    }
}