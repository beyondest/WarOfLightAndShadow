using System;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using Sirenix.Serialization;
using UnityEngine;
using SparFlame.GamePlaySystem.Resource;
using UnityEngine.Serialization;

namespace SparFlame.Database.DatabaseDefinition
{
    [CreateAssetMenu(fileName = "ResourceSpawnDatabase", menuName = "GameData/ResourceSpawnDatabase", order = 0)]
    public class ResourceSpawnDatabaseSo : ScriptableObject
    {
       [OnValueChanged(nameof(CheckConfigValid))]
        public List<ResourceSpawnDataItem> items ;

        public void CheckConfigValid()
        {
            var added = new HashSet<float>();
            foreach (var pair in items)
            {
                if (!added.Add(pair.timePoint))
                {
                    Debug.LogError($"Same time point already added {pair.timePoint}");
                }
            }
        }
    }

    [Serializable]
    public class ResourceSpawnDataItem
    {
        public int timePoint;
        public List<ResourceTypeToAmountPair> pairs;
    }

    [Serializable]
    public class ResourceTypeToAmountPair
    {
        public ResourceType resourceType;
        public int amount;
    }
    
}