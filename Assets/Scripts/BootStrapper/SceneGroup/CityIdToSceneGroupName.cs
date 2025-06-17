using System;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

namespace SparFlame.BootStrapper
{
    public class CityIdToSceneGroupName : MonoBehaviour
    {
        [TableList,SerializeField]
        private List<CityIdToSceneGroupNameMapping> mappings;
        
        public static CityIdToSceneGroupName Instance;
        public readonly Dictionary<int, string> CityIdToSceneGroupNameDict = new();

        private void Awake()
        {
            if(!Instance)
                Instance = this;
            else
                Destroy(gameObject);
        }

        private void Start()
        {
            foreach (var map in mappings)
            {
                CityIdToSceneGroupNameDict.Add(map.cityId, map.sceneGroupName);
            }
        }
        [Serializable]
        public class CityIdToSceneGroupNameMapping
        {
            public int cityId;
            public string sceneGroupName;
        }
    }

    
}