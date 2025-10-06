using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;
using Random = Unity.Mathematics.Random;

namespace SparFlame.Core.Utils
{
    public static class MathUtils
    {
        public static float2 Get2D(ref Random rng)
        {
            float angle = rng.NextFloat(0f, math.PI * 2f);
            return new float2(math.cos(angle), math.sin(angle));
        }
        public static float3 Get3D(ref Random rng)
        {
            float z = rng.NextFloat(-1f, 1f);         // cosθ
            float theta = rng.NextFloat(0f, math.PI * 2f); // φ

            float r = math.sqrt(1f - z * z);
            float x = r * math.cos(theta);
            float y = r * math.sin(theta);

            return new float3(x, y, z);
        }
        public static List<Vector2> GenerateCirclePoints(Vector2 center, float radius, int pointCount)
        {
            var points = new List<Vector2>();

            var angleStep = 360f / pointCount;

            for (var i = 0; i < pointCount; i++)
            {
                var angleInDegrees = i * angleStep;
                var angleInRadians = angleInDegrees * Mathf.Deg2Rad;

                var x = center.x + radius * Mathf.Cos(angleInRadians);
                var y = center.y + radius * Mathf.Sin(angleInRadians);

                var point = new Vector2(x, y);
                points.Add(point);
            }

            return points;
        }


        /// <summary>
        /// Returns an interval [lower, upper] with a range width of l, wrapping x, and the position of x in the interval is random
        /// </summary>
        public static (float lower, float upper) GenerateRandomBoundsAround(float x, float l,
            Unity.Mathematics.Random rng)
        {
            var t = rng.NextFloat(0f, 1f);
            var lower = x - l * t;
            var upper = lower + l;

            return (lower, upper);
        }


        public static List<TGet> GetChildrenFromMatching<TGet, TItems>(
            List<TItems> sourceList,
            Func<TItems, bool> condition,
            Func<TItems, TGet> childSelector)
        {
            var result = new List<TGet>();

            foreach (var item in sourceList)
            {
                if (condition(item))
                {
                    var childrenComponents = childSelector(item);
                    if (childrenComponents != null)
                    {
                        result.Add(childrenComponents); // 保留顺序
                    }
                }
            }

            return result;
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static uint GetSeedByIndexTimeBias(int index, int seedBias, float elapsedTime)
        {
            return math.hash(new int2(index + seedBias, (int)(elapsedTime * 10000)));
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static quaternion NextQuaternion(ref Random random, bool onlyXZ = true)
        {
            if (onlyXZ)
            {
                var angle = random.NextFloat(0f, math.PI * 2f);
                return quaternion.RotateY(angle);
            }
            else
            {
                var axis = math.normalize(random.NextFloat3Direction());
                var angle = random.NextFloat(0f, math.PI * 2f);
                return quaternion.AxisAngle(axis, angle);
            }
        }

        public static void GetSnapGridPosition(in float3 hitPosition, float rotationAngle,
            float3 boxColliderSize, float gridSize, out float3 gridPosition)
        {
            // 1. 计算旋转后占用格子数
            var rawSizeX = (int)math.ceil(boxColliderSize.x / gridSize);
            var rawSizeZ = (int)math.ceil(boxColliderSize.z / gridSize);

            // 是否旋转90/270度（调换X和Z）
            var rotated90 = math.abs(math.abs(math.round(rotationAngle) % 180) - 90) < 0.001f;


            var sizeX = rotated90 ? rawSizeZ : rawSizeX;
            var sizeZ = rotated90 ? rawSizeX : rawSizeZ;
            // if (rotated90)
            // {
            //     Debug.Log($"rawx {rawSizeX}, newx {sizeX}, rawz {rawSizeZ}, newz {sizeZ}");
            // }
            // 2. 对齐方式：使得坐标落在合法中心点上
            var halfGrid = gridSize / 2f;

            // 计算 snappedX（如果是偶数格，就落在偶数 * halfGrid，如果是奇数格，就落在奇数 * halfGrid）
            var xRaw = math.floor(hitPosition.x / halfGrid) * halfGrid;
            var zRaw = math.floor(hitPosition.z / halfGrid) * halfGrid;

            var xSnapped = ((sizeX % 2 == 0)
                ? math.round(xRaw / gridSize) * gridSize
                : math.round((xRaw - halfGrid) / gridSize) * gridSize + halfGrid);

            var zSnapped = ((sizeZ % 2 == 0)
                ? math.round(zRaw / gridSize) * gridSize
                : math.round((zRaw - halfGrid) / gridSize) * gridSize + halfGrid);

            gridPosition = new float3(xSnapped, hitPosition.y, zSnapped);
        }

        public static int Hash(string str)
        {
            FixedString64Bytes string64 = str;
            return string64.GetHashCode();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float3 GetRandomPointInRange(float3 origin, float minDistance, float maxDistance, ref Random rnd)
        {
            var angle = rnd.NextFloat(0f, math.PI * 2f);
            var distance = rnd.NextFloat(minDistance, maxDistance);
            var offset = new float3(math.cos(angle), 0f, math.sin(angle)) * distance;
            return origin + offset;
        }
    }
}