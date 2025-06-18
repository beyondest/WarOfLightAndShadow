using System;
using Unity.Collections;

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

    }
}