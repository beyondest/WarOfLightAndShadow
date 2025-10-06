using SparFlame.Components.General;
using SparFlame.Components.MainGameplay;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;

// ReSharper disable Unity.Entities.SingletonMustBeRequested

namespace GamePlaySystem.Functionality.MainGameplay.General
{
    [UpdateInGroup(typeof(InitializationSystemGroup))]
    public partial struct MainGameplayInitDistinguishSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<PlayerFactionData>();
            state.RequireForUpdate<EndInitializationEntityCommandBufferSystem.Singleton>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var gameStatus = SystemAPI.GetSingleton<GameStatusData>();
            if(gameStatus.Value == GameStatus.NotStarted)return;
            var ecbP = SystemAPI.GetSingleton<EndInitializationEntityCommandBufferSystem.Singleton>()
                .CreateCommandBuffer(state.WorldUnmanaged).AsParallelWriter();
            state.Dependency = new InitDistinguishJob
            {
                PlayerFactionData = SystemAPI.GetSingleton<PlayerFactionData>(),
                ECB = ecbP
            }.ScheduleParallel(state.Dependency);
            // if (SystemAPI.HasSingleton<ReassignMainGameplayAITagRequest>())
            // {
            //     state.EntityManager.DestroyEntity(SystemAPI.GetSingletonEntity<ReassignMainGameplayAITagRequest>());
            //     new ReassignTagJob
            //     {
            //         ECB = ecbP,
            //         PlayerFactionData = SystemAPI.GetSingleton<PlayerFactionData>()
            //     }.ScheduleParallel();
            // }
        }


        [BurstCompile]
        [WithNone(typeof(PlayerTag))]
        [WithNone(typeof(AITag))]
        public partial struct InitDistinguishJob : IJobEntity
        {
            public EntityCommandBuffer.ParallelWriter ECB;
            [ReadOnly] public PlayerFactionData PlayerFactionData;

            private void Execute([ChunkIndexInQuery] int index, in MainGameplayGeneralAttr generalAttr,
                Entity selfEntity)
            {
                var relationship =
                    FactionUtils.GetRelationship(PlayerFactionData.faction,
                        PlayerFactionData.subFaction, generalAttr.faction, generalAttr.subFaction);
                if (relationship is Relationship.Self or Relationship.Ally)
                {
                    ECB.AddComponent<PlayerTag>(index, selfEntity);
                }
                else
                {
                    ECB.AddComponent<AITag>(index, selfEntity);
                }
            }
        }

        [BurstCompile]
        [WithAll(typeof(AITag))]
        public partial struct ReassignTagJob : IJobEntity
        {
            public EntityCommandBuffer.ParallelWriter ECB;
            [ReadOnly] public PlayerFactionData PlayerFactionData;

            private void Execute([ChunkIndexInQuery] int index, in MainGameplayGeneralAttr generalAttr,
                Entity selfEntity)
            {
                var relationship =
                    FactionUtils.GetRelationship(PlayerFactionData.faction,
                        PlayerFactionData.subFaction, generalAttr.faction, generalAttr.subFaction);
                if (relationship == Relationship.Self || relationship == Relationship.Ally)
                {
                    ECB.RemoveComponent<AITag>(index, selfEntity);
                    ECB.AddComponent<PlayerTag>(index, selfEntity);
                }
            }
        }
    }
}