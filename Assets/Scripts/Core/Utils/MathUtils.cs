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
            // Calculate how many grids it occupies in each direction
            var rawSizeX = (int)math.ceil(boxColliderSize.x / gridSize);
            var rawSizeZ = (int)math.ceil(boxColliderSize.z / gridSize);

            // Whether to rotate 90 degrees
            var rotated90 = math.abs(math.abs(math.round(rotationAngle) % 180) - 90) < 0.001f;
            
            var sizeX = rotated90 ? rawSizeZ : rawSizeX;
            var sizeZ = rotated90 ? rawSizeX : rawSizeZ;
            // if (rotated90)
            // {
            //     Debug.Log($"rawx {rawSizeX}, newx {sizeX}, rawz {rawSizeZ}, newz {sizeZ}");
            // }
            // Snap the position to half grid point, which is grid center
            var halfGrid = gridSize / 2f;

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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float2x2 GetRotationMatrix(in quaternion rotation)
        {
            var selfTransformForward = math.forward(rotation);
            var angleInRadians = math.atan2(selfTransformForward.x, selfTransformForward.z);
            var selfRotMat = new float2x2(math.cos(angleInRadians), -math.sin(angleInRadians),
                math.sin(angleInRadians), math.cos(angleInRadians));
            return selfRotMat;
        }
        
        public static void ObbDetect(in float2x2 selfRotMat, in float2x2 otherRotMat, float2 selfHalf,
            float2 otherHalf,
            float2 delta, out float minOverlap, out bool overlapped, out float2 smallestAxis)
        {
            var axes = new NativeArray<float2>(4, Allocator.Temp);
            axes[0] = selfRotMat.c0; // self local x
            axes[1] = selfRotMat.c1; // self local z
            axes[2] = otherRotMat.c0; // other local x
            axes[3] = otherRotMat.c1; // other local z

            smallestAxis = float2.zero;
            minOverlap = float.MaxValue;
            overlapped = true;

            for (var a = 0; a < 4; a++)
            {
                var axis = math.normalize(axes[a]);

                // 2 boxes project onto this axis
                var projSelf = math.abs(math.dot(axis, selfRotMat.c0)) * selfHalf.x +
                               math.abs(math.dot(axis, selfRotMat.c1)) * selfHalf.y;
                    
                var projOther = math.abs(math.dot(axis, otherRotMat.c0)) * otherHalf.x +
                                math.abs(math.dot(axis, otherRotMat.c1)) * otherHalf.y;

                var centerDist = math.abs(math.dot(axis, delta));

                var overlap = projSelf + projOther - centerDist;

                if (overlap < 0f)
                {
                    overlapped = false;
                    break;
                }

                if (overlap < minOverlap)
                {
                    minOverlap = overlap;
                    // A direction parallel to axis and point to other pos from self pos
                    smallestAxis = axis * math.sign(math.dot(axis, delta));
                }
            }
        }
    }
}