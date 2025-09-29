using SparFlame.Components.General;
using SparFlame.Components.MainGameplay;
using SparFlame.Components.SubGameplay;
using SparFlame.Core.Utils;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Transforms;

// ReSharper disable Unity.Entities.SingletonMustBeRequested

namespace SparFlame.Systems.SubGameplay.EnemyAI
{
    [UpdateInGroup(typeof(InitializationSystemGroup))]
    public partial struct SubGameplayInitDistinguishSystem : ISystem
    {
        private BufferLookup<LinkedEntityGroup> _linkedEntityGroupLookup;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<EndInitializationEntityCommandBufferSystem.Singleton>();
            state.RequireForUpdate<GeneralRandom>();
            state.RequireForUpdate<PlayerFactionData>();
            state.RequireForUpdate<GameStatusData>();
            _linkedEntityGroupLookup = state.GetBufferLookup<LinkedEntityGroup>(true);
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var gameStatus = SystemAPI.GetSingleton<GameStatusData>();
            if(gameStatus.Value == GameStatus.NotStarted)return;
            _linkedEntityGroupLookup.Update(ref state);
            var playerFactionData = SystemAPI.GetSingleton<PlayerFactionData>();
            var ecbP = SystemAPI.GetSingleton<EndInitializationEntityCommandBufferSystem.Singleton>()
                .CreateCommandBuffer(state.WorldUnmanaged).AsParallelWriter();
            var job =  new DistinguishEnemyJob
            {
                ECB = ecbP,
                PlayerFactionData = playerFactionData
            }.ScheduleParallel(state.Dependency);
            job.Complete();
 
        }

        [BurstCompile]
        [WithNone(typeof(AITag))]
        [WithNone(typeof(PlayerTag))]
        [WithNone(typeof(ResourceAttr))]
        [WithNone(typeof(FakeUnitNeedAddToArmyGroupAfterAssignSingleId))]
        public partial struct DistinguishEnemyJob : IJobEntity
        {
            public EntityCommandBuffer.ParallelWriter ECB;
            [ReadOnly] public PlayerFactionData PlayerFactionData;

            private void Execute([ChunkIndexInQuery] int index, in SubGameplayGeneralAttr attr,
                in LocalTransform transform, Entity selfEntity)
            {
                // General distinguish
                var relationship = FactionUtils.GetRelationship(PlayerFactionData.faction,
                    PlayerFactionData.subFaction, attr.Faction, attr.SubFaction);
                if (relationship is Relationship.Self or Relationship.Ally)
                {
                    ECB.AddComponent<PlayerTag>(index, selfEntity);
                    return;
                }

                ECB.AddComponent<AITag>(index, selfEntity);
                
            }
        }
        
        

    }
}