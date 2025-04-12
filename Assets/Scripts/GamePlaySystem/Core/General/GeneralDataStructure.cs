using System;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace SparFlame.GamePlaySystem.General
{
    public struct GeneralDS
    {
        [Serializable]
        public struct Range
        {
            public float lower;
            public float upper;
        }
        
        [Serializable]
        public class DatabaseItemData
        {
            // Config. Those info will not 
            [Header("General")]
            
            public string gameplayName;
            
            public GameObject prefab;

            public int id;
            public int subKey;
            
            [TextArea(3, 10)]
            public string description;
            
            public AssetReferenceSprite sprite2D;
            
            
            
        }
    }
}
