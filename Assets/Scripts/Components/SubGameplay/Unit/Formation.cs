using System;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace SparFlame.Components.SubGameplay
{
    public enum StandPositionType
    {
        Front,
        Middle,
        Back,
        Side,
    }

    public enum FormationShape
    {
        Square,
        Triangle
    }

    public struct FormationTransform : IComponentData
    {
        public LocalTransform Transform;
    }

    public struct FormationMovingTag : IComponentData, IEnableableComponent
    {
        
    }

    [Serializable]
    public struct FormationConfig : IComponentData
    {
        /// <summary>
        /// 判断邻居的半径。点之间的距离小于等于该值时，认为在同一簇。
        /// </summary>
        public float modeCenterRadius;

        public float squareSpacing;
        public float triangleSpacing;
    }



    public static class FormationUtils
    {
        /// <summary>
        /// 从一组坐标中找出 "众数中心" —— 即邻居最多的那个点。
        /// 该点必须是输入集中的点本身。
        /// </summary>
        public static float3 FindModeCenter(NativeList<float3> points, in FormationConfig config)
        {
            if (points.Length == 0)
                return float3.zero;

            int bestIndex = 0;
            int bestCount = -1;
            float bestTotalDist = float.MaxValue;

            float radius = config.modeCenterRadius > 0 ? config.modeCenterRadius : 1f;
            float radiusSq = radius * radius;

            for (int i = 0; i < points.Length; i++)
            {
                float3 pi = points[i];
                int count = 0;
                float totalDist = 0f;

                for (int j = 0; j < points.Length; j++)
                {
                    if (i == j) continue;
                    float3 pj = points[j];
                    float3 d = pi - pj;
                    float distSq = math.lengthsq(d);

                    // if (config.UseSqrDistance)
                    // {
                        if (distSq <= radiusSq)
                        {
                            count++;
                            totalDist += distSq;
                        }
                    // }
                    // else
                    // {
                    //     float dist = math.sqrt(distSq);
                    //     if (dist <= radius)
                    //     {
                    //         count++;
                    //         totalDist += dist;
                    //     }
                    // }
                }

                // 选：邻居最多；若并列，选总距离最小
                if (count > bestCount || (count == bestCount && totalDist < bestTotalDist))
                {
                    bestIndex = i;
                    bestCount = count;
                    bestTotalDist = totalDist;
                }
            }

            return points[bestIndex];
        }
        /// <summary>
        /// 生成阵型目标 LocalTransform 列表（位置 + 朝向）。
        /// 返回的 NativeList 由调用方负责 Dispose().
        /// </summary>
        /// <param name="center">阵型中心（世界坐标）</param>
        /// <param name="direction">二维方向 (x,z) —— y 忽略，朝向向量为 float3(direction.x,0,direction.y)</param>
        /// <param name="shape">阵型形状（Square / Triangle）</param>
        /// <param name="unitCount">单位数量</param>
        /// <param name="spacing">单位间距（默认 1）</param>
        /// <param name="allocator">NativeList 分配器</param>
        public static NativeList<LocalTransform> GenerateFormation(
            float3 center,
            float2 direction,
            FormationShape shape,
            int unitCount,
            float spacing = 1f,
            Allocator allocator = Allocator.Temp)
        {
            var list = new NativeList<LocalTransform>(math.max(0, unitCount), allocator);

            if (unitCount <= 0)
                return list;

            // 计算朝向（forward）与右方向（right），y向上
            float3 forward = new float3(direction.x, 0f, direction.y);
            if (math.lengthsq(forward) < 1e-6f)
                forward = new float3(0f, 0f, 1f); // fallback
            forward = math.normalize(forward);
            float3 up = new float3(0f, 1f, 0f);
            float3 right = math.normalize(math.cross(up, forward)); // right = up x forward
            // quaternion 朝向（使实体面向 forward）
            quaternion rot = quaternion.LookRotationSafe(forward, up);

            switch (shape)
            {
                case FormationShape.Square:
                    GenerateSquare(center, forward, right, rot, unitCount, spacing, ref list);
                    break;
                case FormationShape.Triangle:
                    GenerateTriangle(center, forward, right, rot, unitCount, spacing, ref list);
                    break;
                default:
                    GenerateSquare(center, forward, right, rot, unitCount, spacing, ref list);
                    break;
            }

            return list;
        }

        // Square: 最简单的方阵，按行优先填充（从后向前以 center 为中点对称）
        static void GenerateSquare(
            float3 center,
            float3 forward,
            float3 right,
            quaternion rot,
            int unitCount,
            float spacing,
            ref NativeList<LocalTransform> outList)
        {
            int gridSize = (int)math.ceil(math.sqrt((float)unitCount));
            // 中心偏移，使阵列以 center 为几何中心
            float halfOffset = (gridSize - 1) * 0.5f * spacing;

            int placed = 0;
            for (int r = 0; r < gridSize && placed < unitCount; r++)
            {
                for (int c = 0; c < gridSize && placed < unitCount; c++)
                {
                    float3 offset = (c * spacing - halfOffset) * right + (r * spacing - halfOffset) * forward;
                    float3 pos = center + offset;
                    var lt = LocalTransform.FromPositionRotationScale(pos, rot, 1f);
                    outList.Add(lt);
                    placed++;
                }
            }
        }

        // Triangle: 等腰三角（第一行 1 个，第二行 2 个，...），每行水平中心对齐，底朝向 forward 的反方向（或你想要的）
        static void GenerateTriangle(
            float3 center,
            float3 forward,
            float3 right,
            quaternion rot,
            int unitCount,
            float spacing,
            ref NativeList<LocalTransform> outList)
        {
            // 计算需要多少行 r 满足 1 + 2 + ... + r >= unitCount  => r*(r+1)/2 >= unitCount
            int r = (int)math.floor((math.sqrt(8f * unitCount + 1f) - 1f) * 0.5f);
            if (r * (r + 1) / 2 < unitCount) r++;

            // 总行数 r，计算三角形的几何高度偏移（以使 center 位于几何中心）
            // 先计算每行宽度（单位数）和每行中心位置，然后用 center 做对齐
            // 计算第一行（最前面）在 forward 方向上的起始偏移，使整个三角形以 center 为中心
            // 找到行索引的中心位置（对称轴）
            // 我们把行 0 放在前端（最靠 forward 的位置），行 r-1 放在后端（靠近 -forward）
            // 计算 vertical span (沿 forward 的长度)
            float frontRowIndex = 0;
            float backRowIndex = r - 1;
            float verticalSpan = (backRowIndex - frontRowIndex) * spacing;
            float frontToCenter = verticalSpan * 0.5f; // 从最前面行到几何中心的偏移

            int placed = 0;
            for (int row = 0; row < r && placed < unitCount; row++)
            {
                int itemsInRow = row + 1; // 1,2,3,...
                // 行在 forward 方向上的偏移（让三角形以几何中心对齐）
                float rowForwardOffset = (row * spacing) - frontToCenter;
                // 每行的水平（right 方向）偏移使该行居中：宽度 = (itemsInRow -1)*spacing, half = ...
                float rowHalfWidth = (itemsInRow - 1) * 0.5f * spacing;

                for (int i = 0; i < itemsInRow && placed < unitCount; i++)
                {
                    float colRightOffset = i * spacing - rowHalfWidth;
                    float3 offset = right * colRightOffset + forward * rowForwardOffset;
                    float3 pos = center + offset;
                    var lt = LocalTransform.FromPositionRotationScale(pos, rot, 1f);
                    outList.Add(lt);
                    placed++;
                }
            }

            // 如果 unitCount 超过 r*(r+1)/2（这里不会，因为我们按 r 算好），会提前停止
        }
    }
}