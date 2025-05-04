using SparFlame.GamePlaySystem.General;
using SparFlame.GamePlaySystem.Interact;
using SparFlame.GamePlaySystem.Map.GamePlaySystem.Core.Map;
using SparFlame.GamePlaySystem.RandomSpawn.GamePlaySystem.Functionality.RandomSpawn;
using SparFlame.GamePlaySystem.Resource;
using SparFlame.Utils;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace SparFlame.GamePlaySystem.RandomSpawn
{
    [UpdateInGroup(typeof(InitializationSystemGroup))]
    [UpdateAfter(typeof(OccupiedTagManageSystem))]
    public partial struct EnvSpawnSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
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
            if (gameStatusData.Value != GameStatus.Init) return;

            var ecb = SystemAPI.GetSingleton<EndInitializationEntityCommandBufferSystem.Singleton>()
                .CreateCommandBuffer(state.WorldUnmanaged).AsParallelWriter();

            // Only for seed use
            var curTime = (float)SystemAPI.Time.ElapsedTime;
            var mapInfo = SystemAPI.GetSingleton<MapInitInfo>();
            var envSpawnDataBuffer = SystemAPI.GetSingletonBuffer<EnvSpawnTypeTotalAmount>();
            var envSpawnPrefabBuffer = SystemAPI.GetSingletonBuffer<EnvSpawnPrefabData>();

            var envPrefabDatabase = new NativeParallelMultiHashMap<int, ProbabilityPrefabEntry>(1, Allocator.TempJob);
            var envSpawnDatabase = new NativeHashMap<int, int>(1, Allocator.TempJob);
            foreach (var configData in envSpawnDataBuffer)
            {
                envSpawnDatabase[(int)configData.type] = configData.amount;
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

            var envTypeSpecialDatas = SystemAPI.GetSingletonBuffer<EnvTileTypeSpecialData>();
            var envTypeToTotalWeight = new NativeArray<float>(envTypeSpecialDatas.Length, Allocator.TempJob);

            var job = new ComputeSpawnWeightsJob
            {
                EnvType2TotalWeight = envTypeToTotalWeight,
                SpecialDatas = envTypeSpecialDatas
            }.Schedule(state.Dependency);

            job.Complete();
            var shouldSpawnTypes = new NativeHashSet<int>(1, Allocator.TempJob);
            foreach (var key in envPrefabDatabase.GetKeyArray(Allocator.Temp))
            {
                shouldSpawnTypes.Add(key);
            }

            var seedBias = SystemAPI.GetSingletonRW<GeneralRandom>().ValueRW.Rnd.NextInt();
            var job2 = new EnvSpawnJob
            {
                PrefabDatabase = envPrefabDatabase,
                ShouldSpawnTypes = shouldSpawnTypes,
                SpawnTypeToSpawnAmountMap = envSpawnDatabase,
                ElapsedTime = curTime,
                TileSize = mapInfo.tileSize,
                SeedBias = seedBias,
                ECB = ecb,
                EnvSpecialDatas = envTypeSpecialDatas,
                EnvTypeToTotalWeight = envTypeToTotalWeight
            }.ScheduleParallel(state.Dependency);
            job2.Complete();

            envPrefabDatabase.Dispose();
            envSpawnDatabase.Dispose();
            envTypeToTotalWeight.Dispose();
            shouldSpawnTypes.Dispose();
        }


        [BurstCompile]
        private partial struct ComputeSpawnWeightsJob : IJobEntity
        {
            // Tile Type to tile type amounts
            [NativeDisableParallelForRestriction] public NativeArray<float> EnvType2TotalWeight;
            [ReadOnly] public DynamicBuffer<EnvTileTypeSpecialData> SpecialDatas;
            private void Execute( in LocalTransform transform, in OccupiedTag tag,
                in TileData data)
            {
                if (tag.Faction != FactionTag.Neutral) return;
                foreach (var envTypeData in SpecialDatas)
                {
                    foreach (var tileTypeToSpawnWeight in envTypeData.spawnableTiles)
                    {
                        if (tileTypeToSpawnWeight.tileType == data.TypeA)
                        {
                            EnvType2TotalWeight[(int)envTypeData.type] += tileTypeToSpawnWeight.weight * data.WeightA;
                        }
                        if (tileTypeToSpawnWeight.tileType == data.TypeB)
                        {
                            EnvType2TotalWeight[(int)envTypeData.type] += tileTypeToSpawnWeight.weight * (1 - data.WeightA);
                        }
                    }
                }
            }
        }

        [BurstCompile]
        partial struct EnvSpawnJob : IJobEntity
        {
            [ReadOnly] public NativeHashSet<int> ShouldSpawnTypes;
            [ReadOnly] public NativeHashMap<int, int> SpawnTypeToSpawnAmountMap;
            [ReadOnly] public NativeParallelMultiHashMap<int, ProbabilityPrefabEntry> PrefabDatabase;
            [ReadOnly] public NativeArray<float> EnvTypeToTotalWeight;
            [ReadOnly] public DynamicBuffer<EnvTileTypeSpecialData> EnvSpecialDatas; 
            

            [ReadOnly] public float3 TileSize;
            [ReadOnly] public float ElapsedTime;
            [ReadOnly] public int SeedBias;
            public EntityCommandBuffer.ParallelWriter ECB;

            private void Execute([EntityIndexInQuery] int index, in OccupiedTag tag, in TileData data,
                in LocalTransform transform,
                Entity selfEntity)
            {
                if (tag.Faction != FactionTag.Neutral) return;

                var seed = math.hash(new int2(selfEntity.Index, (int)(ElapsedTime * 1000) + SeedBias));
                var random = new Random(seed);
                foreach (var envType in ShouldSpawnTypes)
                {
                    var envTypeSpecialData = EnvSpecialDatas[envType];
                    var typeAWeight = 0f;
                    var typeBWeight = 0f;
                    foreach (var weight in envTypeSpecialData.spawnableTiles)
                    {
                        if(weight.tileType == data.TypeA)typeAWeight = weight.weight * data.WeightA; 
                        if(weight.tileType == data.TypeB)typeBWeight = weight.weight * (1 - data.WeightA);
                    }
                    
                    var spawnAmount = SpawnTypeToSpawnAmountMap[envType];
                    var thisTileAmount = (int)(spawnAmount * (typeAWeight + typeBWeight / EnvTypeToTotalWeight[envType]));
                    var totalSpawned = 0;
                    

                    while (totalSpawned < thisTileAmount)
                    {
                        var chosen = GeneralUtils.RandomChoosePrefab(ref random, PrefabDatabase, envType);
                        var amount = random.NextInt((int)chosen.AmountRange.lower, (int)chosen.AmountRange.upper);

                        var offset = new float3(
                            random.NextFloat(-TileSize.x * 0.5f, TileSize.x * 0.5f),
                            0f,
                            random.NextFloat(-TileSize.z * 0.5f, TileSize.z * 0.5f)
                        );
                        var spawnPos = transform.Position + offset;

                        var resource = ECB.Instantiate(index, chosen.Prefab);
                        ECB.SetComponent(index, resource, new LocalTransform
                        {
                            Position = spawnPos,
                            Rotation = quaternion.identity,
                            Scale = 1f
                        });
                        ECB.SetComponent(index, resource, new StatData
                        {
                            MaxValue = amount,
                            CurValue = amount
                        });

                        totalSpawned += amount;
                    }
                }
            }
        }
    }
}