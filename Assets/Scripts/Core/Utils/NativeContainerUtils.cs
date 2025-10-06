using System;
using System.Runtime.CompilerServices;
using Unity.Collections;
using Unity.Entities;

namespace SparFlame.Core.Utils
{
    public class NativeContainerUtils
    {
        /// <summary>
        /// get MultiHashMap key i value。
        /// </summary>
        /// <param name="map">multi hashmap</param>
        /// <param name="key">target key</param>
        /// <param name="index">value index</param>
        /// <param name="value">output value</param>
        /// <returns>if success</returns>
        /*
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
        */


        /*
        public static void GetAllValuesForKey<TKey, TValue>(
            NativeParallelMultiHashMap<TKey, TValue> multiHashMap,
            ref NativeList<TValue> values,
            TKey key,
            int cutOffCount = -1)
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
        */
        
        /// <summary>
        /// 通用且安全：需要 T 实现 IEquatable&lt;T&gt;。
        /// Burst 兼容，无装箱、无 LINQ。
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool ContainsEq<T>( NativeList<T> list, in T value)
            where T : unmanaged, IEquatable<T>
        {
            var len = list.Length;
            for (var i = 0; i < len; i++)
            {
                if (value.Equals(list[i]))
                    return true;
            }
            return false;
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool ContainsEq<T>( FixedList128Bytes<T> list, in T value)
            where T : unmanaged, IEquatable<T>
        {
            var len = list.Length;
            for (var i = 0; i < len; i++)
            {
                if (value.Equals(list[i]))
                    return true;
            }
            return false;
        }
    }
}