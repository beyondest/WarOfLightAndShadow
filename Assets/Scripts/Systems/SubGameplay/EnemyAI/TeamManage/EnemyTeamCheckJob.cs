using SparFlame.Components.SubGameplay;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace SparFlame.Systems.SubGameplay.EnemyAI
{

    
    [BurstCompile] // Check enemy base
    public partial struct EnemyBaseCheckJob : IJobEntity
    {
        public EntityCommandBuffer.ParallelWriter ECB;
        [NativeDisableParallelForRestriction] public ComponentLookup<TeamData> TeamDataLookup;
        [ReadOnly] public ComponentLookup<GarrisonAttr> GarrisonAttrLookup;
        [ReadOnly] public BufferLookup<GarrisonEntity> GarrisonEntityLookup;
        [ReadOnly] public NativeHashMap<int, int> TeamType2MaxSpecialUnitCount;
        private void Execute([ChunkIndexInQuery] int index,
            ref DynamicBuffer<EnemyBaseTeamAvailableData> teamAvailableDatas,
            ref DynamicBuffer<EnemyBaseTeamGeneralData> teamGeneralDatas,
            ref DynamicBuffer<EnemyBaseGarrisonTowerData> garrisonTowerDatas)
        {
            // Check wait team complete
            for (var i = teamAvailableDatas.Length - 1; i >= 0; i--)
            {
                var data = teamAvailableDatas[i];
                var isThisTeamFull = true;
                foreach (var entry in data.AvailableMemberCountEntries)
                {
                    if (entry.availableCount != 0)
                    {
                        isThisTeamFull = false;
                        break;
                    }
                }
                if (isThisTeamFull)
                {
                    teamAvailableDatas.RemoveAt(i);
                    ref var teamData = ref TeamDataLookup.GetRefRW(data.TeamEntity).ValueRW;
                    teamData.ShortHanded = false;
                    teamData.SpecialUnitCount = TeamType2MaxSpecialUnitCount[(int)teamData.TeamType];
                    // teamData.Idle = true;
                    ECB.RemoveComponent<TeamWaitTag>(index, data.TeamEntity);
                }
            }
            
            // Check base tower alive and update available unit count
            for(var i = garrisonTowerDatas.Length - 1; i >= 0; i--)
            {
                var data = garrisonTowerDatas[i];
                if (!GarrisonAttrLookup.TryGetComponent(data.Tower, out var garrisonAttr))
                {
                    garrisonTowerDatas.RemoveAt(i);
                    continue;
                }
                var garrisonCount = GarrisonEntityLookup[data.Tower].Length;
                var availableCount = math.clamp(garrisonAttr.MaxGarrisonCount - garrisonCount, 0, int.MaxValue);
                data.AvailableCount = availableCount;
                garrisonTowerDatas[i] = data;
            }
            
        }
    }


    [BurstCompile]
    [WithNone(typeof(TeamWaitTag))] // Only check the outside base team
    public partial struct EnemyTeamCheckShortHandJob : IJobEntity
    {
        [ReadOnly] public NativeHashMap<int, TeamSpecialData> TeamType2SpecialData;
        [ReadOnly] public EnemyTeamManageSystemConfig Config;
        [ReadOnly] public ComponentLookup<UnitAttr> UnitAttributeLookup;
        [ReadOnly] public ComponentLookup<SubGameplayGeneralAttr> GeneralAttributeLookup;
        public EntityCommandBuffer.ParallelWriter ECB;

        private void Execute([ChunkIndexInQuery] int index, ref DynamicBuffer<TeamEntityData> teamEntities,
            ref TeamData teamData, Entity selfEntity)
        {
            // safety check
            if (!GeneralAttributeLookup.HasComponent(teamData.BelongsToBase))
                return;
            var specialData = TeamType2SpecialData[(int)teamData.TeamType];

            if (teamEntities.Length <= (int)Config.TotalCountShortHandRatio * specialData.unitMaxCount
                || teamData.SpecialUnitCount < specialData.specialUnitMinCount)
            {
                // Shorthand, need to go base and add to available buffer
                // Calculate available member count
                var availableMemberCountEntries = specialData.maxMemberCountEntries;
                foreach (var entityData in teamEntities)
                {
                    var unitAttr = UnitAttributeLookup[entityData.Unit];
                    for (var i = 0; i < availableMemberCountEntries.Length; i++)
                    {
                        var entry = availableMemberCountEntries[i];
                        if (entry.unitType == unitAttr.Type
                            && (entry.subTypeIndex == -1
                                || entry.subTypeIndex == unitAttr.SubTypeIndex))
                        {
                            entry.availableCount--;
                            availableMemberCountEntries[i] = entry;
                        }
                    }
                }

                ECB.AppendToBuffer(index, teamData.BelongsToBase, new EnemyBaseTeamAvailableData
                {
                    TeamEntity = selfEntity,
                    TeamType = teamData.TeamType,
                    AvailableMemberCountEntries = availableMemberCountEntries
                });
                ECB.AddComponent<TeamWaitTag>(index, selfEntity);
                teamData.ShortHanded = true;
                // teamData.Idle = false;
            }
        }
    }
}