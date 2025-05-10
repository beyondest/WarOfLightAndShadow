using System;
using System.Collections.Generic;
using System.Linq;
using Sirenix.OdinInspector;
using SparFlame.GamePlaySystem.Map;
using SparFlame.GamePlaySystem.Resource;
using UnityEngine;

namespace SparFlame.Database
{
    [CreateAssetMenu(fileName = "ResourceTypeSpawnDatabase", menuName = "GameData/ResourceTypeSpawnDatabase", order = 0)]
    public class ResourceTypeSpawnDatabaseSo : ScriptableObject
    {
         [TableList]
        public List<ResourceTypeSpawnDatabaseItem> items;
        [Button("Check config valid")]
        public void CheckConfigValid()
        {
            var resourceTypes = new HashSet<ResourceType>();
            
            foreach (var item in items)
            {
                if(!resourceTypes.Add(item.type))
                    throw new ArgumentException($"Env type spawn database config wrong, duplicated resource type {item.type}");
                var set = new HashSet<TileType>();
                foreach (var weight in item.spawnableTiles)
                {
                    if(!set.Add(weight.tileType))
                        throw new ArgumentException($"Env type spawn database config wrong, duplicated tile type{weight.tileType}");
                }
            }

            var total = new HashSet<ResourceType>();
            foreach (ResourceType type in Enum.GetValues(typeof(ResourceType)))
            {
                total.Add(type);
            }

            if (resourceTypes.Count != total.Count)
            {
                var e = total.Except(resourceTypes);
                foreach (var t in e)
                {
                    Debug.LogError($"Resource type spawn database MISS : {t}");
                }
            }
        }
    }
    
    
}