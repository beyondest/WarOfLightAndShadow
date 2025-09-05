using SparFlame.Components.General;
using SparFlame.Components.SubGameplay;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;

namespace SparFlame.Systems.SubGameplay.Construct
{
    public partial struct ConstructingTimerSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<WorldTimeData>();
            state.RequireForUpdate<EndSimulationEntityCommandBufferSystem.Singleton>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            new ConstructingTimerJob
            {
                CurrentTotalHours = SystemAPI.GetSingleton<WorldTimeData>().totalHours,
                ECB = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>()
                    .CreateCommandBuffer(state.WorldUnmanaged).AsParallelWriter()
            }.ScheduleParallel();
        }


        [BurstCompile]
        public partial struct ConstructingTimerJob : IJobEntity
        {
            [ReadOnly] public float CurrentTotalHours;
            
            public EntityCommandBuffer.ParallelWriter ECB;

            private void Execute([ChunkIndexInQuery] int index, ref ConstructingTimer constructingTimer,
                Entity selfEntity, in BuildingAttr buildingAttr)
            {
                if (constructingTimer.builtUpTargetTotalHours > CurrentTotalHours) return;
                ECB.RemoveComponent<ConstructingTimer>(index, selfEntity);
            }
        }
    }
}