using System;
using System.Collections.Generic;
using System.Linq;
using Sirenix.OdinInspector;
using SparFlame.GamePlaySystem.RandomSpawn.GamePlaySystem.Functionality.RandomSpawn;
using UnityEngine;

namespace SparFlame.Database.DatabaseDefinition
{
    [CreateAssetMenu(fileName = "EnvSpawnDatabase", menuName = "GameData/EnvSpawnDatabase", order = 0)]
    public class EnvSpawnDatabaseSo : ScriptableObject
    {
       public List<EnvSpawnDataItem> items;
       
        [Button("Check Probability Config Valid")]
        private void CheckProbValid()
        {
            foreach (EnvType type in Enum.GetValues(typeof(EnvType)))
            {
                var sameTypes = items.Where(dataItem => dataItem.type != type).ToList();
                var totalProb = sameTypes.Sum(item => item.prob);
                if (!Mathf.Approximately(totalProb, 1f))
                    throw new ArgumentException($"Resource {type} total probabilities is not 1f");
            }
        }
    }

    [Serializable]
    public class EnvSpawnDataItem
    {
        public GameObject prefab;
        public EnvType type;
        
        public float prob;
        public int amount;
    }
    
}