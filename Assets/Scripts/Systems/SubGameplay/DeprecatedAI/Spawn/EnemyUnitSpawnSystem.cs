using SparFlame.Components.General;
using SparFlame.Components.SubGameplay;
using SparFlame.Core.Utils;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;

namespace SparFlame.Systems.SubGameplay.EnemyAI
{
    public partial struct EnemyUnitSpawnSystem : ISystem
    {
        private NativeList<int> _wavePoints;
        private NativeHashMap<int, NativeHashMap<int, int>> _wavePoint2Type2Interval;

        private NativeHashMap<int, NativeParallelMultiHashMap<int, PrefabEntryUtils.ProbabilityPrefabEntry>>
            _wavePoint2Type2Entries;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<DarkEnemyDatabaseTag>();
            state.RequireForUpdate<LightEnemyDatabaseTag>();
            state.RequireForUpdate<GameTimeData>();
            state.RequireForUpdate<PlayerFactionData>();
            state.RequireForUpdate<GameWaveData>();
            state.RequireForUpdate<GameStatusData>();
            state.RequireForUpdate<EnemySpawnSystemConfig>();
            state.RequireForUpdate<EndSimulationEntityCommandBufferSystem.Singleton>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var gameStatus = SystemAPI.GetSingleton<GameStatusData>().Value;
            if (gameStatus == GameStatus.Init)
            {
                if (_wavePoints.IsCreated)
                    Deinitialize();
                Initialize(ref state);
                return;
            }

            if (gameStatus != GameStatus.SubGaming) return;
            // var config = SystemAPI.GetSingleton<EnemySpawnSystemConfig>();
            // Only when enemy population not exceeds, will conjure unit


            var ecbSingleton = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>();
            var curTime = SystemAPI.GetSingleton<GameTimeData>().ElapsedTime;
            var curWave = SystemAPI.GetSingleton<GameWaveData>().CurWaveIndex;
            var curPoint = PointDataUtils.GetPoint(curWave, _wavePoints);
            var curPointType2Interval = _wavePoint2Type2Interval[curPoint];
            var curPointType2Entries = _wavePoint2Type2Entries[curPoint];

            if (!(SystemAPI.HasSingleton<DebugTag>() && SystemAPI.TryGetSingleton(out OldEnemyAIDebug debug)))
            {
                debug = new OldEnemyAIDebug
                {
                    enabled = false
                };
            }

            new EnemyUnitSpawnJob
            {
                ECB = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged).AsParallelWriter(),
                CurTime = curTime,
                Type2Interval = curPointType2Interval,
                Type2Entries = curPointType2Entries,
                OldEnemyAIDebug = debug
            }.ScheduleParallel();
        }

        [BurstCompile]
        private partial struct EnemyUnitSpawnJob : IJobEntity
        {
            public EntityCommandBuffer.ParallelWriter ECB;
            [ReadOnly] public OldEnemyAIDebug OldEnemyAIDebug;
            [ReadOnly] public float CurTime;
            [ReadOnly] public NativeHashMap<int, int> Type2Interval;
            [ReadOnly] public NativeParallelMultiHashMap<int, PrefabEntryUtils.ProbabilityPrefabEntry> Type2Entries;

            private void Execute([ChunkIndexInQuery] int index, in SubGameplayGeneralAttr subGameplayGeneralAttr,
                ref AIConjureShrineData data,
                in ConjureAttr attr, Entity selfEntity)
            {
                if (CurTime < data.ConjureTime) return;
                float interval = Type2Interval[(int)attr.ConjuringType];
                if (OldEnemyAIDebug.enabled)
                    interval /= OldEnemyAIDebug.unitSpawnSpeedScale;
                data.ConjureTime = CurTime + interval;
                var entry = PrefabEntryUtils.RandomChoosePrefab(ref data.Rnd, Type2Entries,
                    (int)attr.ConjuringType);
                var conjureCount = data.Rnd.NextInt((int)entry.AmountRange.lower, (int)entry.AmountRange.upper);
                var conjureRequest = ECB.CreateEntity(index);
                ECB.AddComponent<SubGameplayEntityTag>(index, conjureRequest);
                ECB.AddComponent(index, conjureRequest, new ConjureRequest
                {
                    UnitPrefab = entry.Prefab,
                    BuildingEntity = selfEntity,
                    Count = conjureCount
                });
                // var costList = CostListLookup[entry.Prefab];
                //
                // foreach (var cost in costList)
                // {
                //     var costRequest = ECB.CreateEntity(index);
                //     ECB.AddComponent(index, costRequest, new ResourceChangeRequest
                //     {
                //         AbsAmount = math.abs(cost.Amount * conjureCount),
                //         FromFaction = subGameplayGeneralAttr.Faction,
                //         ResourceType = cost.Type,
                //         RequestType = ResourceRequestType.Consume
                //     });
                //     ECB.AddComponent<SubGameplayEntityTag>(index, selfEntity);
                // }
            }
        }


        private void Initialize(ref SystemState state)
        {
            var lightResourceDatabaseEntity = SystemAPI.GetSingletonEntity<LightEnemyDatabaseTag>();
            var darkResourceDatabaseEntity = SystemAPI.GetSingletonEntity<DarkEnemyDatabaseTag>();
            var entity = ~SystemAPI.GetSingleton<PlayerFactionData>().faction == FactionTag.Light
                ? lightResourceDatabaseEntity
                : darkResourceDatabaseEntity;
            var buffer1 = SystemAPI.GetBuffer<EnemyUnitSpawnIntervalData>(entity);
            var buffer2 = SystemAPI.GetBuffer<EnemyUnitSpawnProbPrefabEntry>(entity);
            _wavePoint2Type2Entries =
                new NativeHashMap<int, NativeParallelMultiHashMap<int, PrefabEntryUtils.ProbabilityPrefabEntry>>(10,
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
                        new NativeParallelMultiHashMap<int, PrefabEntryUtils.ProbabilityPrefabEntry>(10,
                            Allocator.Persistent));
                }

                var type2Entry = _wavePoint2Type2Entries[data.WavePoint];
                type2Entry.Add((int)data.UnitType, data.ProbabilityPrefab);
            }
        }

        private void Deinitialize()
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

        [BurstCompile]
        public void OnDestroy(ref SystemState state)
        {
            Deinitialize();
        }
    }
}