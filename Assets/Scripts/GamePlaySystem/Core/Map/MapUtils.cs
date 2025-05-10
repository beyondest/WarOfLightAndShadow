using System;
using System.Runtime.CompilerServices;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Random = Unity.Mathematics.Random;

namespace SparFlame.GamePlaySystem.Map
{
    public struct MapUtils
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsInsideGrid(in float3 gridPos, in float3 gridSize, in float3 curPos)
        {
            var xMax = gridPos.x + gridSize.x / 2;
            var zMax = gridPos.z + gridSize.z / 2;
            var xMin = gridPos.x - gridSize.x / 2;
            var yMin = gridPos.z - gridSize.z / 2;
            if (curPos.x < xMin || curPos.x > xMax
                                || curPos.z < yMin || curPos.z > zMax)
                return false;
            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsTileInCrystalRadius(in float3 gridCenterPos, in float3 crystalPos, float radiusSq,
            float tileSize)
        {
            var xDis = math.min(math.abs(gridCenterPos.x + tileSize/2f - crystalPos.x), 
                math.abs(gridCenterPos.x -tileSize/2f - crystalPos.x));
            var yDis = math.min(math.abs(gridCenterPos.z + tileSize/2f - crystalPos.z),
                math.abs(gridCenterPos.z -tileSize/2f - crystalPos.z));
            return xDis * xDis + yDis * yDis <= radiusSq;
        }

        public static float2 SampleSquareRing(float outerSize, float innerSize, float2 squareCenter, ref Unity.Mathematics.Random random)
        {
            var halfOuter = outerSize / 2f;
            var halfInner = innerSize / 2f;

            // 四个区域的面积
            float areaTopBottom = outerSize * (halfOuter - halfInner);        // 上下
            float areaLeftRight = (halfOuter - halfInner) * innerSize;        // 左右
            float totalArea = 2f * areaTopBottom + 2f * areaLeftRight;

            float pick = random.NextFloat(0f, totalArea);

            if (pick < areaTopBottom) // 上边
            {
                return squareCenter + new float2(
                    random.NextFloat(-halfOuter, halfOuter),
                    random.NextFloat(halfInner, halfOuter)
                );
            }
            else if (pick < 2f * areaTopBottom) // 下边
            {
                return squareCenter + new float2(
                    random.NextFloat(-halfOuter, halfOuter),
                    random.NextFloat(-halfOuter, -halfInner)
                );
            }
            else if (pick < 2f * areaTopBottom + areaLeftRight) // 左边
            {
                return squareCenter + new float2(
                    random.NextFloat(-halfOuter, -halfInner),
                    random.NextFloat(-halfInner, halfInner)
                );
            }
            else // 右边
            {
                return squareCenter + new float2(
                    random.NextFloat(halfInner, halfOuter),
                    random.NextFloat(-halfInner, halfInner)
                );
            }
        }


        public static MapLocType GetMapLocTypeByPos(float3 pos, float3 center, float outerSquareSize, float innerSquareSize,
            float radius)
        {
            var p = pos.xz; // 取平面坐标
            var local = p - center.xz;

            var halfL1 = outerSquareSize * 0.5f;
            var halfL2 = innerSquareSize * 0.5f;

            var absX = math.abs(local.x);
            var absY = math.abs(local.y);

            var distSq = math.lengthsq(local);

            //  判断是否在大正方形范围内（否则返回 0 或错误）
            if (absX > halfL1 || absY > halfL1)
                return MapLocType.None; // 不在地图内

            //  类型 1：中心圆形区域
            if (distSq <= radius * radius)
                return MapLocType.CentralCircle;

            //  判断是否在小正方形内
            if (absX <= halfL2 && absY <= halfL2)
            {
                switch (local)
                {
                    // 类型 2~5：小正方形 - 圆形部分，按象限划分
                    case { x: >= 0, y: >= 0 }:
                        return MapLocType.InnerSquareUpRight; // 右上
                    case { x: < 0, y: >= 0 }:
                        return MapLocType.InnerSquareUpLeft; // 左上
                    case { x: < 0, y: < 0 }:
                        return MapLocType.InnerSquareDownLeft; // 左下
                    case { x: >= 0, y: < 0 }:
                        return MapLocType.InnerSquareDownRight; // 右下
                }
            }

            //  类型 6：大正方形减小正方形区域（正方形环）
            return MapLocType.SquareRing;
        }

        public static void AddRandomizedTilesToBuffer(DynamicBuffer<MapLocTypeToTileType> buffer, ref Random rnd)
        {
            var types = new NativeArray<TileType>(4, Allocator.Temp)
            {
                [0] = TileType.Eldergrove,
                [1] = TileType.ObsidianExpanse,
                [2] = TileType.AetherPrairie,
                [3] = TileType.AncientRuins
            };

            // 2. Fisher-Yates Shuffle 
            for (var i = types.Length - 1; i > 0; i--)
            {
                var j = rnd.NextInt(0, i + 1);
                (types[i], types[j]) = (types[j], types[i]);
            }

            // 3. Add to buffer
            foreach (var t in types)
            {
                buffer.Add(new MapLocTypeToTileType { Type = t });
            }

            types.Dispose();
        }

        public static bool TryGetMapTileTransition(
            float3 pos, float3 center,
            float outerSquareSize, float innerSquareSize, float radius,
            float transitionLength,
            out MapLocType typeA, out MapLocType typeB, out float weightA,
            out TransitionType transitionType)
        {
            var p = pos.xz;
            var local = p - center.xz;

            var halfL1 = outerSquareSize * 0.5f;
            var halfL2 = innerSquareSize * 0.5f;

            var absX = math.abs(local.x);
            var absY = math.abs(local.y);
            var dist = math.length(local);

            typeA = MapLocType.None;
            typeB = MapLocType.None;
            weightA = 1f;
            transitionType = TransitionType.None;
            // 越界检查
            if (absX > halfL1 || absY > halfL1)
                return false;

            bool insideInner = absX <= halfL2 && absY <= halfL2;
            bool insideOuter = absX <= halfL1 && absY <= halfL1;

            // 🌟 1. 圆心与象限之间（0 vs 1~4）
            if (dist > radius - transitionLength/2f && dist <= radius + transitionLength/2f && insideInner)
            {
                typeA = MapLocType.CentralCircle;
                typeB = GetInnerQuadrantType(local.x, local.y);
                var t = (dist - radius - transitionLength/2f) / transitionLength;
                weightA = 1f - math.clamp(t, 0f, 1f);
                transitionType = TransitionType.RoundRing;
                return true;
            }

            // 🌟 2. 象限之间交界（2~5 相邻象限）
            if (insideInner && dist > radius + transitionLength)
            {
                // X方向交界：x 近 0
                if (math.abs(local.x) <= transitionLength/2f)
                {
                    if (local.y >= 0)
                    {
                        typeA = MapLocType.InnerSquareUpLeft;
                        typeB = MapLocType.InnerSquareUpRight;
                    }
                    else
                    {
                        typeA = MapLocType.InnerSquareDownLeft;
                        typeB = MapLocType.InnerSquareDownRight;
                    }

                    transitionType = TransitionType.Horizontal;
                    var t = (local.x - (-transitionLength / 2f)) / transitionLength;
                    weightA = 1f -  math.clamp(t, 0f, 1f);
                    return true;
                }

                // Y方向交界：y 近 0
                if (math.abs(local.y) <= transitionLength/2f)
                {
                    if (local.x >= 0)
                    {
                        typeA = MapLocType.InnerSquareDownRight;
                        typeB = MapLocType.InnerSquareUpRight;
                    }
                    else
                    {
                        typeA = MapLocType.InnerSquareDownLeft;
                        typeB = MapLocType.InnerSquareUpLeft;
                    }
                    transitionType = TransitionType.Vertical;
                    var t = (local.y - (-transitionLength / 2f)) / transitionLength;
                    weightA = 1f -  math.clamp(t, 0f, 1f);
                    return true;
                }
            }

            // 🌟 3. 象限与外部之间（2~5 vs 6）
            if (insideOuter &&
                (absX >= halfL2 - transitionLength/2f || absY >= halfL2 - transitionLength/2f) &&
                absX <= halfL2 + transitionLength/2f && absY <= halfL2 + transitionLength/2f)
            {
                
                typeA = GetInnerQuadrantType(local.x, local.y);
                typeB = MapLocType.SquareRing;

                float borderDist;
                // 正方形环左右两侧
                if (absX > halfL2 - transitionLength / 2f && absY < halfL2 - transitionLength / 2f)
                {
                    transitionType = TransitionType.Horizontal;
                    borderDist = absX - halfL2 - transitionLength / 2f;
                }
                // 正方形环上下两侧
                else if (absX < halfL2 - transitionLength / 2f && absY > halfL2 - transitionLength / 2f)
                {
                    transitionType = TransitionType.Vertical;
                    borderDist = absY - halfL2 - transitionLength / 2f;
                }
                // 正方形环4个角
                else if (absX >= halfL2 - transitionLength / 2f && absY >= halfL2 - transitionLength / 2f)
                {
                    transitionType = TransitionType.SquareRing;
                    // borderDist = math.max(absX - halfL2 - transitionLength / 2f, absY - halfL2 - transitionLength / 2f);
                    borderDist = math.length(new float2(absX - halfL2- transitionLength / 2f, absY - halfL2- transitionLength / 2f)) ;
                }
                else
                {
                    throw new ArgumentException("This should never happen");
                }
                var t = borderDist / transitionLength;
                weightA = 1f - math.clamp(t, 0f, 1f);
                return true;
            }

            return false;
        }

        private static MapLocType GetInnerQuadrantType(float x, float y)
        {
            if (x >= 0 && y >= 0) return MapLocType.InnerSquareUpRight; // 右上
            if (x < 0 && y >= 0) return MapLocType.InnerSquareUpLeft; // 左上
            if (x < 0 && y < 0) return MapLocType.InnerSquareDownLeft; // 左下
            return MapLocType.InnerSquareDownRight; // 右下
        }
    }
}