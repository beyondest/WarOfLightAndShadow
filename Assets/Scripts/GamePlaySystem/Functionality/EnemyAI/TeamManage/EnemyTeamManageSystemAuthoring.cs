using System;
using Sirenix.OdinInspector;
using SparFlame.GamePlaySystem.Units;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace SparFlame.GamePlaySystem.EnemyAI
{
    public class EnemyTeamManageSystemAuthoring : MonoBehaviour
    {
        public float totalCountShortHandRatio = 0.5f;
        private class EnemyTeamManageSystemAuthoringBaker : Baker<EnemyTeamManageSystemAuthoring>
        {
            public override void Bake(EnemyTeamManageSystemAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.None);
                AddComponent(entity, new EnemyTeamManageSystemConfig
                {
                    TotalCountShortHandRatio = authoring.totalCountShortHandRatio
                });
            }
        }
    }

    public struct EnemyTeamManageSystemConfig : IComponentData
    {
        public float TotalCountShortHandRatio;
    }
    
    public struct WaveTeamSpecialData : IBufferElementData
    {
        public int WavePoint;
        public TeamSpecialData TeamSpecialData;
    }
    
    [Serializable]
    public struct MemberCountEntry
    {
        public UnitType unitType;
        /// <summary>
        /// -1 means no filter
        /// </summary>
        public int subTypeIndex;
        public int availableCount; 
    }
    
    public struct TeamWaitTag : IComponentData
    {
        
    }
    
    public struct TeamData : IComponentData
    {
        public AITeamType TeamType;
        public int SpecialUnitCount;
        public bool ShortHanded;
        public Entity BelongsToBase;
    }



    [Serializable]
    public struct TeamSpecialData
    {
        public AITeamType teamType;
        
        [Tooltip("To avoid units split in many teams, and population exceed, so that all enemy units remain idle,\n " +
                 " you have to manage the balance between team max count, team composition, dwelling counts in enemy buildings pack")]
        public int teamsMaxCount;
        [Sirenix.OdinInspector.ReadOnly,Tooltip("Auto calculated by code")]
        public int unitMaxCount;
        public UnitType specialUnitType;
        [Tooltip("-1 means no filter on subType")]
        public int specialUnitSubIndex;
        public int specialUnitMinCount;
        [HideLabel, LabelText("Team composition")]
        public FixedList128Bytes<MemberCountEntry> maxMemberCountEntries;
    }

    
    
    public struct TeamEntityData : IBufferElementData
    {
        public Entity Unit;
    }
    
    
    
}