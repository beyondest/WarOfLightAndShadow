using SparFlame.Components.General;
using SparFlame.Components.MainGameplay;
using SparFlame.Components.VFX;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Transforms;

namespace SparFlame.Systems.MainGameplay.EnemyAI
{
    public partial struct SimulateInvadingSupportCitySystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<SubGameStatusData>();
            state.RequireForUpdate<GameStatusData>();
            state.RequireForUpdate<InvadeSupportCityConfig>();
            state.RequireForUpdate<InvadingSupportCityTag>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var gameStatusData = SystemAPI.GetSingleton<GameStatusData>().Value;
            if(gameStatusData != GameStatus.MainGaming && gameStatusData != GameStatus.SubGaming) return;
            var subGameStatusData = SystemAPI.GetSingleton<SubGameStatusData>();
            if(GameStatusUtils.IsInBattle(subGameStatusData))return;
            var ecb = new EntityCommandBuffer(Allocator.Temp);
            var config = SystemAPI.GetSingleton<InvadeSupportCityConfig>();
            foreach (var (statData, entity) in SystemAPI.Query<
                     RefRW<ArmyGroupStatData>>().WithAll<InvadingSupportCityTag>().WithEntityAccess())
            {
                statData.ValueRW.totalCurrentHp -= config.ReduceHpPerHour;
                if (statData.ValueRW.totalCurrentHp <= 0)
                {
                    ArmyGroupUtils.DestroyArmyGroup(entity,ecb, state.EntityManager);
                }
            }
        }

   
    }
}