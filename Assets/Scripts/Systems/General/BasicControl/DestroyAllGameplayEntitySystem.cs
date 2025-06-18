using SparFlame.Components.SubGameplay;
using Unity.Burst;
using Unity.Entities;

namespace SparFlame.Systems.General.BasicControl
{

    public struct ClearGameplayEntities : IComponentData
    {
        
    }
    [BurstCompile]
    [UpdateInGroup(typeof(LateSimulationSystemGroup))]
    public partial struct DestroyAllGameplayEntitySystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<BeginInitializationEntityCommandBufferSystem.Singleton>();
            state.RequireForUpdate<ClearGameplayEntities>();
        }
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            new DestroyGameplayEntityJob
            {
                ECB = SystemAPI.GetSingleton<BeginInitializationEntityCommandBufferSystem.Singleton>()
                    .CreateCommandBuffer(state.WorldUnmanaged).AsParallelWriter()
            }.ScheduleParallel();
            state.EntityManager.DestroyEntity(SystemAPI.GetSingletonEntity<ClearGameplayEntities>());
        }

        [BurstCompile]
        public void OnDestroy(ref SystemState state)
        {
        }

        [BurstCompile]
        [WithAll(typeof(SubGameplayEntityTag))]
        private partial struct DestroyGameplayEntityJob : IJobEntity
        {
            public EntityCommandBuffer.ParallelWriter ECB;

            private void Execute([ChunkIndexInQuery] int index, Entity selfEntity)
            {
                ECB.DestroyEntity(index, selfEntity);
            }
        }
    }
}