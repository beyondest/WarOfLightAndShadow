using System;
using System.Collections.Generic;
using System.Linq;
using Sirenix.OdinInspector;
using SparFlame.GamePlaySystem.Building;
using SparFlame.GamePlaySystem.EnemyAI;
using SparFlame.GamePlaySystem.General;
using SparFlame.GamePlaySystem.Units;
using SparFlame.Utils;
using UnityEngine;
using UnityEngine.Serialization;

namespace SparFlame.Database.DatabaseDefinition
{
    [CreateAssetMenu(fileName = "EnemyAIDatabase", menuName = "GameData/EnemyAIDatabase", order = 0)]
    public class EnemyAIDatabaseSo : ScriptableObject
    {
        [OnValueChanged(nameof(CheckConfigValid))]
        public List<EnemyAIWaveDataItem> items;

        public void CheckConfigValid()
        {
            var added = new HashSet<float>();
            foreach (var data in items)
            {
                if (!added.Add(data.wavePoint))
                {
                    Debug.LogError($"Same time point already added {data.wavePoint}");
                }
            }
        }

        [Button("Check Probability Config Valid")]
        private void CheckProbValid()
        {
            foreach (var waveDataItem in items)
            {
                foreach (var unitTypeSpawnData in waveDataItem.unitTypeSpawnDatas)
                {
                    if (!Mathf.Approximately(unitTypeSpawnData.entries.Sum(entry => entry.probability), 1f))
                    {
                        throw new ArgumentException($" {unitTypeSpawnData.type} Probability must be 1");
                    }
                }

                if (!Mathf.Approximately(waveDataItem.buildingPacks.Sum(entry => entry.probability), 1f))
                {
                    throw new ArgumentException($" {waveDataItem.wavePoint} wave  Building pack Probability must be 1");
                }
            }
        }
    }

    [Serializable]
    public class EnemyAIWaveDataItem
    {
        [VerticalGroup("General")] public int wavePoint;

        [VerticalGroup("Unit Spawn Config")] public List<EnemyUnitTypeSpawnData> unitTypeSpawnDatas;

        [VerticalGroup("Building Spawn Config"), FoldoutGroup("Building Spawn Config/Generic"),
         HorizontalGroup("Building Spawn Config/Generic/0")]
        public int buildingSpawnInterval;

        [VerticalGroup("Building Spawn Config"), FoldoutGroup("Building Spawn Config/Generic"),
         HorizontalGroup("Building Spawn Config/Generic/1")]
        public int buildingPackSpawnCount;

        [VerticalGroup("Building Spawn Config"), FoldoutGroup("Building Spawn Config/BuildingPacks"),
         HideLabel]
        public List<ProbabilityEntry> buildingPacks;

        [VerticalGroup("AI Strategy")] public List<AITeamType> assignTeamOrder;
        
        [OnValueChanged(nameof(CalMaxUnitCount))]
        [VerticalGroup("AI Team")] public List<TeamSpecialData> teamSpecialDatas;

        [Button("Check Config Valid")]
        private void CheckConfigValid()
        {
            var added = new HashSet<UnitType>();
            foreach (var data in unitTypeSpawnDatas)
            {
                if (!added.Add(data.type))
                {
                    Debug.LogError($"Same time point already added {data.type}");
                }
            }

            if (teamSpecialDatas.Count > 10)
            {
                Debug.LogError("Team Member Count Exceeded 10," +
                               " this is determined by fixedList128Bytes limit");
            }
        }

        private void CalMaxUnitCount()
        {
            for(var i = 0; i < teamSpecialDatas.Count; i++)
            {
                var teamSpecialData = teamSpecialDatas[i];
                teamSpecialData.unitMaxCount = teamSpecialData.maxMemberCountEntries.Sum(entry => entry.availableCount);
                teamSpecialDatas[i] = teamSpecialData;
            }
        }
    }

    [Serializable]
    public class EnemyUnitTypeSpawnData
    {
        public UnitType type;
        public int interval;
        public List<ProbabilityEntry> entries;
    }
    
    [Serializable]
    public class ProbabilityEntry
    {
        public GameObject prefab;
        public float probability;
        public CustomDs.Range amountRange;
    }



}