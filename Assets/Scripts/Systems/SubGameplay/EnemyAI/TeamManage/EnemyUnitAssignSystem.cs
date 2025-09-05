using SparFlame.Components.General;
using SparFlame.Components.SubGameplay;
using SparFlame.Core.Utils;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;

namespace SparFlame.Systems.SubGameplay.EnemyAI
{
    public partial struct EnemyUnitAssignSystem : ISystem
    {
        private NativeHashMap<int, NativeList<AITeamType>> _wavePoint2Strategy;

        private NativeHashMap<int, NativeHashMap<int, TeamSpecialData>>
            _wavePoint2TeamType2MemberCountEntriesLimit;

        private NativeList<int> _wavePoints;

        //
        // private BufferLookup<EnemyBaseTeamAvailableData> _enemyBaseTeamAvailableData;
        // private BufferLookup<EnemyBaseTeamGeneralData> _enemyBaseTeamGeneralData;
        private EntityQuery _needAssignTeamUnits;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<PlayerFactionData>();
            state.RequireForUpdate<DarkEnemyDatabaseTag>();
            state.RequireForUpdate<LightEnemyDatabaseTag>();
            state.RequireForUpdate<EnemyUnitAssignSystemConfig>();
            state.RequireForUpdate<GameWaveData>();
            state.RequireForUpdate<GameStatusData>();
            // _enemyBaseTeamAvailableData = state.GetBufferLookup<EnemyBaseTeamAvailableData>();
            // _enemyBaseTeamGeneralData = state.GetBufferLookup<EnemyBaseTeamGeneralData>();
            _needAssignTeamUnits = SystemAPI.QueryBuilder().WithAll<AITag>().WithNone<InTeamTag>().
                WithAll<UnitAttr>().WithAllRW<AIUnitBelongsTo>()
                .Build();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var gameStatus = SystemAPI.GetSingleton<GameStatusData>().Value;
            if (gameStatus == GameStatus.Init)
            {
                if(_wavePoints.IsCreated)
                    Deinitialize();
                Initialize(ref state);
                return;
            }
            if(gameStatus != GameStatus.SubGaming)return;
            if(_needAssignTeamUnits.IsEmpty)    return;
            var curWavePoint = PointDataUtils.GetPoint(SystemAPI.GetSingleton<GameWaveData>().CurWaveIndex, _wavePoints);


            var unitAttrs = _needAssignTeamUnits.ToComponentDataArray<UnitAttr>(Allocator.Temp);
            var unitEntities = _needAssignTeamUnits.ToEntityArray(Allocator.Temp);
            var bases = _needAssignTeamUnits.ToComponentDataArray<AIUnitBelongsTo>(Allocator.Temp);
            var ecb = new EntityCommandBuffer(Allocator.Temp);
 
            for (int i = 0; i < unitEntities.Length; i++)
            {
                var entity = unitEntities[i];
                var unitAttr = unitAttrs[i];
                var belongsTo = bases[i].Base;
                AssignUnitToTeamAccordingToStrategy(ref state, unitAttr,entity,belongsTo,
                    _wavePoint2Strategy[curWavePoint],_wavePoint2TeamType2MemberCountEntriesLimit[curWavePoint],
                    ecb);
            }

            ecb.Playback(state.EntityManager);
            ecb.Dispose();
            unitAttrs.Dispose();
            unitEntities.Dispose();
            bases.Dispose();
        }

        private bool AssignUnitToTeamAccordingToStrategy(ref SystemState state,
            UnitAttr attr, Entity selfEntity, Entity belongsToBase, NativeList<AITeamType> strategy,
            NativeHashMap<int, TeamSpecialData> teamType2SpecialData, EntityCommandBuffer ecb
        )
        {
            if(!SystemAPI.HasBuffer<AIBaseTeamAvailableData>(belongsToBase))return false;
            var baseAvailableTeamDatas = SystemAPI.GetBuffer<AIBaseTeamAvailableData>(belongsToBase);
            var baseTeamData = SystemAPI.GetBuffer<AIBaseTeamGeneralData>(belongsToBase);

            var assignSuccess = false;
            foreach (var teamType in strategy)
            {
                var teamSpecialData = teamType2SpecialData[(int)teamType];
                var isThisUnitSpecialForThisTeamType = attr.Type == teamSpecialData.specialUnitType
                                                       && (teamSpecialData.specialUnitSubIndex == -1 ||
                                                           attr.SubTypeIndex == teamSpecialData.specialUnitSubIndex);

                // Check if This team is valid for current unit type 
                var isThisUnitValidForThisTeamType = false;
                foreach (var entry in teamSpecialData.maxMemberCountEntries)
                {
                    if (entry.unitType == attr.Type &&
                        (entry.subTypeIndex == -1 || entry.subTypeIndex == attr.SubTypeIndex))
                    {
                        isThisUnitValidForThisTeamType = true;
                        break;
                    }
                }

                if (!isThisUnitValidForThisTeamType)
                {
                    continue;
                }

                // Find an available team to fill the unit into it
                for (var j = 0; j < baseAvailableTeamDatas.Length; j++)
                {
                    var data = baseAvailableTeamDatas[j];
                    if (teamType != data.TeamType) continue;
                    // Try to fill the unit into current team slot
                    for (var i = 0; i < data.AvailableMemberCountEntries.Length; i++)
                    {
                        var entry = data.AvailableMemberCountEntries[i];

                        // Filter to find correct slot
                        if (entry.unitType != attr.Type
                            || (entry.subTypeIndex != -1 && entry.subTypeIndex != attr.SubTypeIndex)
                            || entry.availableCount == 0) continue;

                        // Successfully find a slot
                        assignSuccess = true;
                        ecb.AddComponent(selfEntity, new InTeamTag
                        {
                            BelongsToTeam = data.TeamEntity
                        });

                        // Change base available team data
                        entry.availableCount--;
                        data.AvailableMemberCountEntries[i] = entry;
                        baseAvailableTeamDatas[j] = data;

                        // Update team entity data
                        ecb.AppendToBuffer(data.TeamEntity, new TeamEntityData
                        {
                            Unit = selfEntity
                        });
                        break;
                    }

                    if (assignSuccess) break;
                }

                if (assignSuccess) break;

                // No current team available for this unit and team type
                var curTeamDataInBase = baseTeamData[(int)teamType];
                if (curTeamDataInBase.CurCount < teamSpecialData.teamsMaxCount)
                {
                    // Current team count not full in this base, then add a new team

                    // Create new team
                    
                    var newTeam = state.EntityManager.CreateEntity();
                    ecb.AddComponent<SubGameplayEntityTag>(newTeam);
                    ecb.AddComponent(newTeam, new TeamData
                    {
                        TeamType = teamType,
                        SpecialUnitCount = isThisUnitSpecialForThisTeamType ? 1 : 0,
                        BelongsToBase = belongsToBase,
                        // Idle = false,
                        ShortHanded = true
                    });
                    ecb.AddBuffer<TeamEntityData>(newTeam);
                    ecb.AppendToBuffer(newTeam, new TeamEntityData
                    {
                        Unit = selfEntity
                    });
                    ecb.AddComponent<TeamWaitTag>(newTeam);
                    ecb.AddComponent<TeamStateData>(newTeam);
                    ecb.AddComponent<TeamNeedTargetTag>(newTeam);
                    ecb.SetComponentEnabled<TeamNeedTargetTag>(newTeam, false);
                    // Add new available team data to current belongs to base, and change base team data
                    var entries = teamSpecialData.maxMemberCountEntries;
                    for (var i = 0; i < entries.Length; i++)
                    {
                        var entry = entries[i];
                        // This condition should always happen, because we check the team valid in the head
                        if (entry.unitType == attr.Type &&
                            (entry.subTypeIndex == -1 || entry.subTypeIndex == attr.SubTypeIndex))
                        {
                            entry.availableCount--;
                            entries[i] = entry;
                        }
                    }

                    baseAvailableTeamDatas.Add(new AIBaseTeamAvailableData
                    {
                        TeamEntity = newTeam,
                        TeamType = teamType,
                        AvailableMemberCountEntries = entries
                    });
                    curTeamDataInBase.CurCount++;
                    baseTeamData[(int)teamType] = curTeamDataInBase;
                    // Add In team tag to this unit
                    ecb.AddComponent(selfEntity, new InTeamTag
                    {
                        BelongsToTeam = newTeam
                    });
                    assignSuccess = true;
                    
                    // Create hint info to tell player enemy is assemble a new team
                    var hintRequest = ecb.CreateEntity();
                    ecb.AddComponent<SubGameplayEntityTag>(hintRequest);
                    ecb.AddComponent(hintRequest, new HintRequest
                    {
                        Name = teamType switch
                        {
                            AITeamType.Attack => HintName.EnemyIsAssemblingAttackTeam,
                            AITeamType.Defense => HintName.EnemyIsAssemblingDefenseTeam,
                            AITeamType.Gather => HintName.EnemyIsAssemblingGatheringTeam,
                            AITeamType.Harass => HintName.EnemyIsAssemblingStrikeTeam,
                            _ => HintName.None
                        }
                    });
                    
                    break;
                }
            }

            return assignSuccess;
        }

        private void Initialize(ref SystemState state)
        {
            var lightEnemyDatabaseTag = SystemAPI.GetSingletonEntity<LightEnemyDatabaseTag>();
            var darkEnemyDatabaseTag = SystemAPI.GetSingletonEntity<DarkEnemyDatabaseTag>();
            var entity = ~SystemAPI.GetSingleton<PlayerFactionData>().faction == FactionTag.Light
                ? lightEnemyDatabaseTag
                : darkEnemyDatabaseTag;
            var buffer = SystemAPI.GetBuffer<WaveUnitAssignStrategyData>(entity);
            var buffer2 = SystemAPI.GetBuffer<WaveTeamSpecialData>(entity);
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
            foreach (var data in buffer2)
            {
                if (!_wavePoint2TeamType2MemberCountEntriesLimit.ContainsKey(data.WavePoint))
                    _wavePoint2TeamType2MemberCountEntriesLimit.Add(data.WavePoint,
                        new NativeHashMap<int, TeamSpecialData>(5, Allocator.Persistent));
                var teamSpecialData = _wavePoint2TeamType2MemberCountEntriesLimit[data.WavePoint];
                teamSpecialData.Add((int)data.TeamSpecialData.teamType, data.TeamSpecialData);
            }
        }

        private void Deinitialize()
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

        [BurstCompile]
        public void OnDestroy(ref SystemState state)
        {
            Deinitialize();
        }
    }
}