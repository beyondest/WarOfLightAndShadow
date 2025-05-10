using System;
using System.Collections.Generic;
using System.Linq;
using Sirenix.OdinInspector;
using SparFlame.GamePlaySystem.EnemyAI;
using SparFlame.GamePlaySystem.Units;
using SparFlame.Utils;
using UnityEngine;

namespace SparFlame.Database
{
    [CreateAssetMenu(fileName = "EnemyAIDatabase", menuName = "GameData/EnemyAIDatabase", order = 0)]
    public class EnemyAIDatabaseSo : ScriptableObject
    {
        [TableList]
        public List<EnemyAIWaveDataItem> items;

      
        [Button("Check Config Valid And Calculate Max Unit Count")]
        private void CheckConfigValid()
        {
            
            var added = new HashSet<float>();
            foreach (var data in items)
            {
                if (!added.Add(data.wavePoint))
                {
                    Debug.LogError($"Same time point already added {data.wavePoint}");
                }

                foreach (var unitTypeSpawnData in data.unitTypeSpawnDatas)
                {
    
                    if (!Mathf.Approximately(unitTypeSpawnData.entries.Sum(entry => entry.probability), 1f))
                    {
                        throw new ArgumentException($" {unitTypeSpawnData.type} Probability must be 1");
                    }
                }

                if (!Mathf.Approximately(data.buildingPacks.Sum(entry => entry.probability), 1f))
                {
                    throw new ArgumentException($" {data.wavePoint} wave  Building pack Probability must be 1");
                }
                data.CheckConfigValid();
                data.CalMaxUnitCount();
            }
       
        }
    }

    [Serializable]
    public class EnemyAIWaveDataItem
    {
        [VerticalGroup("WavePoint"),HideLabel,TableColumnWidth(80,false)] public int wavePoint;

        [VerticalGroup("Unit Spawn Config"),TableList] public List<EnemyUnitTypeSpawnData> unitTypeSpawnDatas;

        [VerticalGroup("BuildingSpawnConfig"),HideLabel,HorizontalGroup("BuildingSpawnConfig/0"),
         TableColumnWidth(120,false),Tooltip("Building spawn interval in this wave point"),
        LabelText("Interval")]
        public int buildingSpawnInterval;

        [VerticalGroup("BuildingSpawnConfig"),HideLabel, HorizontalGroup("BuildingSpawnConfig/1"),
        LabelText("PackCount")]
        public int buildingPackSpawnCount;

        [VerticalGroup("BuildingPacks"),TableList,
         HideLabel, TableColumnWidth(280,false)]
        public List<ProbabilityEntry> buildingPacks;

        [VerticalGroup("AI Strategy"),HideLabel,TableColumnWidth(100,false)] public List<AITeamType> assignTeamOrder;
        
        [VerticalGroup("AI Team"),TableList,TableColumnWidth(500,false)] public List<TeamSpecialDataInspector> teamSpecialDatas;

        public void CheckConfigValid()
        {
            // Check unit type spawn duplicated
            var added = new HashSet<UnitType>();
            foreach (var data in unitTypeSpawnDatas)
            {
                if (!added.Add(data.type))
                {
                    Debug.LogError($"Same unit type already added in type spawn data{data.type}");
                }
            }

            if (teamSpecialDatas.Count > 10)
            {
                Debug.LogError("Team Member Count Exceeded 10," +
                               " this is determined by fixedList128Bytes limit");
            }
            // Check strategy
            var addedTypes = new HashSet<AITeamType>();
            foreach (var type in assignTeamOrder)
            {
                if(!addedTypes.Add(type))
                    Debug.LogError($"Strategy team Type is already added, wave point : {wavePoint}, {type}");
            }
            if(addedTypes.Count != Enum.GetValues(typeof(AITeamType)).Length)
                Debug.LogError($"You miss or duplicate strategy team type, wave point {wavePoint}");

            // Check team type duplicated or missed
            var teamTypes = new HashSet<AITeamType>();
            foreach (var data in teamSpecialDatas)
            {
                if(!teamTypes.Add(data.teamType))
                    Debug.LogError($"Same team type already added {data.teamType}");
            }

            if (teamTypes.Count != Enum.GetValues(typeof(AITeamType)).Length)
            {
                Debug.LogError("You miss team type in team special data config");
                Debug.Log($"{wavePoint} wave point config ");
            }
            // Check team composition 
            foreach (var data in teamSpecialDatas)
            {
                var specialUnit = data.specialUnitType;
                var specialUnitSubType = data.specialUnitSubIndex;
                var temp = new List<int>();
                var unitAdded = new List<CheckUnit>();
                foreach (var entry in data.maxMemberCountEntries)
                {
                    if(entry.unitType == specialUnit && (specialUnitSubType == -1 || entry.subTypeIndex == specialUnitSubType))
                        temp.Add(0);
                    foreach (var unit in unitAdded)
                    {
                        if (entry.unitType == unit.Type && entry.subTypeIndex == unit.SubType)
                            Debug.LogError(
                                $"Same unit already added to team composition ; Wave point : {wavePoint}, team {data.teamType},unit : {unit} ");
                    }
                    unitAdded.Add(new CheckUnit
                    {
                        Type = entry.unitType,
                        SubType = entry.subTypeIndex,
                    });
                }
                if(temp.Count < 1)
                    Debug.LogError($"Team composition must contain special unit, wave point {wavePoint}, team type {data.teamType}");
            }
        }

        public void CalMaxUnitCount()
        {
            for(var i = 0; i < teamSpecialDatas.Count; i++)
            {
                var teamSpecialData = teamSpecialDatas[i];
                teamSpecialData.unitMaxCount = teamSpecialData.maxMemberCountEntries.Sum(entry => entry.availableCount);
                teamSpecialDatas[i] = teamSpecialData;
            }
        }
        private struct CheckUnit
        {
            public UnitType Type;
            public int SubType;
        }
    }

    [Serializable]
    public class EnemyUnitTypeSpawnData
    {
        [VerticalGroup("General"),HorizontalGroup("General/0"),HideLabel, TableColumnWidth(100,false),Tooltip("General Unit Type")]
        public UnitType type;
        [VerticalGroup("General"),HorizontalGroup("General/1"), TableColumnWidth(100,false), Tooltip("General Unit Type spawn interval")]
        public int interval;
        [VerticalGroup("Entries"),TableList,HideLabel]
        public List<ProbabilityEntry> entries;
    }
    
    [Serializable]
    public class ProbabilityEntry
    {
        [AssetsOnly,PreviewField,TableColumnWidth(100,false),VerticalGroup("Prefab")]
        public GameObject prefab;
        [HideLabel, TableColumnWidth(60,false),Tooltip("This prefab spawn probability in general unit type"),
        VerticalGroup("Prob")]
        public float probability;
        [VerticalGroup("AmountRange"),HideLabel, TableColumnWidth(120,false),Tooltip("This prefab spawn count range each time when random picks it")]
        public CustomDs.Range amountRange;
    }



}