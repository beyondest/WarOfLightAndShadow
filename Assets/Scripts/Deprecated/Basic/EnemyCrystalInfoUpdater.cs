// using SparFlame.Components.General;
// using SparFlame.Components.MainGameplay;
// using SparFlame.Components.SubGameplay;
// using Unity.Burst;
// using Unity.Collections;
// using Unity.Entities;
//
// namespace SparFlame.Systems.SubGameplay.EnemyAI
// {
//     public partial struct EnemyCrystalInfoUpdater : ISystem
//     {
//         private EntityQuery _enemyCrystal;
//         private EntityQuery _playerCrystal;
//
//         [BurstCompile]
//         public void OnCreate(ref SystemState state)
//         {
//             state.RequireForUpdate<PlayerFactionData>();
//             state.RequireForUpdate<SubGamingTag>();
//             _enemyCrystal = SystemAPI.QueryBuilder().WithAll<AITag>().WithAll<CrystalDef>().WithAll<StatData>().Build();
//             
//             _playerCrystal = SystemAPI.QueryBuilder().WithAll<PlayerTag>().WithAll<CrystalDef>().WithAll<StatData>().Build();
//             state.EntityManager.CreateSingleton(new EnemyCrystalInfo());
//             state.EntityManager.CreateSingleton(new PlayerCrystalInfo());
//
//         }
//
//         [BurstCompile]
//         public void OnUpdate(ref SystemState state)
//         {
//             UpdateCrystalInfo(ref state);
//         }
//
//  
//         private void UpdateCrystalInfo(ref SystemState state)
//         {
//             
//             var enemyStats = _enemyCrystal.ToComponentDataArray<StatData>(Allocator.Temp);
//             var playerStats = _playerCrystal.ToComponentDataArray<StatData>(Allocator.Temp);
//             var enemyEntities = _enemyCrystal.ToEntityArray(Allocator.Temp);
//             var enemyMaxHp = 0f;
//             var enemyCurHp = 0f;
//             var playerMaxHp = 0f;
//             var playerCurHp = 0f;
//             // Insight enemy crystal, and if enemy is light, single core crystal
//             var enemyCrystalValidCount = 0;
//             var playerCrystalCount = 0;
//
//             for (var i = 0; i < enemyStats.Length; i++)
//             {
//                 var data = enemyStats[i];
//                 var entity = enemyEntities[i];
//                 if(!SystemAPI.IsComponentEnabled<InCameraView>(entity))continue;
//                 enemyMaxHp += data.maxValue + data.bonus;
//                 enemyCurHp += data.curValue + data.bonus;
//                 enemyCrystalValidCount ++;
//             }
//
//             foreach (var data in playerStats)
//             {
//                 playerMaxHp += data.maxValue;
//                 playerCurHp += data.curValue;
//                 playerCrystalCount ++;
//             }
//
//             SystemAPI.SetSingleton(new EnemyCrystalInfo
//             {
//                 CurTotalHp = enemyCurHp,
//                 MaxTotalHp = enemyMaxHp,
//             });
//             SystemAPI.SetSingleton(new PlayerCrystalInfo
//             {
//                 CurTotalHp = playerCurHp,
//                 MaxTotalHp = playerMaxHp,
//             });
//             
//             
//         }
//     }
// }