using SparFlame.GamePlaySystem.General;
using SparFlame.GamePlaySystem.Waves;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;

namespace SparFlame.GamePlaySystem.EnemyAI
{
    public partial struct EnemyUnitAssignSystem : ISystem
    {
        private NativeHashMap<int, NativeList<AITeamType>> _wavePoint2Strategy;

        private NativeHashMap<int, NativeHashMap<int, TeamSpecialData>>
            _wavePoint2TeamType2MemberCountEntriesLimit;

        private NativeList<int> _wavePoints;


        private BufferLookup<EnemyBaseTeamAvailableData> _enemyBaseTeamAvailableData;
        private BufferLookup<EnemyBaseTeamGeneralData> _enemyBaseTeamGeneralData;
        private BufferLookup<TeamEntityData> _teamEntityData;
        private ComponentLookup<TeamData> _teamData;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<EnemyUnitAssignSystemConfig>();
            state.RequireForUpdate<GameWaveData>();
            state.RequireForUpdate<GamingTag>();
            _enemyBaseTeamAvailableData = state.GetBufferLookup<EnemyBaseTeamAvailableData>();
            _enemyBaseTeamGeneralData = state.GetBufferLookup<EnemyBaseTeamGeneralData>();
            _teamEntityData = state.GetBufferLookup<TeamEntityData>();
            _teamData = state.GetComponentLookup<TeamData>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            if (!_wavePoints.IsCreated)
                Initialize();
            var curWavePoint = GeneralUtils.GetPoint(SystemAPI.GetSingleton<GameWaveData>().CurWaveIndex, _wavePoints);

            _enemyBaseTeamAvailableData.Update(ref state);
            _enemyBaseTeamGeneralData.Update(ref state);
            _teamEntityData.Update(ref state);
            _teamData.Update(ref state);

            var ecb = new EntityCommandBuffer(Allocator.TempJob);
            // This job cannot parallel !!!
            new EnemyUnitAssignJob
            {
                ECB = ecb,
                BaseAvailableTeamDataLookup = _enemyBaseTeamAvailableData,
                BaseTeamDataLookup = _enemyBaseTeamGeneralData,
                TeamEntityLookup = _teamEntityData,
                TeamDataLookup = _teamData,
                TeamType2SpecialData = _wavePoint2TeamType2MemberCountEntriesLimit[curWavePoint],
                Strategy = _wavePoint2Strategy[curWavePoint],
            }.Schedule();

            ecb.Playback(state.EntityManager);
            ecb.Dispose();
        }


        private void Initialize()
        {
            var buffer = SystemAPI.GetSingletonBuffer<WaveUnitAssignStrategyData>();
            var buffer3 = SystemAPI.GetSingletonBuffer<WaveTeamSpecialData>();
            _wavePoint2Strategy = new NativeHashMap<int, NativeList<AITeamType>>(5, Allocator.Persistent);
            _wavePoint2TeamType2MemberCountEntriesLimit =
                new NativeHashMap<int, NativeHashMap<int, TeamSpecialData>>(5, Allocator.Persistent);
            _wavePoints = new NativeList<int>(5, Allocator.Persistent);
            // Init Wave to strategy data
            foreach (var data in buffer)
            {
                if (!_wavePoint2Strategy.ContainsKey(data.WavePoint))
                    _wavePoint2Strategy.Add(data.WavePoint, new NativeList<AITeamType>(4, Allocator.Persistent));
                var strategy = _wavePoint2Strategy[data.WavePoint];
                strategy.Add(data.TeamType);
                _wavePoints.Add(data.WavePoint);
            }

            foreach (var data in buffer)
            {
                var strategy = _wavePoint2Strategy[data.WavePoint];
                strategy.ElementAt(data.Order) = data.TeamType;
            }

            // Init wave to team member consist
            foreach (var data in buffer3)
            {
                if (!_wavePoint2TeamType2MemberCountEntriesLimit.ContainsKey(data.WavePoint))
                    _wavePoint2TeamType2MemberCountEntriesLimit.Add(data.WavePoint,
                        new NativeHashMap<int, TeamSpecialData>(5, Allocator.Persistent));
                var teamSpecialData = _wavePoint2TeamType2MemberCountEntriesLimit[data.WavePoint];
                teamSpecialData.Add((int)data.TeamSpecialData.teamType, data.TeamSpecialData);
            }
        }


        [BurstCompile]
        public void OnDestroy(ref SystemState state)
        {
            if (_wavePoint2Strategy.IsCreated)
            {
                foreach (var pair in _wavePoint2Strategy)
                {
                    pair.Value.Dispose();
                }

                _wavePoint2Strategy.Dispose();
            }

            if (_wavePoint2TeamType2MemberCountEntriesLimit.IsCreated)
            {
                foreach (var pair in _wavePoint2TeamType2MemberCountEntriesLimit)
                {
                    pair.Value.Dispose();
                }

                _wavePoint2TeamType2MemberCountEntriesLimit.Dispose();
            }

            if (_wavePoints.IsCreated)
                _wavePoints.Dispose();
        }
    }
}