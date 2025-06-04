using System;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using SparFlame.GamePlaySystem.Hints;
using UnityEngine;
using UnityEngine.Serialization;

namespace SparFlame.Database.Database.DatabaseDefination
{
    [CreateAssetMenu(fileName = "HintDatabase", menuName = "GameData/HintDatabase", order = 0)]
    public class HintDatabaseSo : ScriptableObject
    {
        [TableList] public List<HintItem> items;
    }


    [Serializable]
    public class HintItem
    {
       public HintName name;
       public HintType hintType; 
        [TextArea(3,10)]
        public string content;
        
    }
}