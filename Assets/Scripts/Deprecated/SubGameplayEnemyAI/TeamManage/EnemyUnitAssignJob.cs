/*using SparFlame.GamePlaySystem.General;
using SparFlame.GamePlaySystem.Units;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

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


    }
}*/