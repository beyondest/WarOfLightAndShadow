using SparFlame.GamePlaySystem.General;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;

namespace GamePlaySystem.Functionality.MainGameplay.General
{
    [UpdateInGroup(typeof(InitializationSystemGroup))]
    public partial struct InitDistinguishSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<PlayerFactionData>();
            state.RequireForUpdate<EndSimulationEntityCommandBufferSystem.Singleton>();
            state.RequireForUpdate<MainGamingTag>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            new InitDistinguishJob
            {
                PlayerFaction = SystemAPI.GetSingleton<PlayerFactionData>().Value,
                ECB = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>()
                    .CreateCommandBuffer(state.WorldUnmanaged).AsParallelWriter(),
            }.ScheduleParallel();
        }
        
    
        [BurstCompile]
        [WithNone(typeof(PlayerTag))]
        [WithNone(typeof(AITag))]
        public partial struct InitDistinguishJob : IJobEntity
        {
            public EntityCommandBuffer.ParallelWriter ECB;
            [ReadOnly] public FactionTag PlayerFaction;

            private void Execute([ChunkIndexInQuery] int index,in MainGameplayGeneralAttr generalAttr, Entity selfEntity)
            {
                if (generalAttr.Faction == PlayerFaction)
                {
                    ECB.AddComponent<PlayerTag>(index,selfEntity);
                }
                else if(generalAttr.Faction == ~PlayerFaction)
                {
                    ECB.AddComponent<AITag>(index, selfEntity);
                }
            }
        }
    }
}