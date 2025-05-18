using SparFlame.GamePlaySystem.General;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace SparFlame.GamePlaySystem.CustomParticleSystem
{
    public struct ParabolaProjectileData : IComponentData
    {
        
        // Must be set when projectile spawn
        
        public Entity Target;
        public float StartTime;
        public StatChangeRequest Request;
        public float MaxFlightDistance;
        public bool NotStopUntilReachMaxDis;
        public float3 StartPos;

        // Internal data
        public bool IsTargetAlive;
        public float3 TargetLastPos;

        // Fixed data of this kind projectile
        public Entity HitEffectPrefab;
        public float HorizontalSpeed; // Fixed speed
        public float MaxIncreaseHeight; // Relative max height from start pos
    }

    public partial struct ParabolaProjectileSystem : ISystem
    {
        private ComponentLookup<LocalTransform> _transformLookup;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<ParabolaProjectileConfig>();
            state.RequireForUpdate<EndSimulationEntityCommandBufferSystem.Singleton>();
            state.RequireForUpdate<GameTimeData>();
            state.RequireForUpdate<GamingTag>();
            state.RequireForUpdate<ParabolaProjectileData>();
            _transformLookup = state.GetComponentLookup<LocalTransform>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
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
                if (data.NotStopUntilReachMaxDis)
                {
                    var direction = targetPos.xz - data.StartPos.xz;
                    var curDis0 = math.length(curTransform.Position.xz - data.StartPos.xz);
                    if (curDis0 >= data.MaxFlightDistance)
                    {
                        DestroyProjectile(index, selfEntity, data, targetPos);
                    }
                    else
                    {
                        var nextPos = curTransform.Position.xz+ math.normalizesafe(direction) * data.HorizontalSpeed * DeltaTime;
                        curTransform.Position= new float3(nextPos.x, curTransform.Position.y, nextPos.y);
                    }
                    return;
                }
                
                // For single target projectile
                var curDis = math.length(targetPos.xz - curTransform.Position.xz);
                if (curDis <= data.HorizontalSpeed * DeltaTime|| curDis < Config.ReachDis)
                {
                    DestroyProjectile(index, selfEntity, data, targetPos);
                    return;
                }
                
                var curDirection = math.normalize(targetPos - curTransform.Position);
                var predictReachDuration =curDis / data.HorizontalSpeed;
                var duration = CurTime - data.StartTime + predictReachDuration;
                float t = (CurTime - data.StartTime) / duration;
                t = math.saturate(t);

                // 垂直方向：抛物线 (y = 4h * t * (1 - t))
                float height = 4f * data.MaxIncreaseHeight * t * (1 - t);
                float3 pos = curTransform.Position + curDirection * data.HorizontalSpeed * DeltaTime;
                pos.y = height;
                var trulyMoveDirection = math.normalize(pos - curTransform.Position);
                // 更新位置
                curTransform.Position = pos;
                // 方向朝向目标
                curTransform.Rotation = quaternion.LookRotationSafe(trulyMoveDirection, math.up());
            }
            
            private void DestroyProjectile(int index, Entity selfEntity,in ParabolaProjectileData data,
                in float3 pos)
            {
                // If this projectile has hit effect, then spawn it
                if (data.HitEffectPrefab != Entity.Null)
                {
                    var hitVfx = ECB.Instantiate(index, data.HitEffectPrefab);
                    ECB.AddComponent<GameplayEntityTag>(index, hitVfx);
                    ECB.AddComponent(index, hitVfx, new VFXData
                    {
                        KeepDuration = 0,
                        Reset = true,
                        StartTime = 0,
                        TimeToLive = 0,
                        Tracker = Entity.Null,
                        VFXType = VFXType.Instant,
                    });
                    var prefabTrans = TransformLookup[data.HitEffectPrefab];
                    prefabTrans.Position = pos;
                    ECB.SetComponent(index, hitVfx, prefabTrans);
                }
                
                // If target is alive, then spawn stat change effect
                if (data.IsTargetAlive)
                {
                    var request = ECB.CreateEntity(index);
                    ECB.AddComponent<GameplayEntityTag>(index, request);
                    ECB.AddComponent(index, request, data.Request);
                }
                ECB.DestroyEntity(index, selfEntity);
            }
        }
    }
}