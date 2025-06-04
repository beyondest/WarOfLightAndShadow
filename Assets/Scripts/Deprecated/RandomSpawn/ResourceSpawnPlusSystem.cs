// using SparFlame.GamePlaySystem.General;
// using SparFlame.GamePlaySystem.Interact;
// using SparFlame.GamePlaySystem.Map;
// using SparFlame.GamePlaySystem.Resource;
// using Unity.Burst;
// using Unity.Collections;
// using Unity.Entities;
// using Unity.Mathematics;
// using Unity.Transforms;
// using Random = Unity.Mathematics.Random;
//
// namespace SparFlame.GamePlaySystem.RandomSpawn
// {
//     [UpdateInGroup(typeof(InitializationSystemGroup))]
//     [UpdateAfter(typeof(EnvSpawnPlusSystem))]
//     public partial struct ResourceSpawnPlusSystem : ISystem
//     {
//         // TimePoints to a resource type to amount
//         private NativeHashMap<int, NativeHashMap<int, int>> _resourceSpawnDatabase;
//
//         // Resource type to special data
//         private NativeHashMap<int, ResourceTileTypeSpecialData> _resourceTileTypeSpecialData;
//
//         private NativeHashSet<int> _renewableResources;
//
//         private NativeHashSet<int> _shouldRespawnResources;
//
//         // int resource type to prefab
//         private NativeParallelMultiHashMap<int, ProbabilityPrefabEntry> _resourcePrefabDatabase;
//
//         [BurstCompile]
//         public void OnCreate(ref SystemState state)
//         {
//             state.RequireForUpdate<MapInfo>();
//             state.RequireForUpdate<GameTimeData>();
//             state.RequireForUpdate<EndInitializationEntityCommandBufferSystem.Singleton>();
//             state.RequireForUpdate<MapInitInfo>();
//             state.RequireForUpdate<GeneralRandom>();
//             state.RequireForUpdate<ResourceSpawnState>();
//             state.RequireForUpdate<GlobalResourceDataTag>();
//             state.RequireForUpdate<ResourceSpawnSystemConfig>();
//             state.RequireForUpdate<ResourceSpawnData>();
//             state.RequireForUpdate<GameStatusData>();
//         }
//
//         [BurstCompile]
//         public void OnUpdate(ref SystemState state)
//         {
//             if (!_resourceSpawnDatabase.IsCreated)
//                 Initialize();
//             var gameStatusData = SystemAPI.GetSingleton<GameStatusData>();
//
//             var curResourceState = SystemAPI.GetSingletonRW<ResourceSpawnState>();
//             if (gameStatusData.Value == GameStatus.Init)
//             {
//                 curResourceState.ValueRW.LastTimePoint = 0;
//                 SpawnResource(ref state,0,
//                     true);
//                 var curTimePointMap = _resourceSpawnDatabase[0];
//                 // Update after structural change
//                 var globalResourceDataCenter = SystemAPI.GetBuffer<ResourceTypeToAvailableAmount>(
//                     SystemAPI.GetSingletonEntity<GlobalResourceDataTag>());
//                 for (var i = 0; i < globalResourceDataCenter.Length; i++)
//                 {
//                     var data = globalResourceDataCenter[i];
//                     if (!curTimePointMap.ContainsKey((int)data.ResourceType)) continue;
//                     data.Amount = curTimePointMap[(int)data.ResourceType];
//                     globalResourceDataCenter[i] = data;
//                 }
//                 return;
//             }
//             if (gameStatusData.Value != GameStatus.Gaming) return;
//
//
//         }
//         
//         /*var curTime = SystemAPI.GetSingleton<GameTimeData>().ElapsedTime;
//         var curTimePoints =
//             GeneralUtils.GetPointData<ResourcePointData, int>(curTime,
//                 SystemAPI.GetSingletonBuffer<ResourcePointData>());
//         var timePointReached = curTimePoints != curResourceState.ValueRO.LastTimePoint;
//         var notEnough = GlobalDataShouldRespawn(globalResourceDataCenter, curTimePoints);
//         if (timePointReached
//             || notEnough)
//         {
//             curResourceState.ValueRW.LastTimePoint = curTimePoints;
//             SpawnResource(ref state, globalResourceDataCenter, curTimePoints,
//                 timePointReached);
//             var curTimePointMap = _resourceSpawnDatabase[curTimePoints];
//             for (var i = 0; i < globalResourceDataCenter.Length; i++)
//             {
//                 var data = globalResourceDataCenter[i];
//                 if (!curTimePointMap.ContainsKey((int)data.ResourceType)) continue;
//                 data.Amount = curTimePointMap[(int)data.ResourceType];
//                 globalResourceDataCenter[i] = data;
//             }
//         }*/
//
//         [BurstCompile]
//         public void OnDestroy(ref SystemState state)
//         {
//             if (_resourceSpawnDatabase.IsCreated)
//             {
//                 foreach (var pair in _resourceSpawnDatabase)
//                 {
//                     pair.Value.Dispose();
//                 }
//
//                 _resourceSpawnDatabase.Dispose();
//             }
//
//             if (_renewableResources.IsCreated)
//                 _renewableResources.Dispose();
//             if (_shouldRespawnResources.IsCreated)
//                 _shouldRespawnResources.Dispose();
//             if (_resourcePrefabDatabase.IsCreated)
//             {
//                 _resourcePrefabDatabase.Dispose();
//             }
//
//             if (_resourceTileTypeSpecialData.IsCreated)
//                 _resourceTileTypeSpecialData.Dispose();
//         }
//
//         private void SpawnResource(
//             ref SystemState state,
//             int curTimePoints,
//             bool timePointReached
//         )
//         {
//             var occupiedConfig = SystemAPI.GetSingleton<MapInitInfo>();
//             var tileSize = occupiedConfig.tileSize;
//
//             var curResourceMap = _resourceSpawnDatabase[curTimePoints];
//             if (timePointReached)
//             {
//                 _shouldRespawnResources.Clear();
//                 foreach (var pair in curResourceMap)
//                 {
//                     if(pair.Value != 0)
//                         _shouldRespawnResources.Add(pair.Key);
//                 }
//             }
//             var specialDatas = SystemAPI.GetSingletonBuffer<ResourceTileTypeSpecialData>();
//
//             ref var rnd = ref SystemAPI.GetSingletonRW<GeneralRandom>().ValueRW.Rnd;
//             CalculateSpawnTiles(ref state, _shouldRespawnResources, specialDatas, out var typeToSpawnableTiles,
//                 out var totalTypeWeight);
//             SpawnRes(ref state, curResourceMap, typeToSpawnableTiles, _resourcePrefabDatabase, totalTypeWeight, ref rnd,
//                 new float3(tileSize, tileSize, tileSize));
//             foreach (var pair in typeToSpawnableTiles)
//             {   
//                 pair.Value.Dispose();
//             }
//             typeToSpawnableTiles.Dispose();
//             totalTypeWeight.Dispose();
//
//         }
//
//
//         private void Initialize()
//         {
//             var buffer = SystemAPI.GetSingletonBuffer<ResourceSpawnData>();
//             var buffer2 = SystemAPI.GetSingletonBuffer<RenewableResourceType>();
//             var buffer3 = SystemAPI.GetSingletonBuffer<ResourceEntityPrefabData>();
//             var buffer4 = SystemAPI.GetSingletonBuffer<ResourceTileTypeSpecialData>();
//             _resourceSpawnDatabase = new NativeHashMap<int, NativeHashMap<int, int>>(5, Allocator.Persistent);
//             _resourceTileTypeSpecialData = new NativeHashMap<int, ResourceTileTypeSpecialData>(5, Allocator.Persistent);
//             _shouldRespawnResources = new NativeHashSet<int>(5, Allocator.Persistent);
//             _resourcePrefabDatabase =
//                 new NativeParallelMultiHashMap<int, ProbabilityPrefabEntry>(5, Allocator.Persistent);
//             _renewableResources = new NativeHashSet<int>(5, Allocator.Persistent);
//
//             var scale = 1f;
//             if (SystemAPI.HasSingleton<DebugTag>() && SystemAPI.TryGetSingleton(out RandomSpawnDebug debug))
//             {
//                 scale = debug.resourceSpawnAmountScale;
//             }
//             foreach (var data in buffer)
//             {
//                 if (!_resourceSpawnDatabase.ContainsKey(data.TimePoints))
//                 {
//                     _resourceSpawnDatabase.Add(data.TimePoints,
//                         new NativeHashMap<int, int>(5, Allocator.Persistent));
//                 }
//
//                 var subDict = _resourceSpawnDatabase[data.TimePoints];
//                 subDict[(int)data.ResourceType] =(int)(data.Amount * scale);
//             }
//
//             foreach (var type in buffer2)
//             {
//                 _renewableResources.Add((int)type.ResourceType);
//             }
//
//             foreach (var data in buffer3)
//             {
//                 _resourcePrefabDatabase.Add((int)data.Type, new ProbabilityPrefabEntry
//                 {
//                     Prefab = data.Prefab,
//                     Probability = data.Probability,
//                     AmountRange = data.AmountRange
//                 });
//             }
//
//             foreach (var specialData in buffer4)
//             {
//                 _resourceTileTypeSpecialData.Add((int)specialData.Type, specialData);
//             }
//         }
//
//         private void CalculateSpawnTiles(ref SystemState state, in NativeHashSet<int> shouldSpawnTypes,
//             in DynamicBuffer<ResourceTileTypeSpecialData> specialDatas,
//             out NativeHashMap<int, NativeList<TileSpawnEntry>> typeToSpawnableTiles,
//             out NativeHashMap<int, float> totalTypeWeight)
//         {
//             var typeToSpawnableTiles0 = new NativeHashMap<int, NativeList<TileSpawnEntry>>(10, Allocator.TempJob);
//             var totalTypeWeight0 = new NativeHashMap<int, float>(10, Allocator.TempJob);
//             
//             foreach (var envType in shouldSpawnTypes)
//             {
//                 totalTypeWeight0.Add(envType, 0f);
//                 typeToSpawnableTiles0.Add(envType, new NativeList<TileSpawnEntry>(Allocator.TempJob));
//             }
//             foreach (var (refRoTileData, refROccupiedTag, transform, entity) in SystemAPI
//                          .Query<RefRO<TileData>, RefRO<OccupiedTag>, RefRO<LocalTransform>>().WithEntityAccess())
//             {
//                 if (refROccupiedTag.ValueRO.Faction != FactionTag.Neutral) continue;
//                 var thisTileData = refRoTileData.ValueRO;
//  
//                 foreach (var resourceType in shouldSpawnTypes)
//                 {
//                     var envTypeSpecialData = specialDatas[resourceType];
//                     var typeAWeight = 0f;
//                     var typeBWeight = 0f;
//                    
//                     foreach (var spawnableTile in envTypeSpecialData.SpawnableTiles)
//                     {
//                         
//                         // This tile as tile A should summon this env type weight is typeAWeight
//                         if (spawnableTile.tileType == thisTileData.TypeA)
//                             typeAWeight = spawnableTile.weight * thisTileData.WeightA;
//                         // This tile as tile B should summon this env type weight is typeBWeight
//                         if (spawnableTile.tileType == thisTileData.TypeB)
//                             typeBWeight = spawnableTile.weight * (1 - thisTileData.WeightA);
//                     }
//
//                     var wei = typeAWeight + typeBWeight;
//                     if (wei != 0f)
//                     {
//                         totalTypeWeight0[resourceType] += wei;
//                         var newEntry = new TileSpawnEntry
//                         {
//                             TileEntity = entity,
//                             WeightAccumulated = totalTypeWeight0[resourceType],
//                             Position = transform.ValueRO.Position
//                         };
//                         typeToSpawnableTiles0[resourceType].Add(newEntry);
//                     }
//                 }
//             }
//
//             typeToSpawnableTiles = typeToSpawnableTiles0;
//             totalTypeWeight = totalTypeWeight0;
//         }
//
//         private void SpawnRes(ref SystemState state, in NativeHashMap<int, int> typeToTotalAmount,
//             in NativeHashMap<int, NativeList<TileSpawnEntry>> typeToSpawnableTiles,
//             NativeParallelMultiHashMap<int, ProbabilityPrefabEntry> prefabDatabase,
//             in NativeHashMap<int, float> typeToTotalWeight, ref Random rnd,
//             in float3 tileSize)
//         {
//             var ecb = new EntityCommandBuffer(Allocator.Temp);
//             var edgeMargin = SystemAPI.GetSingleton<ResourceSpawnSystemConfig>().edgeMargin;
//             var mapInfo = SystemAPI.GetSingleton<MapInfo>();
//             var minPosValue = -tileSize.x/2f + edgeMargin;
//             var maxPosValue = -tileSize.x/2f + mapInfo.OuterSquareSize - edgeMargin;
//             var minPos = new float3(minPosValue, 0f, minPosValue);
//             var maxPos = new float3(maxPosValue, 0f, maxPosValue);
//             foreach (var envPair in typeToTotalWeight)
//             {
//                 var resourceType = envPair.Key;
//                 var totalWeight = envPair.Value;
//                 var totalAmount = typeToTotalAmount[resourceType];
//                 var spawnableTiles = typeToSpawnableTiles[resourceType];
//                 if(spawnableTiles.Length == 0) continue;
//                 while (totalAmount > 0)
//                 {
//                     var pick = rnd.NextFloat(0f, totalWeight);
//                     foreach (var entry in spawnableTiles)
//                     {
//                         if (entry.WeightAccumulated >= pick)
//                         {
//                             var chosen = GeneralUtils.RandomChoosePrefab(ref rnd, prefabDatabase, resourceType);
//                             var amount = rnd.NextInt((int)chosen.AmountRange.lower, (int)chosen.AmountRange.upper);
//                             var offset = new float3(
//                                 rnd.NextFloat(-tileSize.x * 0.5f, tileSize.x * 0.5f),
//                                 0f,
//                                 rnd.NextFloat(-tileSize.z * 0.5f, tileSize.z * 0.5f)
//                             );
//                             var spawnPos = entry.Position + offset;
//                             spawnPos = math.clamp(spawnPos,minPos, maxPos );
//                             var entity = ecb.Instantiate(chosen.Prefab);
//                             ecb.AddComponent<GameplayEntityTag>(entity);
//                             ecb.SetComponent(entity, new LocalTransform
//                             {
//                                 Position = spawnPos,
//                                 Rotation = quaternion.identity,
//                                 Scale = 1f
//                             });
//                             ecb.SetComponent(entity, new StatData
//                             {
//                                 MaxValue = amount,
//                                 CurValue = amount
//                             });
//                             totalAmount -= amount;
//                             break;
//                         }
//                     }
//                 }
//             }
//
//             ecb.Playback(state.EntityManager);
//             ecb.Dispose();
//         }
//     }
// }