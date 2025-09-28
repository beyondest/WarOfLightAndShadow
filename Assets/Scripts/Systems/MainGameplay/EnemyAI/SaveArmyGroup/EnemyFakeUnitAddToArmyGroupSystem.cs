using SparFlame.Components.General;
using SparFlame.Components.MainGameplay;
using Unity.Burst;
using Unity.Entities;

namespace SparFlame.Systems.MainGameplay.EnemyAI
{
    [RequireMatchingQueriesForUpdate]
    public partial struct EnemyFakeUnitAddToArmyGroupSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<EndSimulationEntityCommandBufferSystem.Singleton>();
            state.RequireForUpdate<FakeUnitNeedAddToArmyGroupAfterAssignSingleId>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            
            new FakeUnitAddJob
            {
                ECB = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>()
                    .CreateCommandBuffer(state.WorldUnmanaged).AsParallelWriter()
            }.ScheduleParallel();
        }

    }

    /// <summary>
    /// This job waits for all fake units init complete to save
    /// </summary>
    [BurstCompile]
    [WithNone(typeof(InArmyGroup))]
    [WithNone(typeof(AssignGlobalSingleIDRequest))]
    [WithNone(typeof(AssignRandomRequest))]
    public partial struct FakeUnitAddJob : IJobEntity
    {
        public EntityCommandBuffer.ParallelWriter ECB;

        private void Execute([ChunkIndexInQuery] int index, Entity selfEntity,
            in FakeUnitNeedAddToArmyGroupAfterAssignSingleId fakeUnitNeedAddToArmyGroupAfterAssignSingleId)
        {
            // Add fake unit to army group
            var addToArmyGroupRequest = ECB.CreateEntity(index);
            ECB.AddComponent(index, addToArmyGroupRequest, new AddToArmyGroupRequest
            {
                ArmyGroup = fakeUnitNeedAddToArmyGroupAfterAssignSingleId.ArmyGroup,
                Type = AddToArmyGroupType.OnlySpecifiedUnit,
                Unit = selfEntity
            });
            ECB.RemoveComponent<FakeUnitNeedAddToArmyGroupAfterAssignSingleId>(index, selfEntity);
        }
    }
}