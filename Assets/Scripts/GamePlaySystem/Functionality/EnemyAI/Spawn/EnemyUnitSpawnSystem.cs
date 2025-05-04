using SparFlame.GamePlaySystem.General;
using SparFlame.GamePlaySystem.Resource;
using SparFlame.GamePlaySystem.Spawn;
using SparFlame.GamePlaySystem.Waves;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace SparFlame.GamePlaySystem.EnemyAI
{
    public partial struct EnemyUnitSpawnSystem : ISystem
    {
        private NativeList<int> _wavePoints;
        private NativeHashMap<int, NativeHashMap<int, int>> _wavePoint2Type2Interval;
        private NativeHashMap<int, NativeParallelMultiHashMap<int, ProbabilityPrefabEntry>> _wavePoint2Type2Entries;
        private BufferLookup<CostList> _costListLookup;
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<AllyResourceDataTag>();
            state.RequireForUpdate<PlayerFactionData>();
            state.RequireForUpdate<EnemyResourceDataTag>();
            state.RequireForUpdate<EndSimulationEntityCommandBufferSystem.Singleton>();
            state.RequireForUpdate<GameWaveData>();
            state.RequireForUpdate<GameStartTime>();
            state.RequireForUpdate<GamingTag>();
            state.RequireForUpdate<EnemySpawnSystemConfig>();
            _costListLookup = state.GetBufferLookup<CostList>(true);
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            if(!_wavePoints.IsCreated)
                Initialize();
            // Only when enemy population not exceeds, will conjure unit
            var curPlayerFaction = SystemAPI.GetSingleton<PlayerFactionData>().Value;
            var enemyResourceDataCenter = curPlayerFaction == FactionTag.Ally ? SystemAPI.GetSingletonEntity<EnemyResourceDataTag>() :
                    SystemAPI.GetSingletonEntity<AllyResourceDataTag>();
            var enemyResourceData = SystemAPI.GetBuffer<ResourceAvailableData>(enemyResourceDataCenter);
            if (enemyResourceData[(int)ResourceType.Population].Amount> 0)
            {
                _costListLookup.Update(ref state);
                var ecbSingleton = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>();
                var curTime = SystemAPI.Time.ElapsedTime - SystemAPI.GetSingleton<GameStartTime>().Value;
                var curWave = SystemAPI.GetSingleton<GameWaveData>().CurWaveIndex;
                var curPoint = GeneralUtils.GetPoint(curWave, _wavePoints);
                var curPointType2Interval = _wavePoint2Type2Interval[curPoint];
                var curPointType2Entries = _wavePoint2Type2Entries[curPoint];
                new EnemyUnitSpawnJob
                {
                    ECB = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged).AsParallelWriter(),
                    CurTime = (float)curTime,
                    Type2Interval = curPointType2Interval,
                    Type2Entries = curPointType2Entries,
                    CostListLookup = _costListLookup
                }.ScheduleParallel();
            }
        }
        
        [BurstCompile]
        private partial struct EnemyUnitSpawnJob : IJobEntity
        {
            public EntityCommandBuffer.ParallelWriter ECB;
            [ReadOnly] public float CurTime;
            [ReadOnly] public NativeHashMap<int, int> Type2Interval;
            [ReadOnly] public NativeParallelMultiHashMap<int, ProbabilityPrefabEntry> Type2Entries;
            [ReadOnly] public BufferLookup<CostList> CostListLookup;
            private void Execute([ChunkIndexInQuery] int index,in GeneralAttr generalAttr, ref EnemyConjureShrineData data,
                in ConjureAttr attr, Entity selfEntity)
            {
                if (CurTime < data.ConjureTime) return;
                var interval = Type2Interval[(int)attr.ConjuringType];
                data.ConjureTime = CurTime + interval;
                var entry = GeneralUtils.RandomChoosePrefab(ref data.Rnd, Type2Entries,
                    (int)attr.ConjuringType);
                var conjureCount = data.Rnd.NextInt((int)entry.AmountRange.lower, (int)entry.AmountRange.upper);
                var conjureRequest = ECB.CreateEntity(index);
                ECB.AddComponent(index, conjureRequest, new ConjureRequest
                {
                    UnitPrefab = entry.Prefab,
                    BuildingEntity = selfEntity,
                    Count = conjureCount
                });
                var costList = CostListLookup[entry.Prefab];
                foreach (var cost in costList)
                {
                    var costRequest = ECB.CreateEntity(index);
                    ECB.AddComponent(index,costRequest, new ResourceChangeRequest
                    {
                        AbsAmount = math.abs(cost.Amount * conjureCount),
                        FromFaction = generalAttr.FactionTag,
                        Type = cost.Type,
                        RequestType = ResourceRequestType.Consume
                    });
                }
            }
        }
        
        
        private void Initialize()
        {
            var buffer1 = SystemAPI.GetSingletonBuffer<EnemyUnitSpawnIntervalData>();
            var buffer2 = SystemAPI.GetSingletonBuffer<EnemyUnitSpawnProbPrefabEntry>();
            _wavePoint2Type2Entries =
                new NativeHashMap<int, NativeParallelMultiHashMap<int, ProbabilityPrefabEntry>>(10,
                    Allocator.Persistent);
            _wavePoint2Type2Interval = new NativeHashMap<int, NativeHashMap<int, int>>(10, Allocator.Persistent);
            _wavePoints = new NativeList<int>(10, Allocator.Persistent);
            foreach (var data in buffer1)
            {
                _wavePoints.Add(data.WavePoint);
                if (!_wavePoint2Type2Interval.ContainsKey(data.WavePoint))
                {
                    _wavePoint2Type2Interval.Add(data.WavePoint, new NativeHashMap<int, int>(5, Allocator.Persistent));
                }

                var type2Interval = _wavePoint2Type2Interval[data.WavePoint];
                type2Interval.Add((int)data.UnitType, data.Interval);
            }

            _wavePoints.Sort();

            foreach (var data in buffer2)
            {
                if (!_wavePoint2Type2Entries.ContainsKey(data.WavePoint))
                {
                    _wavePoint2Type2Entries.Add(data.WavePoint,
                        new NativeParallelMultiHashMap<int, ProbabilityPrefabEntry>(10, Allocator.Persistent));
                }

                var type2Entry = _wavePoint2Type2Entries[data.WavePoint];
                type2Entry.Add((int)data.UnitType, data.ProbabilityPrefab);
            }
        }

        [BurstCompile]
        public void OnDestroy(ref SystemState state)
        {
            if (_wavePoint2Type2Interval.IsCreated)
            {
                foreach (var pair in _wavePoint2Type2Interval)
                {
                    pair.Value.Dispose();
                }
                _wavePoint2Type2Interval.Dispose();
            }

            if (_wavePoint2Type2Entries.IsCreated)
            {
                foreach (var dKvPair in _wavePoint2Type2Entries)
                {
                    dKvPair.Value.Dispose();
                }
                _wavePoint2Type2Entries.Dispose();

            }
            if (_wavePoints.IsCreated)
                _wavePoints.Dispose();
        }
    }
}