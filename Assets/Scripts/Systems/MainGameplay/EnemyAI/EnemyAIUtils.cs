using SparFlame.Components.General;
using SparFlame.Components.MainGameplay;
using SparFlame.Core.Structs;
using SparFlame.Core.Utils;
using Unity.Collections;
using Unity.Entities;

// ReSharper disable UseIndexFromEndExpression


public static class EnemyAIUtils
{
    // 为邻居存放的值类型
    private struct Neighbor
    {
        public int ID;
    }

    // directRoads: 所有直接连边（双向的话需要在传入的时候包含双向，或者方法内部插入双向）
    // startCityId: 起点 city id（与 directRoads 中使用的 cityId 空间一致）
    // cityFactionById: 长度至少覆盖所有 cityId 的数组，索引用 cityId -> factionId（注意：如果 cityId 不是连续索引，你需自己把 cityId 映射成索引；这里假定 cityId 可作为索引）
    // myFactionId: 我方 faction id（与 cityFactionById 中存放的一致）
    // playerCities: reachable player city
    // 约定：方法内部使用 Allocator.Temp 申请临时容器（调用者须在 Burst/JOB 内确保安全）
    public static void FindReachablePlayerCities(
        DynamicBuffer<DirectRoadPointData> directRoads,
        int startCityId,
        NativeParallelHashMap<int, Relationship> cityIdToPlayerRelations,
        NativeList<int> playerCities)
    {
        // 清空输出
        playerCities.Clear();

        if (directRoads.Length == 0) return;

        // 构建邻接（双向）：NativeParallelMultiHashMap<cityId, Neighbor>
        var capacity = directRoads.Length * 2;
        var adj = new NativeParallelMultiHashMap<int, Neighbor>(capacity, Allocator.Temp);

        foreach (var e in directRoads)
        {
            // 添加双向连接
            adj.Add(e.cityAId, new Neighbor
            {
                ID = e.cityBId
            });
            adj.Add(e.cityBId, new Neighbor
            {
                ID = e.cityAId
            });
        }

        // 结构：stack 用于 DFS（也可改为队列 BFS）
        var stack = new NativeList<int>(Allocator.Temp);
        var visited = new NativeHashSet<int>(capacity, Allocator.Temp);
        var playerCityIds = new NativeHashSet<int>(capacity, Allocator.Temp); // 用于去重敌方城市

        // push start
        stack.Add(startCityId);
        visited.Add(startCityId);

        while (stack.Length > 0)
        {
            var cur = stack[stack.Length - 1];
            stack.RemoveAtSwapBack(stack.Length - 1);

            foreach (var nb in adj.GetValuesForKey(cur))
            {
                var nid = nb.ID;
                // 如果已经访问过，则跳过
                if (visited.Contains(nid)) continue;

                // 先判断该节点的派系
                var cityRelationWithPlayer = cityIdToPlayerRelations[nid];
                if (cityRelationWithPlayer is Relationship.Self or Relationship.Ally)
                {
                    // player，记录并阻断 （不将 nid 压入 stack）
                    playerCityIds.Add(nid);
                    visited.Add(nid); // 标记已访问，避免重复处理
                }
                else
                {
                    // 是我方/可通行的城市，继续遍历
                    visited.Add(nid);
                    stack.Add(nid);
                }
            }
        }

        // 把 enemies 拷贝到 outEnemyCities（按需排序）
        foreach (var id in playerCityIds)
        {
            playerCities.Add(id);
        }

        // 释放临时容器
        playerCityIds.Dispose();
        visited.Dispose();
        stack.Dispose();
        adj.Dispose();
    }


    public static float EvaluateLevelAddThreatenValue(int level, float a, float b, float c)
    {
        if (level is < 1 or > 30)
            return 1f;

        var co = a;

        switch (level)
        {
            case >= 11 and <= 20:
                co *= c;
                break;
            case >= 21:
                co *= c * c;
                break;
        }

        return co * level + b;
    }


    public static void FindNearestCityToInvade(in DynamicBuffer<InvadeTarget> targets,
        NativeParallelHashMap<IntPair, float> distanceMap,
        int currentCityId, out Entity best, out float minDistance)
    {
        best = Entity.Null;
        minDistance = float.MaxValue;
        foreach (var e in targets)
        {
            var distance = distanceMap[new IntPair(currentCityId, e.CityPrefabId)];
            if (distance < minDistance)
            {
                minDistance = distance;
                best = e.City;
            }
        }
    }
}