using SparFlame.Components.General;
using SparFlame.Components.SubGameplay;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;

namespace SparFlame.Systems.SubGameplay.Interact
{
    public partial struct SprintBuffSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<SprintBuffConfig>();
            state.RequireForUpdate<EndSimulationEntityCommandBufferSystem.Singleton>();
            state.RequireForUpdate<GameTimeData>();
            state.RequireForUpdate<SprintBuff>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            new SprintBuffJob
            {
                DeltaTime = SystemAPI.GetSingleton<GameTimeData>().DeltaTime,
                ECB = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>()
                    .CreateCommandBuffer(state.WorldUnmanaged).AsParallelWriter(),
                Config = SystemAPI.GetSingleton<SprintBuffConfig>()
            }.ScheduleParallel();
        }

   

        [BurstCompile]
        public partial struct SprintBuffJob : IJobEntity
        {
            public EntityCommandBuffer.ParallelWriter ECB;
            [ReadOnly] public float DeltaTime;
            [ReadOnly] public SprintBuffConfig Config;

            private void Execute(
                [ChunkIndexInQuery] int index,
                ref SprintBuff sprintBuffData,
                ref InteractAbilityBonus bonus,
                Entity selfEntity)
            {
                sprintBuffData.LastTime -= DeltaTime;
                if (sprintBuffData.LastTime <= 0)
                {
                    ECB.SetComponentEnabled<SprintBuff>(index, selfEntity, false);
                    bonus.MoveSpeedBonus -= Config.sprintSpeedBonusAmount;
                }
            }
        }
    }
}