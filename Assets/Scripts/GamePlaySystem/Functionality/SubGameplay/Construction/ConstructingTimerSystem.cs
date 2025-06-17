using SparFlame.GamePlaySystem.General;
using SparFlame.GamePlaySystem.Movement;
using SparFlame.GamePlaySystem.Resource;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;

namespace SparFlame.GamePlaySystem.Building
{
    public partial struct ConstructingTimerSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<GameTimeData>();
            state.RequireForUpdate<EndSimulationEntityCommandBufferSystem.Singleton>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            new ConstructingTimerJob
            {
                DeltaTime = SystemAPI.GetSingleton<GameTimeData>().DeltaTime,
                ECB = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>()
                    .CreateCommandBuffer(state.WorldUnmanaged).AsParallelWriter()
            }.ScheduleParallel();
        }

     
        [BurstCompile]
        public partial struct ConstructingTimerJob : IJobEntity
        {
            [ReadOnly] public float DeltaTime;
            public EntityCommandBuffer.ParallelWriter ECB;
            private void Execute([ChunkIndexInQuery]int index,ref ConstructingData constructingTimer,
                Entity selfEntity, in BuildingAttr buildingAttr)
            {
                constructingTimer.LastTime -= DeltaTime;
                if (constructingTimer.LastTime <= 0)
                {
                    ECB.RemoveComponent<ConstructingData>(index, selfEntity);
                    if(buildingAttr.Type == BuildingType.Dwellings)
                        ECB.SetComponentEnabled<DwellingGeneratePopulationTag>(index, selfEntity, true);
                    ECB.SetComponentEnabled<VolumeObstacleSpawnRequest>(index, selfEntity, true);
                }
            }
        }
    
    }
}