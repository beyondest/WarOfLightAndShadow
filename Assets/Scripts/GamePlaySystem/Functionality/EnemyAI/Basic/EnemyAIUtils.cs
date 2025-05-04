using System.Runtime.CompilerServices;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
// ReSharper disable UseIndexFromEndExpression

namespace SparFlame.GamePlaySystem.EnemyAI
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
            foreach (var pair in config.proPairs)
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
        public static TargetLocPair ChooseTargetAndTryRemove(NativeList<TargetLocPair> pairs)
        {
            var pair = pairs[pairs.Length - 1];
            if(pairs.Length > 1)
                pairs.RemoveAt(pairs.Length - 1);
            return pair;
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void ResetPendingCommand(ref EnemyUnitCommandData commandData)
        {
            commandData.TargetEntity = Entity.Null;
            commandData.CommandType = EnemyCommandType.None;
            commandData.TargetPos = float3.zero;
            commandData.Focus = false;
        }
    }
}