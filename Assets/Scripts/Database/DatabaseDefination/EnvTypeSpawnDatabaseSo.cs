using System;
using System.Collections.Generic;
using System.Linq;
using Sirenix.OdinInspector;
using SparFlame.GamePlaySystem.RandomSpawn;
using UnityEngine;

namespace SparFlame.Database
{
    [CreateAssetMenu(fileName = "EnvTypeSpawnDatabase", menuName = "GameData/EnvTypeSpawnDatabase", order = 0)]
    public class EnvTypeSpawnDatabaseSo : ScriptableObject
    {
        [TableList]
        public List<EnvDatabaseItem> items;

        [Button("Check config valid")]
        public void CheckConfigValid()
        {
            var envSet = new HashSet<EnvType>();
            
            foreach (var item in items)
            {
                if(!envSet.Add(item.type))
                    throw new ArgumentException($"Env type spawn database config wrong, duplicated env type {item.type}");
                var set = new HashSet<TileType>();
                foreach (var weight in item.spawnableTiles)
                {
                    if(!set.Add(weight.tileType))
                        throw new ArgumentException($"Env type spawn database config wrong, duplicated tile type{weight.tileType}");
                }
                
            }

            var totalSet = new HashSet<EnvType>();
            foreach (EnvType type in Enum.GetValues(typeof(EnvType)))
            {
                totalSet.Add(type);
            }

            if (envSet.Count != totalSet.Count)
            {
                var e = totalSet.Except(envSet);
                foreach (var t in e)
                {
                    Debug.LogError($"Env type spawn database MISS {t}");
                }
            }
        }
    }

    [Serializable]
    public class EnvDatabaseItem
    {
        [VerticalGroup("TotalAmount"),TableColumnWidth(100,false),HideLabel]
        public EnvType type;
        [VerticalGroup("TotalAmount"),TableColumnWidth(100,false),HideLabel]
        public int amount;
        [TableList]
        public List<TileTypeToSpawnWeight> spawnableTiles;
    }
}