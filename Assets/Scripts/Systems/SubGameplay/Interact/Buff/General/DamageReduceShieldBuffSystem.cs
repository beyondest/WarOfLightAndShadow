using SparFlame.Components.General;
using SparFlame.Components.SubGameplay;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;

namespace SparFlame.Systems.SubGameplay.Interact.General
{
    public partial struct DamageReduceShieldBuffSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<EndSimulationEntityCommandBufferSystem.Singleton>();
            state.RequireForUpdate<SubGamingTag>();
            state.RequireForUpdate<GameTimeData>();
            state.RequireForUpdate<DamageReduceShieldBuff>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var ecb = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>()
                .CreateCommandBuffer(state.WorldUnmanaged);
            new CheckTimeJob
            {
                ElapsedTime = SystemAPI.GetSingleton<GameTimeData>().ElapsedTime,
                ECB = ecb.AsParallelWriter()
            }.ScheduleParallel();
        }

        [BurstCompile]
        public void OnDestroy(ref SystemState state)
        {

        }
        [WithNone(typeof(UnitDeadTag))]
        public partial struct CheckTimeJob : IJobEntity
        {
            public EntityCommandBuffer.ParallelWriter ECB;
            [ReadOnly] public float ElapsedTime;
            private void Execute([ChunkIndexInQuery]int index,in DamageReduceShieldBuff buff, Entity selfEntity)
            {
                if(buff.StopTime >ElapsedTime)return;
                ECB.RemoveComponent<DamageReduceShieldBuff>(index,selfEntity );
            }
        }
    }
    
}