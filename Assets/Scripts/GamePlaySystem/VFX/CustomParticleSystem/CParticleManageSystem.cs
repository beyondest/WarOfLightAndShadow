using System;
using System.Collections.Generic;
using SparFlame.GamePlaySystem.General;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

namespace SparFlame.GamePlaySystem.CustomParticleSystem
{
    [UpdateBefore(typeof(TransformSystemGroup))]
    public partial class CParticleManageSystem : SystemBase
    {
        private NativeParallelMultiHashMap<int, VFXPrefabDataPair> _vfxName2PrefabDatabase;
        private EntityQuery _requestQuery;

        protected override void OnCreate()
        {
            RequireForUpdate<GamingTag>();
            RequireForUpdate<CParticleSystemConfig>();
            RequireForUpdate<VFXConfigData>();
            _requestQuery = SystemAPI.QueryBuilder().WithAll<VFXRequest>().Build();
        }

        protected override void OnStartRunning()
        {
            if (!_vfxName2PrefabDatabase.IsCreated)
                Initialize();
        }

        private void Initialize()
        {
            _vfxName2PrefabDatabase =
                new NativeParallelMultiHashMap<int, VFXPrefabDataPair>(5, allocator: Allocator.Persistent);
            var buffer = SystemAPI.GetSingletonBuffer<VFXConfigData>();
            // var tmp = new NativeHashMap<int, NativeList<VFXPrefabDataPair>>(5, Allocator.Temp);
            // foreach (var data in buffer)
            // {
            //     if (!tmp.ContainsKey((int)data.Name))
            //         tmp.Add((int)data.Name, new NativeList<VFXPrefabDataPair>(Allocator.Temp));
            //     var list = tmp[(int)data.Name];
            //     list.Add(data.Pair);
            // }

            foreach (var data in buffer)
            {
                // // pair.Value.Sort(new VFXPrefabPairComparer());
                // foreach (var s in pair.Value)
                // {
                _vfxName2PrefabDatabase.Add((int)data.Name, data.Pair);
                // }
            }

            // foreach (var pair in tmp)
            // {
            //     pair.Value.Dispose();
            // }
            //
            // tmp.Dispose();
        }

        protected override void OnUpdate()
        {
            if (_requestQuery.IsEmpty) return;
            CheckVFXRequest();
        }


        private void CheckVFXRequest()
        {
            var parabolaConfig = SystemAPI.GetSingleton<ParabolaProjectileConfig>();
            var curTime = SystemAPI.GetSingleton<GameTimeData>().ElapsedTime;
            var ecb = new EntityCommandBuffer(Allocator.Temp);
            var entities = _requestQuery.ToEntityArray(Allocator.Temp);
            var reqs = _requestQuery.ToComponentDataArray<VFXRequest>(allocator: Allocator.Temp);
            for (var i = 0; i < reqs.Length; i++)
            {
                var request = reqs[i];
                var entity = entities[i];
                ecb.DestroyEntity(entity);

                // If request to track someone but that entity is dead, then continue
                if (request.VFXTrackTarget != Entity.Null)
                {
                    if (!SystemAPI.HasBuffer<TrackedByVFX>(request.VFXTrackTarget))
                        continue;
                }

                switch (request.RequestType)
                {
                    case VFXRequestType.Spawn:
                    {
                        var pairList = _vfxName2PrefabDatabase.GetValuesForKey((int)request.VFXName);
                        var find = false;
                        var targetPair = new VFXPrefabDataPair();
                        foreach (var pair in pairList)
                        {
                            if (request.Filter.FactionFilterEnable)
                            {
                                if (pair.Filter.FactionFilterEnable && pair.Filter.Faction != request.Filter.Faction)
                                    continue;
                            }

                            if (request.Filter.TierFilterEnable)
                            {
                                if (pair.Filter.TierFilterEnable && pair.Filter.Tier != request.Filter.Tier)
                                    continue;
                            }

                            find = true;
                            targetPair = pair;
                        }

                        if (!find)
                        {
                            // This should never happen
                            Debug.LogError(
                                $"Not find request vfx, this should never happen {request.VFXName} {request.Filter}");
                            continue;
                        }

                        // Check if target is tracked by same name vfx, if so, reset the ttl and continue
                        if (request.VFXTrackTarget != Entity.Null)
                        {
                            var buffer = SystemAPI.GetBuffer<TrackedByVFX>(request.VFXTrackTarget);
                            var findSame = false;
                            foreach (var vfxExist in buffer)
                            {
                                if (vfxExist.Name == request.VFXName)
                                {
                                    var vfxData = SystemAPI.GetComponentRW<VFXData>(vfxExist.VFX);
                                    vfxData.ValueRW.KeepDuration = request.KeepDuration;
                                    vfxData.ValueRW.Reset = true;
                                    findSame = true;
                                    break;
                                }
                            }

                            if (findSame) continue;
                        }

                        var vfx = EntityManager.Instantiate(targetPair.Prefab);
                        var originalTrans = SystemAPI.GetComponent<LocalTransform>(targetPair.Prefab);
                        originalTrans.Position = request.SpawnPosition;
                        EntityManager.SetComponentData(vfx, originalTrans);

                        if (request.VFXTrackTarget != Entity.Null)
                        {
                            var buffer = SystemAPI.GetBuffer<TrackedByVFX>(request.VFXTrackTarget);
                            buffer.Add(new TrackedByVFX
                            {
                                Name = request.VFXName,
                                VFX = vfx
                            });
                        }

                        ecb.AddComponent<GameplayEntityTag>(vfx);
                        // Projectile vfx need to be dealt separately
                        if (targetPair.VFXType != VFXType.Projectile)
                        {
                            ecb.AddComponent(vfx, new VFXData
                            {
                                VFXType = targetPair.VFXType,
                                Tracker = request.VFXTrackTarget,
                                KeepDuration = request.KeepDuration,
                                TimeToLive = 0,
                                StartTime = 0,
                                Reset = true,
                                KillUntilAllStopPlay = targetPair.KillUntilAllStopPlay,
                                MaxWaitTimeForAllStopPlay = targetPair.MaxWaitTimeForAllStopPlay
                            });
                        }
                        else
                        {
                            var hitPrefab = Entity.Null;
                            if (targetPair.PData.HitEffectName != VFXName.None)
                            {
                                foreach (var pair in _vfxName2PrefabDatabase.GetValuesForKey((int)targetPair.PData.HitEffectName))
                                {
                                    if (request.Filter.FactionFilterEnable)
                                    {
                                        if (pair.Filter.FactionFilterEnable && pair.Filter.Faction != request.Filter.Faction)
                                            continue;
                                    }
                                    if (request.Filter.TierFilterEnable)
                                    {
                                        if (pair.Filter.TierFilterEnable && pair.Filter.Tier != request.Filter.Tier)
                                            continue;
                                    }
                                    hitPrefab = pair.Prefab;
                                }
                            }
                            
                            var isTargetAlive = false;
                            var targetLastPos = request.TargetPosition;
                            if (SystemAPI.HasComponent<LocalTransform>(request.StatChangeRequest.Interactee))
                            {
                                isTargetAlive = true;
                                targetLastPos = SystemAPI
                                    .GetComponent<LocalTransform>(request.StatChangeRequest.Interactee).Position;
                            }

                            var dis = math.distance(targetLastPos, request.SpawnPosition);
                            ecb.AddComponent(vfx, new ParabolaProjectileData
                            {
                                HitEffectPrefab = hitPrefab,
                                HorizontalSpeed = targetPair.PData.HorizontalSpeed,
                                MaxAbsHeight = targetPair.PData.ProjectileType == ProjectileType.Parabola
                                    ? targetPair.PData.BaseRelativeHeight * dis *
                                      parabolaConfig.HeightIncreasePerUnitDis
                                    : 0,
                                Target = request.StatChangeRequest.Interactee,
                                Request = request.StatChangeRequest,
                                StartTime = curTime,
                                TargetLastPos = targetLastPos,
                                IsTargetAlive = isTargetAlive,
                                MaxFlightDistance = targetPair.PData.MaxFlightDistance,
                                StartPos = request.SpawnPosition,
                                ProjectileType = targetPair.PData.ProjectileType,
                                InitialHeight = targetPair.PData.InitialHeight,
                            });
                        }
                        break;
                    }
                    case VFXRequestType.Kill:
                        var vfxBuffer = SystemAPI.GetBuffer<TrackedByVFX>(request.VFXTrackTarget);
                        for (var j = vfxBuffer.Length - 1; j >= 0; j--)
                        {
                            var vfx = vfxBuffer[j];
                            // This should always happen
                            if (vfx.Name == request.VFXName)
                            {
                                var data = SystemAPI.GetComponent<VFXData>(vfx.VFX);
                                DestroyVFX(vfx.VFX, data, curTime, ecb);
                                break;
                            }
                        }
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }

            ecb.Playback(EntityManager);
            ecb.Dispose();
            entities.Dispose();
            reqs.Dispose();
        }


        protected override void OnDestroy()
        {
            if (_vfxName2PrefabDatabase.IsCreated)
                _vfxName2PrefabDatabase.Dispose();
        }

        private void DestroyVFX(
            Entity rootEntity, in VFXData data, float curTime,
            EntityCommandBuffer ecb)
        {
            ecb.RemoveComponent<VFXData>(rootEntity);

            // Ensure target is not dead
            if (SystemAPI.HasBuffer<TrackedByVFX>(data.Tracker))
            {
                var buffer = SystemAPI.GetBuffer<TrackedByVFX>(data.Tracker);
                for (int i = buffer.Length - 1; i >= 0; i--)
                {
                    if (buffer[i].VFX == rootEntity)
                    {
                        buffer.RemoveAt(i);
                        break;
                    }
                }
            }

            if (data.KillUntilAllStopPlay)
            {
                var children = SystemAPI.GetBuffer<LinkedEntityGroup>(rootEntity);
                for (int i = 1; i < children.Length; i++)
                {
                    var entity = children[i].Value;
                    if (SystemAPI.ManagedAPI.HasComponent<ParticleSystem>(entity))
                    {
                        var sys = SystemAPI.ManagedAPI.GetComponent<ParticleSystem>(entity);
                        sys.Stop(withChildren: true, ParticleSystemStopBehavior.StopEmitting);
                    }
                }

                ecb.AddComponent(rootEntity, new LateDestroyVFXTag
                {
                    DestroyTime = curTime + data.MaxWaitTimeForAllStopPlay
                });
            }
            else
            {
                ecb.DestroyEntity(rootEntity);
            }
        }
    }
}