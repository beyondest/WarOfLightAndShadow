using System.Runtime.CompilerServices;
using SparFlame.Components.SubGameplay;
using SparFlame.Systems.EnemyAI;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

// ReSharper disable UseIndexFromEndExpression

namespace SparFlame.Systems.SubGameplay.EnemyAI
{
    public struct EnemyAIUtils
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float3 GetRandomPointInRange(float3 origin, float minDistance, float maxDistance, ref Random rnd)
        {
            float angle = rnd.NextFloat(0f, math.PI * 2f);
            float distance = rnd.NextFloat(minDistance, maxDistance);
            float3 offset = new float3(math.cos(angle), 0f, math.sin(angle)) * distance;
            return origin + offset;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float3 GetRandomPointOnCircle(float3 origin, float distance, ref Unity.Mathematics.Random rnd)
        {
            float angle = rnd.NextFloat(0f, math.PI * 2f);
            float3 offset = new float3(math.cos(angle), 0f, math.sin(angle)) * distance;

            return origin + offset;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void AddToMapByTotalValue(in float3 pos, Entity target, float totalValue,
            float goodLower, float normalLower, NativeParallelMultiHashMap<int, TargetLocPair> map)
        {
            var pair = new TargetLocPair
            {
                Target = target,
                Location = pos
            };
            if (totalValue >= goodLower)
            {
                map.Add((int)TargetValueType.Good, pair);
            }
            else
            {
                if (totalValue >= normalLower)
                    map.Add((int)TargetValueType.Normal, pair);
                else map.Add((int)TargetValueType.Bad, pair);
            }
        }

        public static bool ChooseTargetValueTypeRandomly(ref Random rnd,
            in EnemyTeamAssignTargetConfig config,
            NativeParallelMultiHashMap<int, TargetLocPair> map,
            out TargetValueType valueType)
        {
            valueType = TargetValueType.Good;
            if (map.IsEmpty) return false;
            var pick = rnd.NextFloat(0f, 1f);
            var targetValueType = TargetValueType.Good;
            foreach (var pair in config.ProPairs)
            {
                if (pick < pair.prob)
                {
                    targetValueType = pair.valueType;
                    break;
                }
            }

            if (map.ContainsKey((int)targetValueType))
            {
                valueType = targetValueType;
                return true;
            }

            var find = 0;
            while (find < config.TargetValueTypeCount)
            {
                if (map.ContainsKey(find))
                {
                    valueType = (TargetValueType)(find);
                    return true;
                }

                find++;
            }

            return false;
        }


        /// <summary>
        /// Only remove target when list lenght > 1
        /// </summary>
        /// <param name="pairs"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T ChooseTargetAndTryRemove<T>(NativeList<T> pairs)
            where T : unmanaged
        {
            var pair = pairs[pairs.Length - 1];
            if (pairs.Length > 1)
                pairs.RemoveAt(pairs.Length - 1);
            return pair;
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void ResetPendingCommand(ref AIUnitCommandData commandData)
        {
            commandData.TargetEntity = Entity.Null;
            commandData.CommandType = AICommandType.None;
            commandData.TargetPos = float3.zero;
            commandData.Focus = false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsOverlapping(in float2 newPos,in float2 newSize,in NativeList<LocalTransform> existingLocs,
           in NativeList<BuildingPackSquareSize> existingSizes)
        {
            for (int i = 0; i < existingLocs.Length; i++)
            {
                var existingPos = existingLocs[i].Position.xz;
                var existingSize = existingSizes[i].Value;

                if (math.abs(newPos.x - existingPos.x) < (newSize.x + existingSize.x) * 0.5f &&
                    math.abs(newPos.y - existingPos.y) < (newSize.y + existingSize.y) * 0.5f)
                {
                    return true; // 有重叠
                }
            }

            return false;
        }
    }
}