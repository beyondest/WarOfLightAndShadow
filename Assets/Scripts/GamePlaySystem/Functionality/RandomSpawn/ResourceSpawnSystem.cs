using SparFlame.GamePlaySystem.General;
using SparFlame.GamePlaySystem.Interact;
using SparFlame.GamePlaySystem.Map.GamePlaySystem.Core.Map;
using SparFlame.GamePlaySystem.RandomSpawn.GamePlaySystem.Functionality.RandomSpawn;
using SparFlame.GamePlaySystem.Resource;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace SparFlame.GamePlaySystem.RandomSpawn
{
    [UpdateInGroup(typeof(InitializationSystemGroup))]
    [UpdateAfter(typeof(EnvSpawnSystem))]
    public partial struct ResourceSpawnSystem : ISystem
    {
        // TimePoints to resource type to amount
        private NativeHashMap<int, NativeHashMap<int, int>> _resourceSpawnDatabase;

        // Int resource type 
        private NativeHashSet<int> _renewableResources;

        private NativeHashSet<int> _shouldRespawnResources;

        // int resource type to prefab
        private NativeParallelMultiHashMap<int, ProbabilityPrefabEntry> _resourcePrefabDatabase;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<EndInitializationEntityCommandBufferSystem.Singleton>();
            state.RequireForUpdate<MapInitInfo>();
            state.RequireForUpdate<GeneralRandom>();
            state.RequireForUpdate<GameStartTime>();
            state.RequireForUpdate<ResourceSpawnState>();
            state.RequireForUpdate<GlobalResourceDataTag>();
            state.RequireForUpdate<ResourceSpawnSystemConfig>();
            state.RequireForUpdate<ResourceSpawnData>();
            state.RequireForUpdate<GameStatusData>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            if (!_resourceSpawnDatabase.IsCreated)
                Initialize();
            var gameStatusData = SystemAPI.GetSingleton<GameStatusData>();
            
            var curResourceState = SystemAPI.GetSingletonRW<ResourceSpawnState>();
            if (gameStatusData.Value == GameStatus.Init)
            {
                curResourceState.ValueRW.LastTimePoint = -1;
                return;
            }
            if(gameStatusData.Value != GameStatus.Gaming)return;
            
            var globalResourceDataCenter =
                SystemAPI.GetBuffer<ResourceAvailableData>(SystemAPI.GetSingletonEntity<GlobalResourceDataTag>());
            var ecb = SystemAPI.GetSingleton<EndInitializationEntityCommandBufferSystem.Singleton>()
                .CreateCommandBuffer(state.WorldUnmanaged).AsParallelWriter();
            
            var curTime = (float)SystemAPI.Time.ElapsedTime - SystemAPI.GetSingleton<GameStartTime>().Value;
            var curTimePoints = GeneralUtils.GetPointData<ResourcePointData,int>(curTime, SystemAPI.GetSingletonBuffer<ResourcePointData>());
            var timePointReached = curTimePoints != curResourceState.ValueRO.LastTimePoint;
            var notEnough = GlobalDataShouldRespawn(globalResourceDataCenter, curTimePoints);
            if (timePointReached
                ||notEnough )
            {
                curResourceState.ValueRW.LastTimePoint = curTimePoints;
                SpawnResource(ref state, globalResourceDataCenter, curTimePoints,
                    timePointReached, ecb);
                var curTimePointMap = _resourceSpawnDatabase[curTimePoints];
                for (var i = 0; i < globalResourceDataCenter.Length; i++)
                {
                    var data = globalResourceDataCenter[i];
                    data.Amount = curTimePointMap[(int)data.ResourceType];
                    globalResourceDataCenter[i] = data;
                }
            }
        }

        [BurstCompile]
        public void OnDestroy(ref SystemState state)
        {
            if (_resourceSpawnDatabase.IsCreated)
            {
                foreach (var pair in _resourceSpawnDatabase)
                {
                    pair.Value.Dispose();
                }
                _resourceSpawnDatabase.Dispose();
            }
            if (_renewableResources.IsCreated)
                _renewableResources.Dispose();
            if (_shouldRespawnResources.IsCreated)
                _shouldRespawnResources.Dispose();
            if (_resourcePrefabDatabase.IsCreated)
            {
                _resourcePrefabDatabase.Dispose();
            }
        }
        
        private void SpawnResource(
            ref SystemState state,
            DynamicBuffer<ResourceAvailableData> datas,
            int curTimePoints,
            bool timePointReached,
            EntityCommandBuffer.ParallelWriter ecb
        )
        {
            var elapsedTime = (float)SystemAPI.Time.ElapsedTime;
            var occupiedConfig = SystemAPI.GetSingleton<MapInitInfo>();
            var tileSize = occupiedConfig.tileSize;

            var curResourceMap = _resourceSpawnDatabase[curTimePoints];
            if (timePointReached)
            {
                _shouldRespawnResources.Clear();
                foreach (var pair in curResourceMap)
                {
                    _shouldRespawnResources.Add(pair.Key);
                }
            }

            var specialDatas = SystemAPI.GetSingletonBuffer<ResourceTileTypeSpecialData>();
            var resourceTypeToTotalWeight = new NativeArray<float>(specialDatas.Length, Allocator.TempJob);
            var datasList = new NativeList<ResourceAvailableData>(5, Allocator.TempJob);
            foreach (var data in datas)
            {
                datasList.Add(data);
            }
            var job = new ComputeSpawnWeightsJob
            {
                ResourceTypeToTotalWeight = resourceTypeToTotalWeight,
                SpecialDatas = specialDatas,
            }.Schedule(state.Dependency);
            
            job.Complete();
            var seedBias = SystemAPI.GetSingletonRW<GeneralRandom>().ValueRW.Rnd.NextInt();
            var job2 = new SpawnResourceJob
            {
                PrefabDatabase = _resourcePrefabDatabase,
                ShouldSpawnTypes = _shouldRespawnResources,
                SpawnTypeToSpawnAmountMap = curResourceMap,
                ElapsedTime = elapsedTime,
                TileSize = tileSize,
                Datas = datasList,
                SeedBias = seedBias,
                ECB = ecb,
                ResourceSpecialDatas = specialDatas,
                ResourceTypeToTotalWeight = resourceTypeToTotalWeight,
            }.ScheduleParallel(state.Dependency);
            job2.Complete();
            datasList.Dispose();
            resourceTypeToTotalWeight.Dispose();
        }

        private bool GlobalDataShouldRespawn(in DynamicBuffer<ResourceAvailableData> globalResourceDataCenter,
            int curTimePoints)
        {
            _shouldRespawnResources.Clear();
            var curResourceMap = _resourceSpawnDatabase[curTimePoints];
            foreach (var data in globalResourceDataCenter)
            {
                // If data is below half of time point resource amount, then respawn this type
                if (curResourceMap[(int)data.ResourceType] / 2 > data.Amount)
                {
                    _shouldRespawnResources.Add((int)data.ResourceType);
                }
            }
            return _shouldRespawnResources.Count > 0;
        }
        
        [BurstCompile]
        private partial struct ComputeSpawnWeightsJob : IJobEntity
        {
            [NativeDisableParallelForRestriction]public NativeArray<float> ResourceTypeToTotalWeight;
            [ReadOnly] public DynamicBuffer<ResourceTileTypeSpecialData> SpecialDatas;

            private void Execute( in LocalTransform transform, in OccupiedTag tag,
                in TileData data)
            {
                if (tag.Faction != FactionTag.Neutral) return;
                foreach (var resourceTypeData in SpecialDatas)
                {
                    foreach (var tileTypeToSpawnWeight in resourceTypeData.spawnableTiles)
                    {
                        if (tileTypeToSpawnWeight.tileType == data.TypeA)
                        {
                            ResourceTypeToTotalWeight[(int)resourceTypeData.type] += tileTypeToSpawnWeight.weight * data.WeightA;
                        }
                        if (tileTypeToSpawnWeight.tileType == data.TypeB)
                        {
                            ResourceTypeToTotalWeight[(int)resourceTypeData.type] += tileTypeToSpawnWeight.weight * (1 - data.WeightA);
                        }
                    }
                }
            }
        }

        [BurstCompile]
        partial struct SpawnResourceJob : IJobEntity
        {
            [ReadOnly] public NativeList<ResourceAvailableData> Datas;
            [ReadOnly] public NativeHashSet<int> ShouldSpawnTypes;
            [ReadOnly] public NativeHashMap<int, int> SpawnTypeToSpawnAmountMap;
            [ReadOnly] public NativeParallelMultiHashMap<int, ProbabilityPrefabEntry> PrefabDatabase;
            [ReadOnly] public NativeArray<float> ResourceTypeToTotalWeight;
            [ReadOnly] public DynamicBuffer<ResourceTileTypeSpecialData> ResourceSpecialDatas;

            [ReadOnly] public float3 TileSize;
            [ReadOnly] public float ElapsedTime;
            [ReadOnly] public int SeedBias;
            public EntityCommandBuffer.ParallelWriter ECB;

            private void Execute([EntityIndexInQuery] int index, in OccupiedTag tag, in LocalTransform transform,
                in TileData data,
                Entity selfEntity)
            {
                if (tag.Faction != FactionTag.Neutral) return;

                var seed = GeneralUtils.GetSeedByIndexTimeBias(selfEntity.Index, SeedBias, ElapsedTime);
                var random = new Random(seed);

                // var random = new Random((uint)(ElapsedTime * 1000 + selfEntity.Index * 17 + 1));

                foreach (var resourceType in ShouldSpawnTypes)
                {
                    var typeSpecialData = ResourceSpecialDatas[resourceType];
                    var typeAWeight = 0f;
                    var typeBWeight = 0f;
                    foreach (var spawnData in typeSpecialData.spawnableTiles)
                    {
                        if(spawnData.tileType == data.TypeA)typeAWeight = spawnData.weight * data.WeightA; 
                        if(spawnData.tileType == data.TypeB)typeBWeight = spawnData.weight * (1 - data.WeightA);
                    }
                    
                    var spawnAmount = SpawnTypeToSpawnAmountMap[resourceType] - Datas[resourceType].Amount; // Target amount - cur amount
                    
                    var thisTileAmount = (int)(spawnAmount * (typeAWeight + typeBWeight) / ResourceTypeToTotalWeight[resourceType]);
                    var totalSpawned = 0;

                    while (totalSpawned < thisTileAmount)
                    {
                        var chosen = GeneralUtils.RandomChoosePrefab(ref random, PrefabDatabase, resourceType);
                        var amount = random.NextInt((int)chosen.AmountRange.lower, (int)chosen.AmountRange.upper);

                        var offset = new float3(
                            random.NextFloat(-TileSize.x * 0.5f, TileSize.x * 0.5f),
                            0f,
                            random.NextFloat(-TileSize.z * 0.5f, TileSize.z * 0.5f)
                        );
                        var spawnPos = transform.Position + offset;

                        var resource = ECB.Instantiate(index,chosen.Prefab);
                        ECB.SetComponent(index,resource, new LocalTransform
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



        private void Initialize()
        {
            var buffer = SystemAPI.GetSingletonBuffer<ResourceSpawnData>();
            var buffer2 = SystemAPI.GetSingletonBuffer<RenewableResourceType>();
            var buffer3 = SystemAPI.GetSingletonBuffer<ResourceEntityPrefabData>();
            _resourceSpawnDatabase = new NativeHashMap<int, NativeHashMap<int, int>>(5, Allocator.Persistent);
            foreach (var data in buffer)
            {
                if (!_resourceSpawnDatabase.ContainsKey(data.TimePoints))
                {
                    _resourceSpawnDatabase.Add(data.TimePoints,
                        new NativeHashMap<int, int>(5, Allocator.Persistent));
                }
                var subDict = _resourceSpawnDatabase[data.TimePoints];
                subDict[(int)data.ResourceType] = data.Amount;
            }

            _renewableResources = new NativeHashSet<int>(5, Allocator.Persistent);
            foreach (var type in buffer2)
            {
                _renewableResources.Add((int)type.ResourceType);
            }

            _shouldRespawnResources = new NativeHashSet<int>(5, Allocator.Persistent);
            _resourcePrefabDatabase = new NativeParallelMultiHashMap<int, ProbabilityPrefabEntry>(5, Allocator.Persistent);
            foreach (var data in buffer3)
            {
                 _resourcePrefabDatabase.Add((int)data.Type, new ProbabilityPrefabEntry
                 {
                     Prefab = data.Prefab,
                     Probability = data.Probability,
                     AmountRange = data.AmountRange
                 });
            }
        }
    }
}