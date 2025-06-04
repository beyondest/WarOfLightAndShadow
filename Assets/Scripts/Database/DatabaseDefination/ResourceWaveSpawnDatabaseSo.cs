using System;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;
using SparFlame.GamePlaySystem.Resource;

namespace SparFlame.Database
{
    [CreateAssetMenu(fileName = "ResourceWaveSpawnDatabase", menuName = "GameData/ResourceWaveSpawnDatabase", order = 0)]
    public class ResourceWaveSpawnDatabaseSo : ScriptableObject
    {
       [TableList]
        public List<ResourceWaveSpawnDataItem> items ;

        [Button("Check config valid")]
        public void CheckConfigValid()
        {
            var added = new HashSet<float>();
            foreach (var pair in items)
            {
                if (!added.Add(pair.timePoint))
                {
                    Debug.LogError($"Same time point already added {pair.timePoint}");
                }
                var added2 = new HashSet<ResourceType>();
                foreach (var pair2 in pair.pairs)
                {
                    if (!added2.Add(pair2.resourceType))
                    {
                        Debug.LogError($"Same resource type already added {pair.timePoint}");
                    }
                }
            }
        }
    }

    [Serializable]
    public class ResourceWaveSpawnDataItem
    {
        public int timePoint;
        [TableList]
        public List<ResourceTypeToAmountPair> pairs;
    }

    [Serializable]
    public class ResourceTypeToAmountPair
    {
        public ResourceType resourceType;
        public int amount;
    }
    
}