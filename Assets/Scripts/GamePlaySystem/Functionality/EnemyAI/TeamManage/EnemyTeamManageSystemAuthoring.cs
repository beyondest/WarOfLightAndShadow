using System;
using System.Collections.Generic;
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
        [HideLabel, Tooltip("UnitType"),VerticalGroup("Type_SubType"), TableColumnWidth(120,false)]
        public UnitType unitType;
        /// <summary>
        /// -1 means no filter
        /// </summary>
        [HideLabel, Tooltip("SubTypeIndex"),VerticalGroup("Type_SubType")]
        public int subTypeIndex;
        [HideLabel,Tooltip("Max count in inspector"),VerticalGroup("MaxCount"), TableColumnWidth(80,false)]
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
        public int teamsMaxCount;
        public int unitMaxCount;
        public UnitType specialUnitType;
        public int specialUnitSubIndex;
        public int specialUnitMinCount;
        public FixedList128Bytes<MemberCountEntry> maxMemberCountEntries;
    }
    
    [Serializable]
    public struct TeamSpecialDataInspector
    {
        [HideLabel, TableColumnWidth(150,false),VerticalGroup("Team")]
        public AITeamType teamType;
        
        [Tooltip("To avoid units split in many teams, and population exceed, so that all enemy units remain idle,\n " +
                 " you have to manage the balance between team max count, team composition, dwelling counts in enemy buildings pack"),
       VerticalGroup("Team"),HideLabel, LabelText("TeamMaxCount")]
        public int teamsMaxCount;
        [Sirenix.OdinInspector.ReadOnly,Tooltip("Auto calculated by code")
         , VerticalGroup("Team"), HideLabel,LabelText("UnitMaxCount")]
        public int unitMaxCount;
        
        [HideLabel,TableColumnWidth(150,false),Tooltip("SpecialUnitType"),VerticalGroup("SpecialUnit")]
        public UnitType specialUnitType;
        [Tooltip("Special Unit Sub type index filter : -1 means no filter on subType")
         ,HideLabel,LabelText("SUSubIdx"),
        VerticalGroup("SpecialUnit")]
        public int specialUnitSubIndex;
        [Tooltip("SpecialUnitMinCount"), HideLabel, LabelText("SUMin"),
        VerticalGroup("SpecialUnit")]
        public int specialUnitMinCount;
        [HideLabel, VerticalGroup("Team composition"),TableList,TableColumnWidth(200, false)]
        public List<MemberCountEntry> maxMemberCountEntries;
    }

    
    public struct TeamEntityData : IBufferElementData
    {
        public Entity Unit;
    }
    
    
    
}