using System;
using System.Collections.Generic;
using System.Linq;
using Sirenix.OdinInspector;
using SparFlame.GamePlaySystem.RandomSpawn;
using UnityEngine;

namespace SparFlame.Database
{
    [CreateAssetMenu(fileName = "EnvDatabase", menuName = "GameData/EnvDatabase", order = 0)]
    public class EnvDatabaseSo : ScriptableObject
    {
        [TableList] public List<EnvDataItem> items;

        [Button("Add New Item")]
        public void AddNewItem()
        {
            items.Add(new EnvDataItem());
        }
        [Button("Check Env Config Valid")]
        private void CheckProbValid()
        {
            var types = new HashSet<EnvType>();
            foreach (var data in items)
            {
                types.Add(data.type);
            }

            foreach (EnvType type in types)
            {
                var sameTypes = items.Where(dataItem => dataItem.type == type).ToList();
                var totalProb = sameTypes.Sum(item => item.prob);
                if (!Mathf.Approximately(totalProb, 1f))
                {
                    var totalProb0 = 0f;
                    foreach (var item in sameTypes)
                    {
                        Debug.Log($"{item.prefab.name} {item.prob} total : {totalProb0}");
                    }
                    throw new ArgumentException($"Resource {type} total probabilities is not 1f");

                }
            }
        }
    }

    [Serializable]
    public class EnvDataItem
    {
        [AssetsOnly,PreviewField] public GameObject prefab;
        public EnvType type;

        public float prob;
        public int amount;
    }
}