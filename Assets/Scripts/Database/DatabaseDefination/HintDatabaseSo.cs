using System;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using SparFlame.Components.General;
using UnityEngine;

namespace SparFlame.Database
{
    [CreateAssetMenu(fileName = "HintDatabase", menuName = "GameData/HintDatabase", order = 0)]
    public class HintDatabaseSo : ScriptableObject
    {
        [TableList] public List<HintDataItem> items;
    }


    [Serializable]
    public class HintDataItem
    {
       public HintName name;
       public HintType hintType; 
        [TextArea(3,10)]
        public string content;
        
    }
}