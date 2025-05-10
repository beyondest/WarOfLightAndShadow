using System;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

namespace SparFlame.GamePlaySystem.EnemyAI
{
    public class EnemyInitDistinguishSystemAuthoring : MonoBehaviour
    {
        public EnemyInitDistinguishConfig config;
        private class EnemyInitSystemAuthoringBaker : Baker<EnemyInitDistinguishSystemAuthoring>
        {
            public override void Bake(EnemyInitDistinguishSystemAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.None);
                authoring.config.aiTeamTypeCount = Enum.GetValues(typeof(AITeamType)).Length;
                AddComponent(entity, authoring.config);
            }
        }
    }

    [Serializable]
    public struct EnemyInitDistinguishConfig : IComponentData
    {
         [Sirenix.OdinInspector.ReadOnly]
         public int aiTeamTypeCount;
    }

    
    // Enemy Base Specific Data


    public struct EnemyBaseBelongsTo : IComponentData
    {
        public Entity BuildingPack;
    }
    public struct EnemyBaseTeamGeneralData : IBufferElementData
    {
        public AITeamType TeamType;
        public int CurCount;
    }
    public struct EnemyBaseTeamAvailableData : IBufferElementData
    {
        public AITeamType TeamType; // gather / attack / defense / harass
        public Entity TeamEntity; 
        public FixedList128Bytes<MemberCountEntry> AvailableMemberCountEntries;
    }
    public struct EnemyBaseGarrisonTowerData : IBufferElementData
    {
        public Entity Tower;
        public int AvailableCount;
    }
    
   
  
}