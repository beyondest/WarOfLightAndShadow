using SparFlame.GamePlaySystem.CameraControl;
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
            state.RequireForUpdate<PlayerFactionData>();
            state.RequireForUpdate<GamingTag>();
            _enemyCrystal = SystemAPI.QueryBuilder().WithAll<AITag>().WithAll<CrystalDef>().WithAll<StatData>().Build();
            
            _playerCrystal = SystemAPI.QueryBuilder().WithAll<PlayerTag>().WithAll<CrystalDef>().WithAll<StatData>().Build();
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
            var playerFaction = SystemAPI.GetSingleton<PlayerFactionData>().Value;
            
            var enemyStats = _enemyCrystal.ToComponentDataArray<StatData>(Allocator.Temp);
            var playerStats = _playerCrystal.ToComponentDataArray<StatData>(Allocator.Temp);
            var enemyEntities = _enemyCrystal.ToEntityArray(Allocator.Temp);
            var playerEntities = _playerCrystal.ToEntityArray(Allocator.Temp);
            var enemyMaxHp = 0f;
            var enemyCurHp = 0f;
            var playerMaxHp = 0f;
            var playerCurHp = 0f;
            // Insight enemy crystal, and if enemy is light, single core crystal
            var enemyCrystalValidCount = 0;
            var playerCrystalCount = 0;

            for (var i = 0; i < enemyStats.Length; i++)
            {
                var data = enemyStats[i];
                var entity = enemyEntities[i];
                if(playerFaction == FactionTag.Enemy && !SystemAPI.HasComponent<LightSingleCrystalTag>(entity))continue;
                if(!SystemAPI.IsComponentEnabled<InCameraView>(entity))continue;
                enemyMaxHp += data.MaxValue + data.Bonus;
                enemyCurHp += data.CurValue + data.Bonus;
                enemyCrystalValidCount ++;
            }

            for (var i = 0; i < playerStats.Length; i++)
            {
                var data = playerStats[i];
                var entity = playerEntities[i];
                // If player is light, only count the single core crystal stat; otherwise count all crystals
                if (playerFaction == FactionTag.Ally && !SystemAPI.HasComponent<LightSingleCrystalTag>(entity))continue;
                playerMaxHp += data.MaxValue;
                playerCurHp += data.CurValue;
                playerCrystalCount ++;
            }

            SystemAPI.SetSingleton(new EnemyCrystalInfo
            {
                TotalCount = enemyStats.Length,
                InSightValidCount = enemyCrystalValidCount,
                CurTotalHp = enemyCurHp,
                MaxTotalHp = enemyMaxHp,
            });
            SystemAPI.SetSingleton(new PlayerCrystalInfo
            {
                TotalCount = playerCrystalCount,
                CurTotalHp = playerCurHp,
                MaxTotalHp = playerMaxHp,
            });
            
            
        }
    }
}