using System;
using SparFlame.Components.General;
using SparFlame.Components.VFX;
using SparFlame.Core.Utils;
using SparFlame.Systems.General;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Entities.UniversalDelegates;
using Unity.Transforms;
using UnityEngine;

// ReSharper disable Unity.Entities.MustBeSurroundedWithRefRwRo

namespace SparFlame.Systems.General.VFX
{
    public partial struct CParticlePlaySystem : ISystem
    {
        private BufferLookup<TrackedByVFX> _vfxLookup;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<WaitInfo>();
            state.RequireForUpdate<GameStatusData>();
            state.RequireForUpdate<EndSimulationEntityCommandBufferSystem.Singleton>();
            state.RequireForUpdate<GameTimeData>();
            _vfxLookup = state.GetBufferLookup<TrackedByVFX>();
        }

        public void OnUpdate(ref SystemState state)
        {
            var gameStatus = SystemAPI.GetSingleton<GameStatusData>().Value;
            if(gameStatus != GameStatus.MainGaming && gameStatus != GameStatus.SubGaming)return;
            var waitInfo = SystemAPI.GetSingleton<WaitInfo>();
            if(waitInfo.WaitType != WaitType.None)return;
            
            _vfxLookup.Update(ref state);
            var ecb = new EntityCommandBuffer(Allocator.TempJob);
            PlayVFX(ref state, ecb);
            LateDestroyVFX(ref state, ecb);
            ecb.Playback(state.EntityManager);
            ecb.Dispose();
        }

        private void PlayVFX(ref SystemState state, EntityCommandBuffer ecb)
        {
            var curTime = SystemAPI.GetSingleton<GameTimeData>().ElapsedTime;
            foreach (var (vfxRw, groups, selfEntity) in SystemAPI
                         .Query<RefRW<VFXData>, DynamicBuffer<LinkedEntityGroup>>()
                         .WithEntityAccess())
            {
                ref var data = ref vfxRw.ValueRW;
                if (data.Reset)
                {
                    data.StartTime = curTime;
                    // Means this vfx either lives instantly or lives forever until manually killed
                    data.TimeToLive = data.KeepDuration == 0 ? float.MaxValue : data.KeepDuration;
                    for (int i = 1; i < groups.Length; i++)
                    {
                        var entity = groups[i].Value;
                        if (SystemAPI.ManagedAPI.HasComponent<ParticleSystem>(entity))
                        {
                            var sys = SystemAPI.ManagedAPI.GetComponent<ParticleSystem>(entity);
                            sys.Play();
                        }
                    }

                    data.Reset = false;
                    if (data.Tracker != Entity.Null)
                    {
                        // Tracker is dead
                        if (!SystemAPI.HasComponent<LocalTransform>(data.Tracker))
                        {
                            DestroyVFX(ref state, selfEntity, data, curTime, ecb);
                        }
                        else
                        {
                            var targetTransform = SystemAPI.GetComponent<LocalTransform>(data.Tracker);
                            var trans = SystemAPI.GetComponentRW<LocalTransform>(selfEntity);
                            trans.ValueRW.Position = targetTransform.Position;
                            // trans.ValueRW.Rotation = targetTransform.Rotation;
                        }
                    }
                    continue;
                }

                if (data.Tracker != Entity.Null)
                {
                    // Tracker is dead
                    if (!SystemAPI.HasComponent<LocalTransform>(data.Tracker))
                    {
                        DestroyVFX(ref state, selfEntity, data, curTime, ecb);
                    }
                    else
                    {
                        var targetTransform = SystemAPI.GetComponent<LocalTransform>(data.Tracker);
                        var trans = SystemAPI.GetComponentRW<LocalTransform>(selfEntity);
                        trans.ValueRW.Position = targetTransform.Position;
                        trans.ValueRW.Rotation = targetTransform.Rotation;
                    }
                }

                switch (data.VFXType)
                {
                    case VFXType.Instant:
                        var allStop = true;
                        for (int i = 1; i < groups.Length; i++)
                        {
                            if(!SystemAPI.ManagedAPI.HasComponent<ParticleSystem>(groups[i].Value))continue;
                            var sys = SystemAPI.ManagedAPI.GetComponent<ParticleSystem>(groups[i].Value);
                            if (sys.IsAlive())
                            {
                                allStop = false;
                                break;
                            }
                        }

                        if (allStop)
                        {
                            DestroyVFX(ref state, selfEntity, data, curTime, ecb);
                        }

                        break;
                    case VFXType.Continuos:
                        if (curTime > data.StartTime + data.TimeToLive)
                        {
                            DestroyVFX(ref state, selfEntity, data, curTime, ecb);
                            return;
                        }

                        break;
                    case VFXType.None:
                    case VFXType.Projectile:
                        // This should never happen
                        break;
                    default:
                        BurstSafe.UnexpectedEnum(data.VFXType);
                        break;
                }
            }
        }

        private void LateDestroyVFX(ref SystemState state, EntityCommandBuffer ecb)
        {
            var curTime = SystemAPI.GetSingleton<GameTimeData>().ElapsedTime;
            foreach (var (tag, groups, selfEntity) in SystemAPI
                         .Query<RefRO<LateDestroyVFXTag>, DynamicBuffer<LinkedEntityGroup>>()
                         .WithEntityAccess())
            {
                if (curTime > tag.ValueRO.DestroyTime)
                {
                    ecb.DestroyEntity( selfEntity);
                }
                else
                {
                    var allStop = true;
                    for (int i = 1; i < groups.Length; i++)
                    {
                        var tar = groups[i].Value;
                        if(!SystemAPI.ManagedAPI.HasComponent<ParticleSystem>(tar))continue;
                        var sys = SystemAPI.ManagedAPI.GetComponent<ParticleSystem>(tar);
                        if (sys.IsAlive(true))
                        {
                            allStop = false;
                        }
                    }
                    if (allStop)
                    {
                        ecb.DestroyEntity( selfEntity);
                    }
                }
            }
        }


        private void DestroyVFX(ref SystemState state,
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