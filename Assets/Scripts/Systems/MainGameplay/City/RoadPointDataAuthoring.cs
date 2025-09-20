using System.Collections.Generic;
using SparFlame.Components.MainGameplay;
using Unity.Entities;
using UnityEngine;

namespace SparFlame.Systems.MainGameplay.City
{
    public class RoadPointDataAuthoring : MonoBehaviour
    {
        public List<DirectRoadPointData> directRoadPoints;

        private class RoadPointDataAuthoringBaker : Baker<RoadPointDataAuthoring>
        {
            public override void Bake(RoadPointDataAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.None);
                var directDataBuffer = AddBuffer<DirectRoadPointData>(entity);
                foreach (var data in authoring.directRoadPoints)
                {
                    directDataBuffer.Add(data);
                }

                var roadPoints = RoadPointUtils.ComputeRoadPointData_List(authoring.directRoadPoints);
                var buffer = AddBuffer<RoadPointData>(entity);
                foreach (var roadPoint in roadPoints)
                {
                    buffer.Add(roadPoint);
                }
            }
        }
    }

    public static class RoadPointUtils
    {
        // 用于内部邻接列表
        private class Edge
        {
            public readonly int To;
            public readonly float W;

            public Edge(int t, float ww)
            {
                To = t;
                W = ww;
            }
        }

        // 计算所有 pairs 最短路径（如果可达），返回去重后的 RoadPointData 列表（CityAId < CityBId）
        public static List<RoadPointData> ComputeRoadPointData_List(List<DirectRoadPointData> directEdges)
        {
            // 收集所有 city id 并建立映射到索引
            var citySet = new HashSet<int>();
            foreach (var e in directEdges)
            {
                citySet.Add(e.cityAId);
                citySet.Add(e.cityBId);
            }

            var cityIds = new List<int>(citySet);
            cityIds.Sort(); // 可选：决定索引顺序
            var idToIndex = new Dictionary<int, int>(cityIds.Count);
            for (int i = 0; i < cityIds.Count; ++i) idToIndex[cityIds[i]] = i;

            int n = cityIds.Count;
            var adj = new List<Edge>[n];
            for (int i = 0; i < n; ++i) adj[i] = new List<Edge>();

            foreach (var e in directEdges)
            {
                int a = idToIndex[e.cityAId];
                int b = idToIndex[e.cityBId];
                float w = e.directDistance;
                // 假设道路双向，如是单向请仅添加单向
                adj[a].Add(new Edge(b, w));
                adj[b].Add(new Edge(a, w));
            }

            var results = new List<RoadPointData>();

            // Dijkstra from each node
            for (int src = 0; src < n; ++src)
            {
                var dist = new float[n];
                for (int i = 0; i < n; ++i) dist[i] = float.PositiveInfinity;
                dist[src] = 0f;

                // 简单二进制堆优先队列实现
                var pq = new SimpleMinHeap();
                pq.Push(src, 0f);

                while (pq.Count > 0)
                {
                    var top = pq.Pop();
                    int u = top.Index;
                    float d = top.Dist;
                    if (d > dist[u]) continue;

                    foreach (var edge in adj[u])
                    {
                        int v = edge.To;
                        float nd = d + edge.W;
                        if (nd < dist[v])
                        {
                            dist[v] = nd;
                            pq.Push(v, nd);
                        }
                    }
                }

                // 收集结果，只保存 CityAId < CityBId 避免重复
                for (int tgt = src + 1; tgt < n; ++tgt)
                {
                    if (float.IsInfinity(dist[tgt])) continue;
                    var rp = new RoadPointData
                    {
                        CityAId = cityIds[src],
                        CityBId = cityIds[tgt],
                        TotalDistance = dist[tgt]
                    };
                    results.Add(rp);
                }
            }

            return results;
        }

        // ---- 简单最小堆（用于 Dijkstra） ----
        class HeapNode
        {
            public readonly int Index;
            public readonly float Dist;

            public HeapNode(int i, float d)
            {
                Index = i;
                Dist = d;
            }
        }

        class SimpleMinHeap
        {
            private readonly List<HeapNode> data = new List<HeapNode>();
            public int Count => data.Count;

            public void Push(int idx, float dist)
            {
                data.Add(new HeapNode(idx, dist));
                int i = data.Count - 1;
                while (i > 0)
                {
                    int p = (i - 1) / 2;
                    if (data[p].Dist <= data[i].Dist) break;
                    var tmp = data[p];
                    data[p] = data[i];
                    data[i] = tmp;
                    i = p;
                }
            }

            public HeapNode Pop()
            {
                var ret = data[0];
                int last = data.Count - 1;
                data[0] = data[last];
                data.RemoveAt(last);
                int i = 0;
                while (true)
                {
                    int l = 2 * i + 1;
                    int r = l + 1;
                    int smallest = i;
                    if (l < data.Count && data[l].Dist < data[smallest].Dist) smallest = l;
                    if (r < data.Count && data[r].Dist < data[smallest].Dist) smallest = r;
                    if (smallest == i) break;
                    var tmp = data[i];
                    data[i] = data[smallest];
                    data[smallest] = tmp;
                    i = smallest;
                }

                return ret;
            }
        }
    }
}