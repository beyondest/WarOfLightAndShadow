using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Random = Unity.Mathematics.Random;

// ReSharper disable UseIndexFromEndExpression

namespace SparFlame.GamePlaySystem.General
{
    public struct GeneralUtils
    {
        /// <summary>
        /// get MultiHashMap key i value。
        /// </summary>
        /// <param name="map">multi hashmap</param>
        /// <param name="key">target key</param>
        /// <param name="index">value index</param>
        /// <param name="value">output value</param>
        /// <returns>if success</returns>
        public static bool TryGetValueAt<TKey, TValue>(
            NativeParallelMultiHashMap<TKey, TValue> map,
            TKey key,
            int index,
            out TValue value)
            where TKey : unmanaged, IEquatable<TKey>
            where TValue : unmanaged
        {
            value = default;

            if (!map.TryGetFirstValue(key, out var current, out var it))
                return false;

            int i = 0;
            while (i < index)
            {
                if (!map.TryGetNextValue(out current, ref it))
                    return false;
                i++;
            }

            value = current;
            return true;
        }


        public static void GetAllValuesForKey<TKey, TValue>(
            NativeParallelMultiHashMap<TKey, TValue> multiHashMap,
            ref NativeList<TValue> values,
            TKey key,
            int cutOffCount)
            where TKey : unmanaged, IEquatable<TKey>
            where TValue : unmanaged
        {
            switch (cutOffCount)
            {
                case 0:
                    return;
                case -1:
                    cutOffCount = int.MaxValue;
                    break;
            }

            var count = 0;
            if (multiHashMap.TryGetFirstValue(key, out var value, out var iterator))
            {
                do
                {
                    values.Add(value);
                    count++;
                } while (count < cutOffCount && multiHashMap.TryGetNextValue(out value, ref iterator));
            }
        }


        /// <summary>
        /// E.g. cur wave point is 3, settings is 2, 4, 6, then cur point data is data of settings 4
        /// </summary>
        /// <param name="curPoint"></param>
        /// <param name="buffer"></param>
        /// <typeparam name="TPointData"></typeparam>
        /// <typeparam name="TData"></typeparam>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static TData GetPointData<TPointData, TData>(float curPoint, DynamicBuffer<TPointData> buffer)
            where TPointData : unmanaged, IPointsData<TData>
            where TData : unmanaged
        {
            foreach (var t in buffer)
            {
                if (t.Points > curPoint)
                {
                    return t.Value;
                }
            }

            return buffer[buffer.Length - 1].Value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static TData GetPointData<TPointData, TData>(float curTimeFromGameStart, IList<TPointData> buffer)
            where TPointData : unmanaged, IPointsData<TData>
            where TData : unmanaged
        {
            foreach (var t in buffer)
            {
                if (t.Points > curTimeFromGameStart)
                {
                    return t.Value;
                }
            }

            return buffer[buffer.Count - 1].Value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int GetPoint(int curPoint, NativeList<int> list)
        {
            foreach (var t in list)
            {
                if (t > curPoint)
                {
                    return t;
                }
            }

            return list[list.Length - 1];
        }

        // [MethodImpl(MethodImplOptions.AggressiveInlining)]
        // public static TData GetPointData<TData>(float curTimeFromGameStart,NativeHashMap<int, TData> dict)
        //     where TData : unmanaged
        // {
        //     TData result = default;
        //     var list = new NativeList<int>();
        //     dict.
        //     foreach (var key in list)
        //     {
        //         if (key > curTimeFromGameStart)
        //         {
        //             return ;
        //         }
        //         result = key.Value;
        //     }
        //     return result;
        // }


        public static ProbabilityPrefabEntry RandomChoosePrefab(ref Unity.Mathematics.Random random,
            NativeParallelMultiHashMap<int, ProbabilityPrefabEntry> map,
            int key)
        {
            if (!map.TryGetFirstValue(key, out var firstEntry, out var it))
            {
                throw new ArgumentException($"key {key} not found in Resource prefab database");
            }

            var pick = random.NextFloat(0f, 1f);
            // Debug.Log($"[Key:{key}] Pick = {pick}, FirstEntryProb = {firstEntry.Probability}");
            var accum = firstEntry.Probability;
            if (pick <= accum)
            {
                return firstEntry;
            }

            var fallBack = firstEntry;
            while (map.TryGetNextValue(out var entry, ref it))
            {
                accum += entry.Probability;
                fallBack = entry;
                if (pick <= accum)
                {
                    // Debug.Log($"[Key:{key}] Hit = {entry}, Accum = {accum}");
                    return entry;
                }
            }

            // Debug.Log($"[Key:{key}] Fallback! Pick = {pick}, TotalAccum = {accum}");
            return fallBack;
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
                float angle = random.NextFloat(0f, math.PI * 2f);
                return quaternion.RotateY(angle);
            }
            else
            {
                float3 axis = math.normalize(random.NextFloat3Direction());
                float angle = random.NextFloat(0f, math.PI * 2f);
                return quaternion.AxisAngle(axis, angle);
            }
        }
    }
}