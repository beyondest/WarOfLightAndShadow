using SparFlame.GamePlaySystem.RandomSpawn;
using SparFlame.GamePlaySystem.Waves;
using Unity.Burst;
using Unity.Entities;

namespace SparFlame.GamePlaySystem.EnemyAI
{
    [UpdateInGroup(typeof(InitializationSystemGroup))]
    [UpdateAfter(typeof(PlayerFirstBaseSpawnSystem))]
    [UpdateAfter(typeof(WaveSystem))]
    public partial struct EnemyBuildingPackSpawnSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<EndInitializationEntityCommandBufferSystem.Singleton>();

        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {

        }

        [BurstCompile]
        public void OnDestroy(ref SystemState state)
        {

        }
    }
}