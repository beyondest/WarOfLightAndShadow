using System.Runtime.CompilerServices;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using UnityEngine;
using RaycastHit = Unity.Physics.RaycastHit;

namespace SparFlame.Components.SubGameplay
{
    public struct MovementUtils
    {
        #region MovableData, Surroundings, NavAgent Interface

        public static void ResetMovableData(ref MovableData movableData)
        {
            movableData.MovementState = MovementState.NotMoving;
            movableData.MovementCommandType = MovementCommandType.None;
            movableData.DetailInfo = DetailInfo.None;
        }

        /// <summary>
        /// Warning : interactRangeSq must be set in job if attack/heal/harvest move
        /// </summary>
        /// <param name="movableData"></param>
        /// <param name="targetPos"></param>
        /// <param name="targetColliderSize"></param>
        /// <param name="commandType"></param>
        /// <param name="interactRange"></param>
        public static void SetMoveTarget(ref MovableData movableData, float3 targetPos, float3 targetColliderSize,
            MovementCommandType commandType, float interactRange)
        {
            movableData.ForceCalculate = true;
            movableData.TargetCenterPos = targetPos;
            movableData.TargetColliderShape = targetColliderSize;
            movableData.MovementCommandType = commandType;
            movableData.InteractRange = interactRange;
            movableData.MovementState = MovementState.NotMoving;
            movableData.DetailInfo = DetailInfo.None;
        }

        public static void ResetNavAgent(ref NavAgentComponent navAgentComponent)
        {
            navAgentComponent.forceCalculate = false;
            navAgentComponent.enableCalculation = false;
            navAgentComponent.calculationComplete = false;
        }

        public static void ResetSurroundings(ref Surroundings surroundings)
        {
            surroundings.MoveSuccess = true;
            surroundings.FrontEntity = Entity.Null;
            surroundings.LeftEntity = Entity.Null;
            surroundings.RightEntity = Entity.Null;
            surroundings.CompromiseTimes = 0;
        }

        #endregion


        #region Physics detection or math methods

        public static bool RayCastToTerrainToGetNormal(ref PhysicsWorldSingleton physicsWorld,
            in float3 origin, float detectLength, uint colliderWith, uint belongs, out RaycastHit hit)
        {
            float3 direction = math.normalize(new float3(0f, -1f, 0f));

            // Raycast 输入
            RaycastInput rayInput = new RaycastInput
            {
                Start = origin,
                End = origin + direction * detectLength,
                Filter = new CollisionFilter
                {
                    BelongsTo = belongs,
                    CollidesWith = colliderWith,
                    GroupIndex = 0
                }
            };
            if (physicsWorld.CollisionWorld.CastRay(rayInput, out hit))
            {
                return true;
                /*if (math.lengthsq(normal) > 1e-6f)
                {
                    float3 nn = math.normalize(normal);
                    Debug.DrawLine(origin, origin + nn * length, Color.red);

                    // 箭头
                    float3 right = math.normalize(math.cross(nn, new float3(0.001f, 1f, 0.001f)));
                    if (math.lengthsq(right) < 1e-6f) right = new float3(1, 0, 0);
                    float3 up = math.normalize(math.cross(right, nn));

                    float headLen = 0.2f;
                    float headWidth = 0.08f;

                    Debug.DrawLine(origin + nn * length,
                        origin + nn * (length - headLen) + (up + right) * headWidth,
                        Color.red);
                    Debug.DrawLine(origin + nn * length,
                        origin + nn * (length - headLen) + (up - right) * headWidth,
                        Color.red);
                }*/
            }

            return false;
        }


        /// <summary>
        /// Will cast 2 rays in one direction, one is left corner ray, the other is right corner ray.
        /// </summary>
        /// <param name="physicsWorld"></param>
        /// <param name="curPos"></param>
        /// <param name="collideWith"></param>
        /// <param name="belongs"></param>
        /// <param name="direction">Detect ray direction</param>
        /// <param name="detectLength">the ray cast length</param>
        /// <param name="hitEntity"></param>
        /// <returns>Only when 2 rays hit nothing, will return false.
        /// Otherwise, return true, and hitEntity will be the left hit one or right hit one,
        /// depends on which side hits</returns>
        public static bool ObstacleInDirection(ref PhysicsWorldSingleton physicsWorld,
            float3 curPos, uint collideWith,
            uint belongs, float3 direction, float detectLength, out Entity hitEntity)
        {
            var rayOrigin = curPos + new float3(0, 0.1f, 0);
            var rayEnd = rayOrigin + direction * detectLength;
#if DEBUG
            Debug.DrawLine(rayOrigin, rayEnd, Color.red);
#endif

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


        /// <summary>
        /// This method calculates the min distance between pos and a rect with centerPos and size
        /// </summary>
        /// <param name="centerPos"></param>
        /// <param name="size"></param>
        /// <param name="pos"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float DistanceSqPointToRect(float2 centerPos, float2 size, float2 pos)
        {
            var halfSize = size * 0.5f;
            var min = centerPos - halfSize;
            var max = centerPos + halfSize;

            // this clamp method is what you know in scalar, and also works in vector
            var clampedPos = math.clamp(pos, min, max);
            return math.distancesq(pos, clampedPos);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float DistanceSqPointToBox(float3 centerPos, float3 size, float3 pos)
        {
            float3 halfSize = size * 0.5f;
            float3 min = centerPos - halfSize;
            float3 max = centerPos + halfSize;

            float3 clampedPos = math.clamp(pos, min, max);

            return math.distancesq(pos, clampedPos);
        }

        #endregion
    }
}