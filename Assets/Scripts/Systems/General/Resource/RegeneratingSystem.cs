using SparFlame.Components.General;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;

namespace SparFlame.Systems.General.Resource
{
    public partial struct RegeneratingSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<GameTimeData>();
            state.RequireForUpdate<EndSimulationEntityCommandBufferSystem.Singleton>();
            state.RequireForUpdate<SubGamingTag>();
            state.RequireForUpdate<RegeneratingSystemConfig>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var config = SystemAPI.GetSingleton<RegeneratingSystemConfig>();
            var ecb = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>();
            new RegeneratingJob
            {
                ECB = ecb.CreateCommandBuffer(state.WorldUnmanaged).AsParallelWriter(),
                Config = config,
                DeltaTime = SystemAPI.GetSingleton<GameTimeData>().DeltaTime,
            }.ScheduleParallel();
        }
        

        [BurstCompile]
        [WithAll(typeof(RegeneratingTag))]
        public partial struct RegeneratingJob : IJobEntity
        {
            public EntityCommandBuffer.ParallelWriter ECB;
            [ReadOnly] public RegeneratingSystemConfig Config;
            [ReadOnly] public float DeltaTime;
            private void Execute([ChunkIndexInQuery] int index,ref RenewableData renewableData, Entity entity)
            {
                renewableData.RegeneratingLeftTime -= DeltaTime * Config.RegeneratingTimeScale;
                if (renewableData.RegeneratingLeftTime < 0)
                {
                    ECB.RemoveComponent<RegeneratingTag>(index,entity);
                    renewableData.RegeneratingLeftTime = 0;
                }
            }
        }
    }
}