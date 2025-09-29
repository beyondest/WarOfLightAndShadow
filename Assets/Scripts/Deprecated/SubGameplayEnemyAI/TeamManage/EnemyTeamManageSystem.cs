// using SparFlame.Components.General;
// using SparFlame.Components.SubGameplay;
// using SparFlame.Core.Utils;
// using Unity.Burst;
// using Unity.Collections;
// using Unity.Entities;
//
// namespace SparFlame.Systems.SubGameplay.EnemyAI
// {
//     [UpdateAfter(typeof(EnemyUnitAssignSystem))]
//     public partial struct EnemyTeamManageSystem : ISystem
//     {
//         private NativeHashMap<int, NativeHashMap<int, TeamSpecialData>>
//             _wavePoint2TeamType2MemberCountEntriesLimit;
//
//         private NativeHashMap<int, NativeHashMap<int, int>> _wavePoint2TeamType2MaxSpecialUnitCount;
//         private NativeList<int> _wavePoints;
//         
//         
//         private ComponentLookup<TeamData> _teamDataLookup;
//         private ComponentLookup<SubGameplayGeneralAttr> _generalAttributeLookup;
//         private ComponentLookup<UnitAttr> _unitAttributeLookup;
//         private ComponentLookup<GarrisonAttr> _garrisonAttributeLookup;
//         private BufferLookup<GarrisonEntity> _garrisonEntityLookup;
//
//         [BurstCompile]
//         public void OnCreate(ref SystemState state)
//         {
//             state.RequireForUpdate<DarkEnemyDatabaseTag>();
//             state.RequireForUpdate<PlayerFactionData>();
//             state.RequireForUpdate<LightEnemyDatabaseTag>();
//             state.RequireForUpdate<GameWaveData>();
//             state.RequireForUpdate<EndSimulationEntityCommandBufferSystem.Singleton>();
//             state.RequireForUpdate<EnemyTeamManageSystemConfig>();
//             state.RequireForUpdate<GameStatusData>();
//             _generalAttributeLookup = state.GetComponentLookup<SubGameplayGeneralAttr>(true);
//             _teamDataLookup = state.GetComponentLookup<TeamData>();
//             _unitAttributeLookup = state.GetComponentLookup<UnitAttr>(true);
//             _garrisonAttributeLookup = state.GetComponentLookup<GarrisonAttr>(true);
//             _garrisonEntityLookup = state.GetBufferLookup<GarrisonEntity>(true);
//         }
//
//         [BurstCompile]
//         public void OnUpdate(ref SystemState state)
//         {
//             var gameStatusData = SystemAPI.GetSingleton<GameStatusData>().Value;
//             if (gameStatusData == GameStatus.Init)
//             {
//                 if(_wavePoints.IsCreated)
//                     Deinitialize();
//                 Initialize(ref state);
//                 return;
//             }
//             if(gameStatusData != GameStatus.SubGaming)return;
//             
//             var config = SystemAPI.GetSingleton<EnemyTeamManageSystemConfig>();
//
//             var ecbSingleton = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>();
//
//             var curWavePoint = PointDataUtils.GetPoint(SystemAPI.GetSingleton<GameWaveData>().CurWaveIndex, _wavePoints);
//
//             DealtRemoveUnitFromTeamRequest(ref state, curWavePoint);
//
//             _generalAttributeLookup.Update(ref state);
//             _teamDataLookup.Update(ref state);
//             _unitAttributeLookup.Update(ref state);
//             _garrisonAttributeLookup.Update(ref state);
//             _garrisonEntityLookup.Update(ref state);
//
//             var ecbP = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged).AsParallelWriter();
//
//             // Check outside teams to turn to wait teams. Use ecb append to base buffer, so it can parallel
//             state.Dependency = new EnemyTeamCheckShortHandJob
//             {
//                 Config = config,
//                 GeneralAttributeLookup = _generalAttributeLookup,
//                 UnitAttributeLookup = _unitAttributeLookup,
//                 TeamType2SpecialData = _wavePoint2TeamType2MemberCountEntriesLimit[curWavePoint],
//                 ECB = ecbP
//             }.ScheduleParallel(state.Dependency);
//
//             // Check base to change wait team to outside team
//             state.Dependency = new EnemyBaseCheckJob
//             {
//                 ECB = ecbP,
//                 TeamDataLookup = _teamDataLookup,
//                 GarrisonAttrLookup = _garrisonAttributeLookup,
//                 GarrisonEntityLookup = _garrisonEntityLookup,
//                 TeamType2MaxSpecialUnitCount = _wavePoint2TeamType2MaxSpecialUnitCount[curWavePoint],
//             }.ScheduleParallel(state.Dependency);
//         }
//
//         private void DealtRemoveUnitFromTeamRequest(ref SystemState state, int curWavePoint)
//         {
//             var ecb0 = new EntityCommandBuffer(Allocator.Temp);
//             foreach (var (ro, entity) in SystemAPI.Query<RefRO<RemoveFromTeamRequest>>().WithEntityAccess())
//             {
//                 var request = ro.ValueRO;
//                 // Safety check
//                 if (!SystemAPI.HasBuffer<TeamEntityData>(request.BelongsToTeam)) continue;
//                 var buffer = SystemAPI.GetBuffer<TeamEntityData>(request.BelongsToTeam);
//                 for (int i = buffer.Length - 1; i >= 0; i--)
//                 {
//                     var data = buffer[i];
//                     if (data.Unit == request.UnitToRemove)
//                         buffer.RemoveAt(i);
//                 }
//
//                 ref var teamData = ref SystemAPI.GetComponentRW<TeamData>(request.BelongsToTeam).ValueRW;
//
//                 // This team is empty.
//                 if (buffer.Length == 0)
//                 {
//                     var teamGeneralDatas =
//                         SystemAPI.GetBuffer<AIBaseTeamGeneralData>(teamData.BelongsToBase);
//                     var generalData = teamGeneralDatas[(int)teamData.TeamType];
//                     generalData.CurCount--;
//                     teamGeneralDatas[(int)teamData.TeamType] = generalData;
//
//                     // Remove from wait available buffer
//                     if (SystemAPI.HasComponent<TeamWaitTag>(request.BelongsToTeam))
//                     {
//                         var teamAvailableDatas =
//                             SystemAPI.GetBuffer<AIBaseTeamAvailableData>(teamData.BelongsToBase);
//                         for (var i = teamAvailableDatas.Length - 1; i >= 0; i--)
//                         {
//                             var teamAvailableData = teamAvailableDatas[i];
//                             if (teamAvailableData.TeamEntity == request.BelongsToTeam)
//                             {
//                                 teamAvailableDatas.RemoveAt(i);
//                             }
//                         }
//                     }
//
//                     ecb0.DestroyEntity(request.BelongsToTeam);
//                 }
//
//                 var teamSpecialData = _wavePoint2TeamType2MemberCountEntriesLimit[curWavePoint][(int)teamData.TeamType];
//                 // This dead unit is special unit in its team, then reduce the special count
//                 if (teamSpecialData.specialUnitType == request.UnitAttr.Type
//                     && (teamSpecialData.specialUnitSubIndex == -1 ||
//                         teamSpecialData.specialUnitSubIndex == request.UnitAttr.SubTypeIndex))
//                 {
//                     teamData.SpecialUnitCount--;
//                 }
//
//                 ecb0.DestroyEntity(entity);
//             }
//
//             ecb0.Playback(state.EntityManager);
//             ecb0.Dispose();
//         }
//
//
//         private void Initialize(ref SystemState state)
//         {
//             var lightEnemyDatabaseTag = SystemAPI.GetSingletonEntity<LightEnemyDatabaseTag>();
//             var darkEnemyDatabaseTag = SystemAPI.GetSingletonEntity<DarkEnemyDatabaseTag>();
//             var entity = ~SystemAPI.GetSingleton<PlayerFactionData>().faction == FactionTag.Light
//                 ? lightEnemyDatabaseTag
//                 : darkEnemyDatabaseTag;
//             var buffer = SystemAPI.GetBuffer<WaveTeamSpecialData>(entity);
//             _wavePoint2TeamType2MemberCountEntriesLimit =
//                 new NativeHashMap<int, NativeHashMap<int, TeamSpecialData>>(5, Allocator.Persistent);
//             _wavePoints = new NativeList<int>(5, Allocator.Persistent);
//             _wavePoint2TeamType2MaxSpecialUnitCount = new NativeHashMap<int, NativeHashMap<int, int>>(5, Allocator.Persistent);
//             // Init wave to team member consist
//             foreach (var data in buffer)
//             {
//                 if (!_wavePoint2TeamType2MemberCountEntriesLimit.ContainsKey(data.WavePoint))
//                     _wavePoint2TeamType2MemberCountEntriesLimit.Add(data.WavePoint,
//                         new NativeHashMap<int, TeamSpecialData>(4, Allocator.Persistent));
//                 if(!_wavePoint2TeamType2MaxSpecialUnitCount.ContainsKey(data.WavePoint))
//                     _wavePoint2TeamType2MaxSpecialUnitCount.Add(data.WavePoint, new NativeHashMap<int, int>(4, Allocator.Persistent));
//                 var teamSpecialData = _wavePoint2TeamType2MemberCountEntriesLimit[data.WavePoint];
//                 teamSpecialData.Add((int)data.TeamSpecialData.teamType, data.TeamSpecialData);
//                 
//                 // Calculate the max special unit count for each team type
//                 var count = 0;
//                 foreach (var entry in data.TeamSpecialData.maxMemberCountEntries)
//                 {
//                     if (data.TeamSpecialData.specialUnitType == entry.unitType
//                         && (data.TeamSpecialData.specialUnitSubIndex == -1 ||
//                             data.TeamSpecialData.specialUnitSubIndex == entry.subTypeIndex))
//                     {
//                         count += entry.availableCount;
//                     }
//                 }
//                 var maxCount = _wavePoint2TeamType2MaxSpecialUnitCount[data.WavePoint];
//                 maxCount.Add((int)data.TeamSpecialData.teamType, count);
//                 _wavePoints.Add(data.WavePoint);
//             }
//         }
//
//         
//         private void Deinitialize()
//         {
//             if (_wavePoint2TeamType2MemberCountEntriesLimit.IsCreated)
//             {
//                 foreach (var pair in _wavePoint2TeamType2MemberCountEntriesLimit)
//                 {
//                     pair.Value.Dispose();
//                 }
//                 _wavePoint2TeamType2MemberCountEntriesLimit.Dispose();
//             }
//
//             if (_wavePoint2TeamType2MaxSpecialUnitCount.IsCreated)
//             {
//                 foreach (var pair in _wavePoint2TeamType2MaxSpecialUnitCount)
//                 {
//                     pair.Value.Dispose();
//                 }
//                 _wavePoint2TeamType2MaxSpecialUnitCount.Dispose();
//             }
//
//             if (_wavePoints.IsCreated)
//                 _wavePoints.Dispose();
//         }
//         [BurstCompile]
//         public void OnDestroy(ref SystemState state)
//         {
//            Deinitialize();
//         }
//     }
// }