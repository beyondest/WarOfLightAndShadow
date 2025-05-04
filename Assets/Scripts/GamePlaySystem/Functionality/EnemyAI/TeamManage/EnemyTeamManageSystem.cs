using SparFlame.GamePlaySystem.Garrison;
using SparFlame.GamePlaySystem.General;
using SparFlame.GamePlaySystem.Interact;
using SparFlame.GamePlaySystem.Units;
using SparFlame.GamePlaySystem.Waves;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;

namespace SparFlame.GamePlaySystem.EnemyAI
{
    [UpdateAfter(typeof(EnemyUnitAssignSystem))]
    public partial struct EnemyTeamManageSystem : ISystem
    {
        private NativeHashMap<int, NativeHashMap<int, TeamSpecialData>>
            _wavePoint2TeamType2MemberCountEntriesLimit;

        private NativeList<int> _wavePoints;
        private ComponentLookup<TeamData> _teamDataLookup;
        private ComponentLookup<GeneralAttr> _generalAttributeLookup;
        private ComponentLookup<UnitAttr> _unitAttributeLookup;
        private ComponentLookup<GarrisonAttr> _garrisonAttributeLookup;
        private BufferLookup<GarrisonEntity>    _garrisonEntityLookup;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<GameWaveData>();
            state.RequireForUpdate<EndSimulationEntityCommandBufferSystem.Singleton>();
            state.RequireForUpdate<EnemyTeamManageSystemConfig>();
            state.RequireForUpdate<GamingTag>();
            _generalAttributeLookup = state.GetComponentLookup<GeneralAttr>(true);
            _teamDataLookup = state.GetComponentLookup<TeamData>();
            _unitAttributeLookup = state.GetComponentLookup<UnitAttr>(true);
            _garrisonAttributeLookup = state.GetComponentLookup<GarrisonAttr>(true);
            _garrisonEntityLookup = state.GetBufferLookup<GarrisonEntity>(true);
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            if(!_wavePoints.IsCreated)
                Initialize();
            var config = SystemAPI.GetSingleton<EnemyTeamManageSystemConfig>();

            var ecbSingleton = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>();
           
            var curWavePoint = GeneralUtils.GetPoint(SystemAPI.GetSingleton<GameWaveData>().CurWaveIndex, _wavePoints);
            
            DealtRemoveUnitFromTeamRequest(ref state, curWavePoint);
            
            _generalAttributeLookup.Update(ref state);
            _teamDataLookup.Update(ref state);
            _unitAttributeLookup.Update(ref state);
            _garrisonAttributeLookup.Update(ref state);
            
            var ecbP = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged).AsParallelWriter();

            // Check outside teams to turn to wait teams. Use ecb append to base buffer, so it can parallel
            state.Dependency = new EnemyTeamCheckShortHandJob
            {
                Config = config,
                GeneralAttributeLookup = _generalAttributeLookup,
                UnitAttributeLookup = _unitAttributeLookup,
                TeamType2SpecialData = _wavePoint2TeamType2MemberCountEntriesLimit[curWavePoint],
                ECB =ecbP
            }.ScheduleParallel(state.Dependency);

            // Check base to change wait team to outside team
            state.Dependency = new EnemyBaseCheckJob
            {
                ECB = ecbP,
                TeamDataLookup = _teamDataLookup,
                GarrisonAttrLookup = _garrisonAttributeLookup,
                GarrisonEntityLookup = _garrisonEntityLookup,
            }.ScheduleParallel(state.Dependency);
        }

        private void DealtRemoveUnitFromTeamRequest(ref SystemState state, int curWavePoint)
        {
            var ecb0 = new EntityCommandBuffer(Allocator.Temp);
            foreach (var (ro, entity) in SystemAPI.Query<RefRO<RemoveFromTeamRequest>>().WithEntityAccess())
            {
                var request = ro.ValueRO;
                // Safety check
                if (!SystemAPI.HasBuffer<TeamEntityData>(request.BelongsToTeam)) continue;
                var buffer = SystemAPI.GetBuffer<TeamEntityData>(request.BelongsToTeam);
                for (int i = buffer.Length - 1; i >= 0; i--)
                {
                    var data = buffer[i];
                    if (data.Unit == request.UnitToRemove)
                        buffer.RemoveAt(i);
                }

                ref var teamData = ref SystemAPI.GetComponentRW<TeamData>(request.BelongsToTeam).ValueRW;

                // This team is empty.
                if (buffer.Length == 0)
                {
                    var teamGeneralDatas =
                        SystemAPI.GetBuffer<EnemyBaseTeamGeneralData>(teamData.BelongsToBase);
                    var generalData = teamGeneralDatas[(int)teamData.TeamType];
                    generalData.CurCount--;
                    teamGeneralDatas[(int)teamData.TeamType] = generalData;

                    // Remove from wait available buffer
                    if (SystemAPI.HasComponent<TeamWaitTag>(request.BelongsToTeam))
                    {
                        var teamAvailableDatas =
                            SystemAPI.GetBuffer<EnemyBaseTeamAvailableData>(teamData.BelongsToBase);
                        for (var i = teamAvailableDatas.Length - 1; i >= 0; i--)
                        {
                            var teamAvailableData = teamAvailableDatas[i];
                            if (teamAvailableData.TeamEntity == request.BelongsToTeam)
                            {
                                teamAvailableDatas.RemoveAt(i);
                            }
                        }
                    }
                    ecb0.DestroyEntity(request.BelongsToTeam);
                }
                
                var teamSpecialData = _wavePoint2TeamType2MemberCountEntriesLimit[curWavePoint][(int)teamData.TeamType];
                // This dead unit is special unit in its team, then reduce the special count
                if (teamSpecialData.specialUnitType == request.UnitAttr.Type
                    && (teamSpecialData.specialUnitSubIndex == -1 ||
                        teamSpecialData.specialUnitSubIndex == request.UnitAttr.SubTypeIndex))
                {
                    teamData.SpecialUnitCount--;
                }
                ecb0.DestroyEntity(entity);
            }
            ecb0.Playback(state.EntityManager);
            ecb0.Dispose();
        }

        
        private void Initialize()
        {
            var buffer3 = SystemAPI.GetSingletonBuffer<WaveTeamSpecialData>();
            _wavePoint2TeamType2MemberCountEntriesLimit =
                new NativeHashMap<int, NativeHashMap<int, TeamSpecialData>>(5, Allocator.Persistent);
            _wavePoints = new NativeList<int>(5, Allocator.Persistent);
   

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
        
        public void OnDestroy(ref SystemState state)
        {
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