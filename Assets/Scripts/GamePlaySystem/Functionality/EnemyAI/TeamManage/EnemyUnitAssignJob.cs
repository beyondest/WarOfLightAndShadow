using SparFlame.GamePlaySystem.General;
using SparFlame.GamePlaySystem.Units;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;

namespace SparFlame.GamePlaySystem.EnemyAI
{
    [BurstCompile]
    [WithAll(typeof(AITag))]
    [WithNone(typeof(InTeamTag))]
    public partial struct EnemyUnitAssignJob : IJobEntity
    {
        // This wave point data info
        [ReadOnly] public NativeHashMap<int, TeamSpecialData> TeamType2SpecialData;
        [ReadOnly] public NativeList<AITeamType> Strategy;


        // LookUp. This is not a parallel schedule job , so disable restriction
        [NativeDisableParallelForRestriction] public BufferLookup<EnemyBaseTeamGeneralData> BaseTeamDataLookup;

        [NativeDisableParallelForRestriction]
        public BufferLookup<EnemyBaseTeamAvailableData> BaseAvailableTeamDataLookup;

        [NativeDisableParallelForRestriction] public BufferLookup<TeamEntityData> TeamEntityLookup;
        [NativeDisableParallelForRestriction] public ComponentLookup<TeamData> TeamDataLookup;
        public EntityCommandBuffer ECB;

        private void Execute(in UnitAttr attr, in EnemyUnitBelongsTo belongsTo, Entity selfEntity)
        {
            var assignSuccess = AssignUnitToTeamAccordingToStrategy(attr, selfEntity, belongsTo.Base);
            if (!assignSuccess)
            {
                // If enemy cannot spawn more buildings pack ,this state happens when you have a bad setting of enemy team max count, team composition, and enemy dwelling count
                // If enemy can build more buildings, then expand logic happens here
            }
        }

        private bool AssignUnitToTeamAccordingToStrategy(UnitAttr attr, Entity selfEntity, Entity belongsToBase)
        {
            var baseAvailableTeamDatas = BaseAvailableTeamDataLookup[belongsToBase];
            var baseTeamData = BaseTeamDataLookup[belongsToBase];
            var assignSuccess = false;
            foreach (var teamType in Strategy)
            {
                var teamSpecialData = TeamType2SpecialData[(int)teamType];
                var isThisUnitSpecialForThisTeamType = attr.Type == teamSpecialData.specialUnitType
                                                       && (teamSpecialData.specialUnitSubIndex == -1 ||
                                                           attr.SubTypeIndex == teamSpecialData.specialUnitSubIndex);

                // Check if This team is valid for current unit type 
                var isThisUnitValidForThisTeamType = false;
                foreach (var entry in teamSpecialData.maxMemberCountEntries)
                {
                    if (entry.unitType == attr.Type && entry.subTypeIndex == attr.SubTypeIndex)
                    {
                        isThisUnitValidForThisTeamType = true;
                        break;
                    }
                }

                if (!isThisUnitValidForThisTeamType) continue;

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
                        ECB.AddComponent(selfEntity, new InTeamTag
                        {
                            BelongsToTeam = data.TeamEntity
                        });

                        // Change base available team data
                        entry.availableCount--;
                        data.AvailableMemberCountEntries[i] = entry;
                        baseAvailableTeamDatas[j] = data;

                        // Update team entity data
                        var teamEntityBuffer = TeamEntityLookup[data.TeamEntity];
                        teamEntityBuffer.Add(new TeamEntityData
                        {
                            Unit = selfEntity
                        });
                        if (isThisUnitSpecialForThisTeamType)
                        {
                            ref var teamData = ref TeamDataLookup.GetRefRW(data.TeamEntity).ValueRW;
                            teamData.SpecialUnitCount++;
                        }
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
                    var newTeam = ECB.CreateEntity();
                    ECB.AddComponent(newTeam, new TeamData
                    {
                        TeamType = teamType,
                        SpecialUnitCount = isThisUnitSpecialForThisTeamType ? 1 : 0,
                        BelongsToBase = belongsToBase,
                        // Idle = false,
                        ShortHanded = true
                    });
                    ECB.AddBuffer<TeamEntityData>(newTeam);
                    ECB.AppendToBuffer(newTeam, new TeamEntityData
                    {
                        Unit = selfEntity
                    });
                    ECB.AddComponent<TeamWaitTag>(newTeam);
                    ECB.AddComponent<TeamStateData>(newTeam);
                    ECB.AddComponent<TeamNeedTargetTag>(newTeam);
                    ECB.SetComponentEnabled<TeamNeedTargetTag>(newTeam, false);
                    // Add new available team data to current belongs to base, and change base team data
                    var entries = teamSpecialData.maxMemberCountEntries;
                    for (var i = 0; i < entries.Length; i++)
                    {
                        var entry = entries[i];
                        // This condition should always happen, because we check the team valid in the head
                        if (entry.unitType == attr.Type && entry.subTypeIndex == attr.SubTypeIndex)
                        {
                            entry.availableCount--;
                            entries[i] = entry;
                        }
                    }

                    baseAvailableTeamDatas.Add(new EnemyBaseTeamAvailableData
                    {
                        TeamEntity = newTeam,
                        TeamType = teamType,
                        AvailableMemberCountEntries = entries
                    });
                    curTeamDataInBase.CurCount++;
                    baseTeamData[(int)teamType] = curTeamDataInBase;

                    // Add In team tag to this unit
                    ECB.AddComponent(selfEntity, new InTeamTag
                    {
                        BelongsToTeam = newTeam
                    });
                    assignSuccess = true;
                    break;
                }
            }

            return assignSuccess;
        }
    }
}