using System;
using System.Collections.Generic;
using NUnit.Framework;
using SparFlame.GamePlaySystem.Fow;
using SparFlame.GamePlaySystem.General;
using SparFlame.GamePlaySystem.Resource;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using Object = UnityEngine.Object;

namespace SparFlame.GamePlaySystem.CustomParticleSystem.LightLine
{
    public struct UpdateLightLineRequest : IComponentData
    {
    }


    public struct ValidTargetPair : IComponentData
    {
        public float3 Pos;
        public Entity Entity;
    }

    public partial class LightLineTransferSystem : SystemBase
    {
        private EntityQuery _lightBeaconsQuery;
        private EntityQuery _updateLightLineRequestQuery;
        private EntityQuery _lightLineSightQuery;
        private GameObject _lightLinePrefab;
        private float3 _centerPos;
        private float _radiusSq;
        private float _lineHeight;
        private Entity _sightPrefab;
        private float _sightInterval;


        protected override void OnCreate()
        {
            RequireForUpdate<GamingTag>();
            RequireForUpdate<LightSingleCrystalTag>();
            _updateLightLineRequestQuery = SystemAPI.QueryBuilder().WithAll<UpdateLightLineRequest>().Build();
            _lightLineSightQuery = SystemAPI.QueryBuilder().WithAll<LightLineSightTag>().Build();
        }

        protected override void OnStartRunning()
        {
            _lightLinePrefab = LightLineController.Instance.lightLinePrefab;
            _centerPos = SystemAPI.GetComponent<LocalTransform>(SystemAPI.GetSingletonEntity<LightSingleCrystalTag>())
                .Position;
            _lineHeight = LightLineController.Instance.lightLineHeight;
            var config = SystemAPI.GetSingleton<LightLineConfig>();
            _radiusSq = config.LightLineMaxDisSq;
            _sightPrefab = config.SightPrefab;
            _sightInterval = config.SightInterval;

            var playerFaction = SystemAPI.GetSingleton<PlayerFactionData>().Value;
            if (playerFaction == FactionTag.Ally)
            {
                _lightBeaconsQuery = SystemAPI.QueryBuilder().WithAll<CoreCrystalTag>()
                    .WithAll<LocalTransform>().WithNone<LightSingleCrystalTag>().WithAll<PlayerTag>()
                    .WithNone<BlindnessTag>().Build();
            }
            else
            {
                _lightBeaconsQuery = SystemAPI.QueryBuilder().WithAll<CoreCrystalTag>()
                    .WithAll<LocalTransform>().WithNone<LightSingleCrystalTag>().WithAll<AITag>()
                    .WithNone<BlindnessTag>().Build();
            }
            UpdateLightLine();
        }

        protected override void OnUpdate()
        {
            if (_updateLightLineRequestQuery.IsEmpty) return;
            foreach (var entity in _updateLightLineRequestQuery.ToEntityArray(Allocator.Temp))
            {
                EntityManager.DestroyEntity(entity);
            }

            UpdateLightLine();
        }

        private void UpdateLightLine()
        {
            var validTargets = new List<ValidTargetPair>();
            var beaconEntities = _lightBeaconsQuery.ToEntityArray(Allocator.Temp);
            var trans = _lightBeaconsQuery.ToComponentDataArray<LocalTransform>(Allocator.Temp);
            for (var i = 0; i < trans.Length; i++)
            {
                var transform = trans[i];
                validTargets.Add(new ValidTargetPair
                {
                    Entity = beaconEntities[i],
                    Pos = transform.Position
                });
            }

            validTargets.Add(new ValidTargetPair
            {
                Entity = SystemAPI.GetSingletonEntity<LightSingleCrystalTag>(),
                Pos = _centerPos
            });
            // 将中心点加入可用列表

            var reachable = new HashSet<float3>();
            var reachableEntities = new List<Entity>();

            var connections = new HashSet<(float3, float3)>();

            // -----------------------------
            // Step 1: 从 centerPos 向外进行 BFS 扩展
            // -----------------------------
            var queue = new Queue<float3>();
            queue.Enqueue(_centerPos);
            reachable.Add(_centerPos);

            while (queue.Count > 0)
            {
                var current = queue.Dequeue();
                foreach (var target in validTargets)
                {
                    if (reachable.Contains(target.Pos)) continue;
                    if (math.distancesq(current, target.Pos) <= _radiusSq)
                    {
                        reachable.Add(target.Pos);
                        reachableEntities.Add(target.Entity);
                        queue.Enqueue(target.Pos);
                        connections.Add(NormalizePair(current, target.Pos));
                    }
                }
            }

            // -----------------------------
            // Step 2: 创建或更新有效光路
            // -----------------------------
            var lightLineMap = LightLineController.Instance.CurrentLines;

            foreach (var conn in connections)
            {
                var from = conn.Item1;
                var to = conn.Item2;

                if (!lightLineMap.ContainsKey(conn))
                {
                    var go = Object.Instantiate(_lightLinePrefab);
                    go.GetComponent<LineRenderer>().SetPositions(new Vector3[]
                    {
                        new Vector3(from.x, _lineHeight, from.z),
                        new Vector3(to.x, _lineHeight, to.z)
                    });
                    lightLineMap[conn] = go;
                }
                else
                {
                    lightLineMap[conn].GetComponent<LineRenderer>().SetPositions(new Vector3[]
                    {
                        new Vector3(from.x, _lineHeight, from.z),
                        new Vector3(to.x, _lineHeight, to.z)
                    });
                }
            }

            // -----------------------------
            // Step 3: 清理不再连接的光路
            // -----------------------------
            var keysToRemove = new List<(float3, float3)>();
            foreach (var kvp in lightLineMap)
            {
                if (!connections.Contains(kvp.Key))
                {
                    Object.Destroy(kvp.Value);
                    keysToRemove.Add(kvp.Key);
                }
            }

            foreach (var key in keysToRemove)
                lightLineMap.Remove(key);


            // Generate sight alone the road, clear previous sight
            var ecb = new EntityCommandBuffer(Allocator.Temp);

            var entities = _lightLineSightQuery.ToEntityArray(Allocator.Temp);
            foreach (var e in entities)
            {
                ecb.DestroyEntity(e);
            }

            foreach (var kvp in lightLineMap)
            {
                float3 from = kvp.Key.Item1;
                float3 to = kvp.Key.Item2;

                float3 direction = math.normalize(to - from);
                float totalLength = math.distance(from, to);
                int pointCount = (int)(totalLength / _sightInterval);

                for (int i = 1; i < pointCount; i++)
                {
                    float dist = i * _sightInterval;
                    float3 pos = from + direction * dist;
                    pos.y = from.y; // 保持起点高度

                    Entity instance = ecb.Instantiate(_sightPrefab);
                    ecb.AddComponent<GameplayEntityTag>(instance);
                    ecb.SetComponent(instance, LocalTransform.FromPosition(pos));
                }
            }

            ecb.Playback(EntityManager);
            ecb.Dispose();
        }

        private (float3, float3) NormalizePair(float3 a, float3 b)
        {
            // 确保连接方向唯一（排序坐标）
            if (math.abs(a.x - b.x) > 0.0001f) return a.x < b.x ? (a, b) : (b, a);
            if (math.abs(a.y - b.y) > 0.0001f) return a.y < b.y ? (a, b) : (b, a);
            return a.z < b.z ? (a, b) : (b, a);
        }
    }
}