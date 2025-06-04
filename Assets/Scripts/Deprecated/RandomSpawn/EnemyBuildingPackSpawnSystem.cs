// using SparFlame.GamePlaySystem.General;
// using SparFlame.GamePlaySystem.Waves;
// using Unity.Burst;
// using Unity.Collections;
// using Unity.Entities;
// using Unity.Mathematics;
// using Unity.Transforms;
//
// namespace SparFlame.GamePlaySystem.EnemyAI
// {
//     [UpdateInGroup(typeof(InitializationSystemGroup))]
//     [UpdateAfter(typeof(WaveSystem))]
//     public partial struct EnemyBuildingPackSpawnSystem : ISystem
//     {
//         private NativeParallelMultiHashMap<int, ProbabilityPrefabEntry> _wavePoint2Entries;
//         private NativeHashMap<int, IntervalCountPair> _wavePoint2BuildingSpawnIntervalCount;
//         private NativeList<int> _wavePoints;
//         private EntityQuery _enemyBuildingPackQuery;
//
//         private struct IntervalCountPair
//         {
//             public int Interval;
//             public int Count;
//         }
//
//         private struct EnemyBuildingNextSpawnTime : IComponentData
//         {
//             public float Value;
//         }
//
//         [BurstCompile]
//         public void OnCreate(ref SystemState state)
//         {
//             state.RequireForUpdate<DarkEnemyDatabaseTag>();
//             state.RequireForUpdate<LightEnemyDatabaseTag>();
//             state.RequireForUpdate<GameStatusData>();
//             state.RequireForUpdate<EnemyBuildingNextSpawnTime>();
//             state.RequireForUpdate<GameTimeData>();
//             state.RequireForUpdate<PlayerFactionData>();
//             state.RequireForUpdate<EndInitializationEntityCommandBufferSystem.Singleton>();
//             state.RequireForUpdate<GameWaveData>();
//             state.EntityManager.CreateSingleton(new EnemyBuildingNextSpawnTime
//             {
//                 Value = -1f
//             });
//             _enemyBuildingPackQuery = SystemAPI.QueryBuilder().WithAll<AITag>().WithAll<BuildingPackSquareSize>()
//                 .WithAll<LocalTransform>().Build();
//         }
//
//         [BurstCompile]
//         public void OnUpdate(ref SystemState state)
//         {
//             ref var nextSpawnTime = ref SystemAPI.GetSingletonRW<EnemyBuildingNextSpawnTime>().ValueRW;
//
//             var gameStatusData = SystemAPI.GetSingleton<GameStatusData>().Value;
//             if (gameStatusData == GameStatus.Init)
//             {
//                 nextSpawnTime.Value = -1f;
//                 if (_wavePoints.IsCreated)
//                     Deinitialize();
//                 Initialize(ref state);
//
//                 return;
//             }
//
//             if (gameStatusData != GameStatus.Gaming) return;
//             // var ecb = new EntityCommandBuffer(Allocator.TempJob);
//             // var job = new LateUpdateBuildingPackPosJob
//             // {
//             //     ECB = ecb
//             // }.Schedule(state.Dependency);
//             // job.Complete();
//             // ecb.Dispose();
//
//             // var waveData = SystemAPI.GetSingleton<GameWaveData>();
//             // if (waveData.IfWaveUpdateThisFrame || SystemAPI.GetSingleton<GameTimeData>().ElapsedTime >
//             //     nextSpawnTime.Value)
//             // {
//             //     RandomSpawnEnemyBuildingPack(ref state, waveData, ref nextSpawnTime);
//             // }
//         }
//
//         private void RandomSpawnEnemyBuildingPack(ref SystemState state, in GameWaveData waveData,
//             ref EnemyBuildingNextSpawnTime nextSpawnTime)
//         {
//             var ecb = new EntityCommandBuffer(Allocator.Temp);
//
//             var existBuildingLocs = _enemyBuildingPackQuery.ToComponentDataArray<LocalTransform>(Allocator.Temp);
//             var existBuildingSquareSize =
//                 _enemyBuildingPackQuery.ToComponentDataArray<BuildingPackSquareSize>(Allocator.Temp);
//             var existLocs = new NativeList<LocalTransform>(Allocator.Temp);
//             var existSquares = new NativeList<BuildingPackSquareSize>(Allocator.Temp);
//             existLocs.AddRange(existBuildingLocs);
//             existSquares.AddRange(existBuildingSquareSize);
//             var curWavePoint = GeneralUtils.GetPoint(waveData.CurWaveIndex, _wavePoints);
//             var spawnIntervalCount = _wavePoint2BuildingSpawnIntervalCount[curWavePoint];
//             var interval = spawnIntervalCount.Interval;
//             if (SystemAPI.HasSingleton<DebugTag>() && SystemAPI.TryGetSingleton(out EnemyAIDebug debugData))
//             {
//                 interval = (int)(interval / debugData.buildingSpawnSpeedScale);
//             }
//
//             nextSpawnTime.Value = SystemAPI.GetSingleton<GameTimeData>().ElapsedTime + interval;
//
//             var playerFaction = SystemAPI.GetSingleton<PlayerFactionData>().Value;
//             var enemyFaction = ~playerFaction;
//             ref var rnd = ref SystemAPI.GetSingletonRW<GeneralRandom>().ValueRW;
//
//             for (int i = 0; i < spawnIntervalCount.Count; i++)
//             {
//                 var entry = GeneralUtils.RandomChoosePrefab(ref rnd.Rnd, _wavePoint2Entries, curWavePoint);
//                 var squareSize = SystemAPI.GetComponent<BuildingPackSquareSize>(entry.Prefab);
//                 var thisPrefabCount = rnd.Rnd.NextInt((int)entry.AmountRange.lower, (int)entry.AmountRange.upper);
//                 var spawnableOuterSquareSize =
//                     mapInfo.OuterSquareSize - 2 * math.max(squareSize.Value.x, squareSize.Value.y);
//                 for (int j = 0; j < thisPrefabCount; j++)
//                 {
//                     float2 spawnPos;
//                     int maxTries = 30;
//                     int tries = 0;
//                     do
//                     {
//                         spawnPos = enemyFaction switch
//                         {
//                             FactionTag.Enemy => MapUtils.SampleSquareRing(spawnableOuterSquareSize,
//                                 mapInfo.InnerSquareSize,
//                                 mapInfo.WorldCenter.xz, ref rnd.Rnd),
//                             FactionTag.Ally => MapUtils.SampleSquareRing(mapInfo.InnerSquareSize,
//                                 mapInfo.CenterRadius * 2f,
//                                 mapInfo.WorldCenter.xz, ref rnd.Rnd),
//                             _ => float2.zero
//                         };
//
//                         if (SystemAPI.HasSingleton<DebugTag>() &&
//                             SystemAPI.TryGetSingleton(out EnemyAIDebug debug) && debug.enableFixBuildingSpawnPos)
//                         {
//                             spawnPos = debug.buildingFixSpawnPos.xz;
//                             break;
//                         }
//
//                         tries++;
//                     } while (EnemyAIUtils.IsOverlapping(spawnPos, squareSize.Value, existLocs, existSquares) &&
//                              tries < maxTries);
//
//                     // if tries many times but cannot find an available pos, then put it to center
//                     if (tries >= maxTries)
//                     {
//                         spawnPos = mapInfo.WorldCenter.xz;
//                     }
//
//                     
//                     var newLoc = new LocalTransform
//                     {
//                         Position = new float3(spawnPos.x, 0f, spawnPos.y),
//                         Rotation = GeneralUtils.NextQuaternion(ref rnd.Rnd, onlyXZ: true),
//                         Scale = 1f
//                     };
//                     var buildingPack = InstantiateChildrenWithNewParent(ref state, entry.Prefab, newLoc);
//                     state.EntityManager.AddComponent<BuildingPackSquareSize>(buildingPack);
//                     state.EntityManager.SetComponentData(buildingPack, squareSize);
//                     state.EntityManager.AddComponent<GameplayEntityTag>(buildingPack);
//
//                     // var buildingPack = state.EntityManager.Instantiate(entry.Prefab);
//                     // state.EntityManager.AddComponent<GameplayEntityTag>(buildingPack);
//
//
//                     state.EntityManager.SetComponentData(buildingPack, newLoc);
//
//                     // var linkedGroup = state.EntityManager.GetBuffer<LinkedEntityGroup>(buildingPack);
//
//                     // foreach (var linked in linkedGroup)
//                     // {
//                     //     var childEntity = linked.Value;
//                     //
//                     //     // 跳过 root 自己（如果需要）
//                     //     if (childEntity == buildingPack)
//                     //         continue;
//                     //
//                     //     if (!state.EntityManager.HasComponent<LocalTransform>(childEntity))
//                     //         continue;
//                     //
//                     //     var childLocal = state.EntityManager.GetComponentData<LocalTransform>(childEntity);
//                     //
//                     //     // 将 local transform 转为 world transform（旋转 + 平移）
//                     //     var worldPos = newLoc.Position + math.rotate(newLoc.Rotation, childLocal.Position);
//                     //     var worldRot = math.mul(newLoc.Rotation, childLocal.Rotation);
//                     //
//                     //     // 替换为新的世界坐标
//                     //     state.EntityManager.SetComponentData(childEntity, new LocalTransform
//                     //     {
//                     //         Position = worldPos,
//                     //         Rotation = worldRot,
//                     //         Scale = childLocal.Scale // 保留原有缩放
//                     //     });
//                     //
//                     //     // ⚠️（可选）移除父子依赖关系，避免 Transform 系统覆盖这个手动设定
//                     //     if (state.EntityManager.HasComponent<Parent>(childEntity))
//                     //         ecb.RemoveComponent<Parent>(childEntity);
//                     // }
//
//                     existLocs.Add(newLoc);
//                     existSquares.Add(squareSize);
//                 }
//             }
//
//             ecb.Playback(state.EntityManager);
//             ecb.Dispose();
//             existLocs.Dispose();
//             existSquares.Dispose();
//             existBuildingLocs.Dispose();
//             existBuildingSquareSize.Dispose();
//         }
//
//         private void Initialize(ref SystemState state)
//         {
//             var lightEntity = SystemAPI.GetSingletonEntity<LightEnemyDatabaseTag>();
//             var darkEntity = SystemAPI.GetSingletonEntity<DarkEnemyDatabaseTag>();
//             var entity = ~SystemAPI.GetSingleton<PlayerFactionData>().Value == FactionTag.Ally
//                 ? lightEntity
//                 : darkEntity;
//             var buffer = SystemAPI.GetBuffer<EnemyBuildingSpawnData>(entity);
//             var buffer2 = SystemAPI.GetBuffer<EnemyBuildingPackData>(entity);
//             _wavePoints = new NativeList<int>(Allocator.Persistent);
//             _wavePoint2Entries = new NativeParallelMultiHashMap<int, ProbabilityPrefabEntry>(2, Allocator.Persistent);
//             _wavePoint2BuildingSpawnIntervalCount = new NativeHashMap<int, IntervalCountPair>(2, Allocator.Persistent);
//             foreach (var data in buffer)
//             {
//                 _wavePoints.Add(data.WavePoint);
//                 _wavePoint2BuildingSpawnIntervalCount.Add(data.WavePoint,
//                     new IntervalCountPair { Interval = data.Interval, Count = data.SpawnPackCount });
//             }
//
//             foreach (var data in buffer2)
//             {
//                 _wavePoint2Entries.Add(data.WavePoint, data.ProbabilityPrefab);
//             }
//         }
//
//
//         private void Deinitialize()
//         {
//             if (_wavePoints.IsCreated)
//                 _wavePoints.Dispose();
//             if (_wavePoint2Entries.IsCreated)
//             {
//                 _wavePoint2Entries.Dispose();
//             }
//
//             if (_wavePoint2BuildingSpawnIntervalCount.IsCreated)
//             {
//                 _wavePoint2BuildingSpawnIntervalCount.Dispose();
//             }
//         }
//
//         public struct BuildingPackLateUpdatePosTag : IComponentData
//         {
//             public LocalTransform TargetTrans;
//         }
//
//         [BurstCompile]
//         private partial struct LateUpdateBuildingPackPosJob : IJobEntity
//         {
//             public EntityCommandBuffer ECB;
//
//             private void Execute(ref LocalTransform transform, in BuildingPackLateUpdatePosTag data, Entity selfEntity)
//             {
//                 transform = data.TargetTrans;
//                 ECB.RemoveComponent<BuildingPackLateUpdatePosTag>(selfEntity);
//             }
//         }
//
//         private Entity InstantiateChildrenWithNewParent(ref SystemState state, Entity oriParentEntity, LocalTransform newLoc)
//         {
//             if (!SystemAPI.HasBuffer<LinkedEntityGroup>(oriParentEntity))
//                 return Entity.Null;
//
//             var linkedEntities = SystemAPI.GetBuffer<LinkedEntityGroup>(oriParentEntity);
//             if (linkedEntities.Length <= 1)
//                 return Entity.Null;
//
//
//             var crystalPackToChildrenPrefab = new NativeHashMap<Entity, NativeList<Entity>>(1, Allocator.Temp);
//             var crystalPackToChildrenNew = new NativeHashMap<Entity, NativeList<Entity>>(1, Allocator.Temp);
//             for (var i = 1; i < linkedEntities.Length; i++)
//             {
//                 var entity = linkedEntities[i].Value;
//                 if (!SystemAPI.HasComponent<CrystalPackNeedInitTag>(entity))
//                     continue;
//                 var list = new NativeList<Entity>(Allocator.Temp);
//                 crystalPackToChildrenPrefab.Add(entity, list);
//                 var groups = SystemAPI.GetBuffer<LinkedEntityGroup>(entity);
//                 for (var idx = 1; idx < groups.Length; idx++)
//                 {
//                     // We only instantiate the entity that is the root prefab of models, which is set to world space and should not have localToWorld
//                     var linkedEntityGroup = groups[idx];
//                     if(!SystemAPI.HasComponent<GeneralAttr>(linkedEntityGroup.Value))continue;
//                     list.Add(linkedEntityGroup.Value);
//                 }
//             }
//
//             // Create new parent
//             var buildingPack = state.EntityManager.CreateEntity();
//             state.EntityManager.AddComponent<GameplayEntityTag>(buildingPack);
//             state.EntityManager.AddComponent<LocalTransform>(buildingPack);
//             state.EntityManager.AddComponent<LocalToWorld>(buildingPack);
//             var buffer = state.EntityManager.AddBuffer<LinkedEntityGroup>(buildingPack);
//             buffer.Add(buildingPack);
//
//             // Get original parent world transform
//             var bLtw = state.EntityManager.GetComponentData<LocalToWorld>(oriParentEntity);
//             var bLtwInverse = math.inverse(bLtw.Value);
//
//             foreach (var pair in crystalPackToChildrenPrefab)
//             {
//                 var list = new NativeList<Entity>(Allocator.Temp);
//                 crystalPackToChildrenNew.Add(pair.Key,list );
//                 foreach (var originalChild in pair.Value)
//                 {
//                     
//                     var newChild = state.EntityManager.Instantiate(originalChild);
//                     list.Add(newChild);
//                     state.EntityManager.AddComponent<GameplayEntityTag>(newChild);
//                     var trans = state.EntityManager.GetComponentData<LocalTransform>(newChild);
//
//                     float4x4 childLtw = float4x4.TRS(trans.Position, trans.Rotation, trans.Scale);
//                     
//                     // var childLtw = state.EntityManager.GetComponentData<LocalToWorld>(originalChild);
//                     var relativeToB = math.mul(bLtwInverse, childLtw);
//                     
//                     // extract position
//                     float3 position = relativeToB.c3.xyz;
//                     
//                     // extract scale
//                     float3 scale;
//                     scale.x = math.length(relativeToB.c0.xyz);
//                     scale.y = math.length(relativeToB.c1.xyz);
//                     scale.z = math.length(relativeToB.c2.xyz);
//                     
//                     // normalize basis vectors to remove scale from rotation
//                     float3x3 rotationMatrix = new float3x3(
//                         relativeToB.c0.xyz / scale.x,
//                         relativeToB.c1.xyz / scale.y,
//                         relativeToB.c2.xyz / scale.z
//                     );
//                     quaternion rotation = new quaternion(rotationMatrix);
//                     
//                     // Calculate new transform
//                     var childLoc = new LocalTransform
//                     {
//                         Position = position,
//                         Rotation = rotation,
//                         Scale = math.cmax(scale)
//                     };
//                     var worldPos = newLoc.Position + math.rotate(newLoc.Rotation, childLoc.Position);
//                     var worldRot = math.mul(newLoc.Rotation, childLoc.Rotation);
//                     
//                     state.EntityManager.SetComponentData(newChild, new LocalTransform
//                     {
//                         Position = worldPos,
//                         Rotation = worldRot,
//                         Scale = childLoc.Scale // 保留原有缩放
//                     });
//                     // state.EntityManager.SetComponentData(newChild, childLoc);
//                     // state.EntityManager.AddComponent<Parent>(newChild);
//                     if(state.EntityManager.HasComponent<Parent>(newChild))
//                         state.EntityManager.RemoveComponent<Parent>(newChild);
//                     // state.EntityManager.SetComponentData(newChild, new Parent { Value = buildingPack });
//                     var newLinkedEntities = state.EntityManager.GetBuffer<LinkedEntityGroup>(buildingPack);
//                     newLinkedEntities.Add(new LinkedEntityGroup { Value = newChild });
//                 }
//             }
//
//             foreach (var pair in crystalPackToChildrenNew)
//             {
//                 var entity = state.EntityManager.CreateEntity();
//                 state.EntityManager.AddComponent<GameplayEntityTag>(entity);
//                 state.EntityManager.AddComponent<CrystalPackNeedInitTag>(entity);
//                 state.EntityManager.AddBuffer<LinkedEntityGroup>(entity);
//                 var linked = SystemAPI.GetBuffer<LinkedEntityGroup>(entity);
//                 linked.Add(new LinkedEntityGroup
//                 {
//                     Value = entity
//                 });
//                 foreach (var child in pair.Value)
//                 {
//                     linked.Add(new LinkedEntityGroup
//                     {
//                         Value = child
//                     });
//                 }
//             }
//
//             return buildingPack;
//         }
//
//         [BurstCompile]
//         public void OnDestroy(ref SystemState state)
//         {
//             Deinitialize();
//         }
//     }
// }