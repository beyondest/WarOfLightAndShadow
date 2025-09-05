// using SparFlame.GamePlaySystem.General;
// using SparFlame.GamePlaySystem.Map;
// using SparFlame.GamePlaySystem.Resource;
// using Unity.Burst;
// using Unity.Entities;
// using Unity.Mathematics;
// using Unity.Transforms;
//
// namespace SparFlame.GamePlaySystem.RandomSpawn
// {
//     [UpdateInGroup(typeof(InitializationSystemGroup))]
//     [UpdateAfter(typeof(MapInitializeSystem))]
//     public partial struct PlayerFirstBaseSpawnSystem : ISystem
//     {
//         [BurstCompile]
//         public void OnCreate(ref SystemState state)
//         {
//             state.RequireForUpdate<ConstructGridSize>();
//             state.RequireForUpdate<CrystalAffectMapRadiusSq>();
//             state.RequireForUpdate<MapInitInfo>();
//             state.RequireForUpdate<PlayerFactionData>();
//             state.RequireForUpdate<PlayerFirstBaseSpawnConfig>();
//             state.RequireForUpdate<MapInfo>();
//             state.RequireForUpdate<GameStatusData>();
//
//         }
//
//         // [BurstCompile]
//         public void OnUpdate(ref SystemState state)
//         {
//             var gameStatusData = SystemAPI.GetSingleton<GameStatusData>();
//             if (gameStatusData.Value == GameStatus.Init)
//             {
//                 var mapInfo = SystemAPI.GetSingleton<MapInfo>();
//                 var tileSize = SystemAPI.GetSingleton<MapInitInfo>().tileSize;
//                 var playerFaction = SystemAPI.GetSingleton<PlayerFactionData>();
//                 var playerPos = float2.zero;
//                 ref var rnd = ref SystemAPI.GetSingletonRW<GeneralRandom>().ValueRW;
//                 var config = SystemAPI.GetSingleton<PlayerFirstBaseSpawnConfig>();
//                 Entity basePrefab = Entity.Null;
//                 
//                 var spawnableOuterSquareSize =
//                     mapInfo.OuterSquareSize - 2 * math.sqrt( SystemAPI.GetSingleton<CrystalAffectMapRadiusSq>().Value);
//                 switch (playerFaction.Value)
//                 {
//                     case FactionTag.Enemy:
//                         playerPos = MapUtils.SampleSquareRing(spawnableOuterSquareSize, mapInfo.InnerSquareSize,
//                             mapInfo.WorldCenter.xz,
//                             ref rnd.Rnd);
//                         basePrefab = config.DarkPrefab;
//                         break;
//                     case FactionTag.Ally:
//                     {
//                         // var sub = (mapInfo.OuterSquareSize - mapInfo.InnerSquareSize) / 2f;
//                         // var startX = -0.5f * tileSize + sub;
//                         playerPos = new float2(285f, 285f);
//                         // playerPos = MapUtils.SampleSquareRing(mapInfo.InnerSquareSize, mapInfo.CenterRadius * 2f, mapInfo.WorldCenter.xz,
//                         //     ref rnd.Rnd);
//                         // playerPos = rnd.Rnd.NextFloat2(new float2(startX, startX),
//                         //     new float2(startX + mapInfo.InnerSquareSize, startX + mapInfo.InnerSquareSize));
//                         basePrefab = config.LightPrefab;
//                         
//                         break;
//                     }
//                     case FactionTag.Neutral:
//                         break;
//                 }
//
//                 if (SystemAPI.HasSingleton<DebugTag>() && SystemAPI.TryGetSingleton(out RandomSpawnDebug debug) && debug.fixPlayerFirstPawnPosition)
//                 {
//                     playerPos = debug.playerFirstSpawnPosition.xz;
//                 }
//                 GeneralUtils.GetSnapGridPosition(new float3(playerPos.x, 0f, playerPos.y),
//                     0f,SystemAPI.GetComponent<GeneralAttr>(basePrefab).BoxColliderSize,SystemAPI.GetSingleton<ConstructGridSize>().Value,
//                     out var snapPos);
//                 playerPos.x = snapPos.x;
//                 playerPos.y = snapPos.z;
//                 var entity = state.EntityManager.Instantiate(basePrefab);
//                 state.EntityManager.AddComponent<GameplayEntityTag>(entity);
//                 
//                 SystemAPI.SetComponent(entity, new LocalTransform
//                 {
//                     Position = new float3(playerPos.x, 0f,playerPos.y),
//                     Rotation = quaternion.identity,
//                     Scale = 1f
//                 });
//                 
//                 var firstPosSingleton = state.EntityManager.CreateEntity();
//                 state.EntityManager.AddComponent<PlayerFirstBasePos>(firstPosSingleton);
//                 state.EntityManager.SetComponentData(firstPosSingleton, new PlayerFirstBasePos
//                 {
//                     Value = new float3(playerPos.x, 0f, playerPos.y)
//                 });
//                 state.EntityManager.AddComponent<GameplayEntityTag>(firstPosSingleton);
//                 
//                 // var changeOccupiedTagRequest = state.EntityManager.CreateEntity();
//                 // state.EntityManager.AddComponent<GameplayEntityTag>(changeOccupiedTagRequest);
//                 //
//                 // state.EntityManager.AddComponent<ChangeOccupiedTagRequest>(changeOccupiedTagRequest);
//                 // state.EntityManager.SetComponentData(changeOccupiedTagRequest, new ChangeOccupiedTagRequest
//                 // {
//                 //     CrystalFaction = playerFaction.Value,
//                 //     CrystalPos = new float3(playerPos.x, 0f, playerPos.y),
//                 //     IsDestroyed = false
//                 // });
//             }
//         }
//
//     }
// }