using SparFlame.GamePlaySystem.General;
using SparFlame.GamePlaySystem.Interact;
using SparFlame.GamePlaySystem.Waves;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;

namespace SparFlame.GamePlaySystem.EnemyAI
{
    public partial struct EnemyCrystalInfoUpdater : ISystem
    {
        private EntityQuery _enemyCrystal;
        private EntityQuery _playerCrystal;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<GamingTag>();
            _enemyCrystal = SystemAPI.QueryBuilder().WithAll<AITag>().WithAll<CoreCrystalTag>().WithAll<StatData>().Build();
            _playerCrystal = SystemAPI.QueryBuilder().WithAll<PlayerTag>().WithAll<CoreCrystalTag>().WithAll<StatData>().Build();
            state.EntityManager.CreateSingleton(new EnemyCrystalInfo());
            state.EntityManager.CreateSingleton(new PlayerCrystalInfo());

        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            UpdateCrystalInfo(ref state);
        }

 
        private void UpdateCrystalInfo(ref SystemState state)
        {
            var enemyStats = _enemyCrystal.ToComponentDataArray<StatData>(Allocator.Temp);
            var playerStats = _playerCrystal.ToComponentDataArray<StatData>(Allocator.Temp);
            var enemyMaxHp = 0f;
            var enemyCurHp = 0f;
            var playerMaxHp = 0f;
            var playerCurHp = 0f;
            foreach (var data in enemyStats)
            {
                enemyMaxHp += data.MaxValue;
                enemyCurHp += data.CurValue;
            }

            foreach (var data in playerStats)
            {
                playerMaxHp += data.MaxValue;
                playerCurHp += data.CurValue;
            }
            SystemAPI.SetSingleton(new EnemyCrystalInfo
            {
                TotalCount = enemyStats.Length,
                CurTotalHp = enemyCurHp,
                MaxTotalHp = enemyMaxHp,
            });
            SystemAPI.SetSingleton(new PlayerCrystalInfo
            {
                TotalCount = playerStats.Length,
                CurTotalHp = playerCurHp,
                MaxTotalHp = playerMaxHp,
            });
            
        }
    }
}