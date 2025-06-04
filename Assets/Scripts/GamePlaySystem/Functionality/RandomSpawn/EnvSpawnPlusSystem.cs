using SparFlame.GamePlaySystem.General;
using SparFlame.GamePlaySystem.Map;
using SparFlame.GamePlaySystem.Resource;
using SparFlame.Utils;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using Random = Unity.Mathematics.Random;

namespace SparFlame.GamePlaySystem.RandomSpawn
{
    public struct TileSpawnEntry
    {
        public Entity TileEntity;
        public float WeightAccumulated;
        public float3 Position;
    }


    [UpdateInGroup(typeof(InitializationSystemGroup))]
    [UpdateAfter(typeof(OccupiedTagManageSystem))]
    public partial struct EnvSpawnPlusSystem : ISystem
    {
        // Env type int to special data
        private NativeHashMap<int, EnvTileTypeSpecialData> _envType2EnvTileTypeSpecialData;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<EnvSpawnSystemConfig>();
            state.RequireForUpdate<GameTimeData>();
            state.RequireForUpdate<EndInitializationEntityCommandBufferSystem.Singleton>();
            state.RequireForUpdate<MapInitInfo>();
            state.RequireForUpdate<GameStatusData>();
            state.RequireForUpdate<GeneralRandom>();
            state.RequireForUpdate<EnvSpawnPrefabData>();
            state.RequireForUpdate<EnvSpawnTypeTotalAmount>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var gameStatusData = SystemAPI.GetSingleton<GameStatusData>();
            if (!_envType2EnvTileTypeSpecialData.IsCreated)
            {
                Initialize();
            }

            if (gameStatusData.Value != GameStatus.Init) return;

            // var config = SystemAPI.GetSingleton<EnvSpawnSystemConfig>();
            var ecb = SystemAPI.GetSingleton<EndInitializationEntityCommandBufferSystem.Singleton>()
                .CreateCommandBuffer(state.WorldUnmanaged).AsParallelWriter();

            // Only for seed use
            var curTime = SystemAPI.GetSingleton<GameTimeData>().ElapsedTime;
            var mapInfo = SystemAPI.GetSingleton<MapInitInfo>();
            ref var rnd = ref SystemAPI.GetSingletonRW<GeneralRandom>().ValueRW.Rnd;
            var envSpawnDataBuffer = SystemAPI.GetSingletonBuffer<EnvSpawnTypeTotalAmount>();
            var envSpawnPrefabBuffer = SystemAPI.GetSingletonBuffer<EnvSpawnPrefabData>();
            var envTypeSpecialDatas = SystemAPI.GetSingletonBuffer<EnvTileTypeSpecialData>();


            var envPrefabDatabase = new NativeParallelMultiHashMap<int, ProbabilityPrefabEntry>(1, Allocator.TempJob);
            var envTypeToSpawnTotalAmount = new NativeHashMap<int, int>(1, Allocator.TempJob);
            var shouldSpawnTypes = new NativeHashSet<int>(1, Allocator.TempJob);

            foreach (var configData in envSpawnDataBuffer)
            {
                var scale = 1f;
                if (SystemAPI.HasSingleton<DebugTag>() && SystemAPI.TryGetSingleton<RandomSpawnDebug>(out var randomSpawnDebug))
                {
                    scale = randomSpawnDebug.envSpawnAmountScale;
                }
                envTypeToSpawnTotalAmount[(int)configData.Type] = (int)(configData.Amount * scale);
            }

            foreach (var envPrefabData in envSpawnPrefabBuffer)
            {
                envPrefabDatabase.Add((int)envPrefabData.Type, new ProbabilityPrefabEntry
                {
                    AmountRange = new CustomDs.Range
                    {
                        lower = envPrefabData.Amount,
                        upper = envPrefabData.Amount
                    },
                    Prefab = envPrefabData.Prefab,
                    Probability = envPrefabData.Prob
                });
            }

            foreach (var key in envPrefabDatabase.GetKeyArray(Allocator.Temp))
            {
                shouldSpawnTypes.Add(key);
            }

            CalculateSpawnTiles(ref state, shouldSpawnTypes, envTypeSpecialDatas,
                out var typeToSpawnableTiles, out var totalTypeWeight);

            SpawnEnv(ref state, envTypeToSpawnTotalAmount, typeToSpawnableTiles, envPrefabDatabase, totalTypeWeight,
                ref rnd, mapInfo.tileSize);


            envPrefabDatabase.Dispose();
            envTypeToSpawnTotalAmount.Dispose();
            shouldSpawnTypes.Dispose();
            foreach (var pair in typeToSpawnableTiles)
            {
                pair.Value.Dispose();
            }

            typeToSpawnableTiles.Dispose();
            totalTypeWeight.Dispose();
        }

        public void OnDestroy(ref SystemState state)
        {
            if (_envType2EnvTileTypeSpecialData.IsCreated)
                _envType2EnvTileTypeSpecialData.Dispose();
        }

        private void Initialize()
        {
            var buffer = SystemAPI.GetSingletonBuffer<EnvTileTypeSpecialData>();
            _envType2EnvTileTypeSpecialData =
                new NativeHashMap<int, EnvTileTypeSpecialData>(buffer.Length, Allocator.Persistent);
            foreach (var data in buffer)
            {
                _envType2EnvTileTypeSpecialData.Add((int)data.type, data);
            }
        }


        private void CalculateSpawnTiles(ref SystemState state, in NativeHashSet<int> shouldSpawnTypes,
            in DynamicBuffer<EnvTileTypeSpecialData> envSpecialDatas,
            out NativeHashMap<int, NativeList<TileSpawnEntry>> typeToSpawnableTiles,
            out NativeHashMap<int, float> totalTypeWeight)
        {
            var typeToSpawnableTiles0 = new NativeHashMap<int, NativeList<TileSpawnEntry>>(10, Allocator.TempJob);
            var totalTypeWeight0 = new NativeHashMap<int, float>(10, Allocator.TempJob);

            foreach (var envType in shouldSpawnTypes)
            {
                totalTypeWeight0.Add(envType, 0f);
                typeToSpawnableTiles0.Add(envType, new NativeList<TileSpawnEntry>(Allocator.TempJob));
            }

            foreach (var (refRoTileData, refROccupiedTag, transform, entity) in SystemAPI
                         .Query<RefRO<TileData>, RefRO<OccupiedTag>, RefRO<LocalTransform>>().WithEntityAccess())
            {
                if (refROccupiedTag.ValueRO.Faction != FactionTag.Neutral) continue;
                var thisTileData = refRoTileData.ValueRO;
                foreach (var envType in shouldSpawnTypes)
                {
                    var envTypeSpecialData = envSpecialDatas[envType];
                    var typeAWeight = 0f;
                    var typeBWeight = 0f;
                    foreach (var spawnableTile in envTypeSpecialData.spawnableTiles)
                    {
                        // This tile as tile A should summon this env type weight is typeAWeight
                        if (spawnableTile.tileType == thisTileData.TypeA)
                            typeAWeight = spawnableTile.weight * thisTileData.WeightA;
                        // This tile as tile B should summon this env type weight is typeBWeight
                        if (spawnableTile.tileType == thisTileData.TypeB)
                            typeBWeight = spawnableTile.weight * (1 - thisTileData.WeightA);
                    }

                    var wei = typeAWeight + typeBWeight;
                    if (wei != 0f)
                    {
                        totalTypeWeight0[envType] += wei;
                        var newEntry = new TileSpawnEntry
                        {
                            TileEntity = entity,
                            WeightAccumulated = totalTypeWeight0[envType],
                            Position = transform.ValueRO.Position
                        };
                        typeToSpawnableTiles0[envType].Add(newEntry);
                    }
                }
            }

            typeToSpawnableTiles = typeToSpawnableTiles0;
            totalTypeWeight = totalTypeWeight0;
        }

        private void SpawnEnv(ref SystemState state, in NativeHashMap<int, int> envTypeToTotalAmount,
            in NativeHashMap<int, NativeList<TileSpawnEntry>> typeToSpawnableTiles,
            NativeParallelMultiHashMap<int, ProbabilityPrefabEntry> prefabDatabase,
            in NativeHashMap<int, float> typeToTotalWeight, ref Random rnd,
            in float3 tileSize)
        {
            var ecb = new EntityCommandBuffer(Allocator.Temp);
            foreach (var envPair in typeToTotalWeight)
            {
                var envType = envPair.Key;
                var totalWeight = envPair.Value;
                var totalAmount = envTypeToTotalAmount[envType];
                var spawnable = typeToSpawnableTiles[envType];
                if (spawnable.Length == 0) continue;
                while (totalAmount > 0f)
                {
                    var pick = rnd.NextFloat(0f, totalWeight);
                    foreach (var entry in spawnable)
                    {
                        if (entry.WeightAccumulated >= pick)
                        {
                            var chosen = GeneralUtils.RandomChoosePrefab(ref rnd, prefabDatabase, envType);
                            var amount = rnd.NextInt((int)chosen.AmountRange.lower, (int)chosen.AmountRange.upper);
                            var offset = new float3(
                                rnd.NextFloat(-tileSize.x * 0.5f, tileSize.x * 0.5f),
                                0f,
                                rnd.NextFloat(-tileSize.z * 0.5f, tileSize.z * 0.5f)
                            );
                            var spawnPos = entry.Position + offset;

                            var entity = ecb.Instantiate(chosen.Prefab);
                            ecb.SetComponent(entity, new LocalTransform
                            {
                                Position = spawnPos,
                                Rotation = quaternion.identity,
                                Scale = 1f
                            });
                            ecb.AddComponent<GameplayEntityTag>(entity);

                            ecb.AppendToBuffer(entry.TileEntity, new EnvEntities
                            {
                                Value = entity
                            });
                            totalAmount -= amount;
                            break;
                        }
                    }
                }
            }

            ecb.Playback(state.EntityManager);
            ecb.Dispose();
        }
    }
}