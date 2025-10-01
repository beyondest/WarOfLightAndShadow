using SparFlame.Components.General;
using SparFlame.Components.SubGameplay;
using SparFlame.Components.VFX;
using SparFlame.Systems.General.Audio;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace SparFlame.Systems.General.VFX
{
    public struct ParabolaProjectileData : IComponentData
    {
        // Must be set when projectile spawn

        public Entity Target;
        public float StartTime;
        public StatChangeRequest Request;
        public float MaxFlightDistance;
        public ProjectileType ProjectileType;
        public float InitialHeight;
        public float3 StartPos;

        // Internal data
        public bool IsTargetAlive;
        public float3 TargetLastPos;

        // Fixed data of this kind projectile
        public Entity HitEffectPrefab;
        public float HorizontalSpeed; // Fixed speed
        public float MaxAbsHeight; // Relative max height from start pos
    }

    public partial struct ParabolaProjectileSystem : ISystem
    {
        private ComponentLookup<LocalTransform> _transformLookup;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<WaitInfo>();
            state.RequireForUpdate<ParabolaProjectileConfig>();
            state.RequireForUpdate<EndSimulationEntityCommandBufferSystem.Singleton>();
            state.RequireForUpdate<GameTimeData>();
            state.RequireForUpdate<GameStatusData>();
            state.RequireForUpdate<ParabolaProjectileData>();
            _transformLookup = state.GetComponentLookup<LocalTransform>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var gameStatus = SystemAPI.GetSingleton<GameStatusData>().Value;
            if(gameStatus != GameStatus.MainGaming && gameStatus!= GameStatus.SubGaming)return;
            var waitInfo = SystemAPI.GetSingleton<WaitInfo>();
            if(waitInfo.WaitType != WaitType.None)return;
            
            _transformLookup.Update(ref state);
            var ecbSingleton = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>();
            var ecb = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged).AsParallelWriter();
            var timeData = SystemAPI.GetSingleton<GameTimeData>();
            new ParaBolaProjectileJob
            {
                CurTime = timeData.ElapsedTime,
                TransformLookup = _transformLookup,
                ECB = ecb,
                Config = SystemAPI.GetSingleton<ParabolaProjectileConfig>(),
                DeltaTime = timeData.DeltaTime
            }.ScheduleParallel();
        }

        [BurstCompile]
        public partial struct ParaBolaProjectileJob : IJobEntity
        {
            public EntityCommandBuffer.ParallelWriter ECB;
            [NativeDisableParallelForRestriction] public ComponentLookup<LocalTransform> TransformLookup;
            [ReadOnly] public float CurTime;
            [ReadOnly] public ParabolaProjectileConfig Config;
            [ReadOnly] public float DeltaTime;

            private void Execute([ChunkIndexInQuery] int index, ref ParabolaProjectileData data, Entity selfEntity)
            {
                float3 targetPos;
                if (!data.IsTargetAlive || !TransformLookup.TryGetComponent(data.Target, out var targetTransform))
                {
                    // Target is dead, but we keep moving the projectile until it reach the dead body
                    data.IsTargetAlive = false;
                    targetPos = data.TargetLastPos;
                }
                else
                {
                    targetPos = targetTransform.Position;
                    data.TargetLastPos = targetPos;
                }

                ref var curTransform = ref TransformLookup.GetRefRW(selfEntity).ValueRW;


                // For straight thoroughly projectile
                if (data.ProjectileType == ProjectileType.NoHeightChangeUntilReachMaxDis)
                {
                    var direction = targetPos.xz - data.StartPos.xz;
                    var curDis0 = math.length(curTransform.Position.xz - data.StartPos.xz);
                    if (curDis0 >= data.MaxFlightDistance)
                    {
                        DestroyProjectile(index, selfEntity, data, targetPos);
                    }
                    else
                    {
                        var nextPos = curTransform.Position.xz +
                                      math.normalizesafe(direction) * data.HorizontalSpeed * DeltaTime;
                        curTransform.Position = new float3(nextPos.x, curTransform.Position.y, nextPos.y);
                    }

                    return;
                }


                // For single target projectile
                var curDis = math.length(targetPos.xz - curTransform.Position.xz);
                if (curDis <= data.HorizontalSpeed * DeltaTime || curDis < Config.ReachDis)
                {
                    DestroyProjectile(index, selfEntity, data, targetPos);
                    return;
                }

                var curDirection = math.normalize(targetPos - curTransform.Position);
                var predictReachDuration = curDis / data.HorizontalSpeed;
                var duration = CurTime - data.StartTime + predictReachDuration;
                var pos = curTransform.Position + curDirection * data.HorizontalSpeed * DeltaTime;

                float height;
                if (data.ProjectileType == ProjectileType.GoStraightToTargetWithHeightChange)
                {
                    var t = (CurTime - data.StartTime) / duration;
                    t = math.saturate(t);
                    var heightDelta = data.InitialHeight- targetPos.y;
                    height = data.InitialHeight- heightDelta * t;
                    if (height > pos.y) height = pos.y;
                }
                else
                {
                    if (duration == 0) height = targetPos.y;
                    else
                    {
                        var v0 = (targetPos.y - data.InitialHeight + 0.5f * Config.G * duration * duration) / duration;
                        // height =4 * data.MaxAbsHeight * t * (1 - t);
                        var t = CurTime - data.StartTime;
                        height = v0 * t - 0.5f * Config.G * t * t + data.InitialHeight;
                    }
                }

                pos.y = height ;
                var trulyMoveDirection = math.normalize(pos - curTransform.Position);
                // 更新位置
                curTransform.Position = pos;
                // 方向朝向目标
                curTransform.Rotation = quaternion.LookRotationSafe(trulyMoveDirection, math.up());
            }

            private void DestroyProjectile(int index, Entity selfEntity, in ParabolaProjectileData data,
                in float3 pos)
            {
                // If this projectile has hit effect, then spawn it
                if (data.HitEffectPrefab != Entity.Null)
                {
                    var hitVfx = ECB.Instantiate(index, data.HitEffectPrefab);
                    ECB.AddComponent<SubGameplayEntityTag>(index, hitVfx);
                    ECB.AddComponent(index, hitVfx, new VFXData
                    {
                        KeepDuration = 0,
                        Reset = true,
                        StartTime = 0,
                        TimeToLive = 0,
                        Tracker = Entity.Null,
                        VFXType = VFXType.Instant,
                        MaxWaitTimeForAllStopPlay = 2f,
                        KillUntilAllStopPlay = true
                    });
                    var prefabTrans = TransformLookup[data.HitEffectPrefab];
                    prefabTrans.Position = pos;
                    ECB.SetComponent(index, hitVfx, prefabTrans);
                    // Generate tower magic ball hit sound
                    AudioUtils.PlayAudioClip(AudioName.TowerMagicBallHit, pos,ECB, index);
                }

                // If target is alive, then spawn stat change effect
                if (data.IsTargetAlive)
                {
                    var request = ECB.CreateEntity(index);
                    ECB.AddComponent<SubGameplayEntityTag>(index, request);
                    ECB.AddComponent(index, request, data.Request);
                }

                ECB.DestroyEntity(index, selfEntity);
            }
        }
    }
}