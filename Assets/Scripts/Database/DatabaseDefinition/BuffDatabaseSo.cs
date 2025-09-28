using System;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using SparFlame.Components.SubGameplay;
using UnityEngine;

namespace SparFlame.Database
{
    [CreateAssetMenu(fileName = "BuffDatabase", menuName = "GameData/BuffDatabase", order = 0)]
    public class BuffDatabaseSo : ScriptableObject
    {
        [TableList]
        public List<BuffDataItem> items;
    }

   
    [Serializable]
    public class BuffDataItem
    {
        public BuffName buffName;
        [AssetsOnly]
        public GameObject prefab;
        public BuffType buffType;
        public BuffFilter filter;
    }
    
}