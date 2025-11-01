/*using System;
using Unity.Collections;
using Unity.Entities;
using Range = SparFlame.Core.Structs.Range;

namespace SparFlame.Core.Utils
{
    public static class PrefabEntryUtils
    {
        public struct ProbabilityPrefabEntry
        {
            public Entity Prefab;
            public float Probability;
            public Range AmountRange;
        }
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
    }
}*/