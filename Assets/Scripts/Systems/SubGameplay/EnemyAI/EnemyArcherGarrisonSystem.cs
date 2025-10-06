using SparFlame.Components.General;
using SparFlame.Components.SubGameplay;
using Unity.Burst;
using Unity.Entities;
using Unity.Transforms;

namespace SparFlame.Systems.SubGameplay.EnemyAI
{
    public partial struct EnemyArcherGarrisonSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<EndInitializationEntityCommandBufferSystem.Singleton>();
            state.RequireForUpdate<EnemyGarrisonArcher>();
            state.RequireForUpdate<SubGamingTag>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            state.Dependency = new EnemyArcherGarrisonJob
            {
                ECB = SystemAPI.GetSingleton<EndInitializationEntityCommandBufferSystem.Singleton>()
                    .CreateCommandBuffer(state.WorldUnmanaged).AsParallelWriter()
            }.ScheduleParallel(state.Dependency);
        }

    
    }

    [BurstCompile]
    [WithNone(typeof(AssignGlobalSingleIDRequest))]
    public partial struct EnemyArcherGarrisonJob : IJobEntity
    {
        public EntityCommandBuffer.ParallelWriter ECB;

        private void Execute([ChunkIndexInQuery] int index, in EnemyGarrisonArcher data,
            in PrefabId id, Entity selfEntity, in UnitAttr unitAttr, in LocalTransform transform,
            in GlobalSingleId singleId)
        {
            var request = ECB.CreateEntity(index);
            ECB.AddComponent(index, request, new GarrisonInBuildingRequest
            {
                Id = id.value,
                BuildingEntity = data.TargetWall,
                UnitEntity = selfEntity,
                UnitType = unitAttr.type
            });
            ECB.AddComponent<SubGameplayEntityTag>(index, request);
            ECB.RemoveComponent<EnemyGarrisonArcher>(index, selfEntity);
            ECB.AddComponent(index,selfEntity, new InGarrison
            {
                BuildingEntity = data.TargetWall,
                BeforePos = transform.Position,
                SingleId = singleId.value,
                InBuilding = false
            });
        }
    }
}