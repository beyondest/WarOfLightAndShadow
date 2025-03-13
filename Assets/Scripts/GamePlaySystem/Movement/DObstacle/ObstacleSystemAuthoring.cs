using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Entities;
using UnityEngine;
using UnityEngine.Serialization;

namespace SparFlame.GamePlaySystem.Movement
{
    public class ObstacleSystemAuthoring : MonoBehaviour
    {
        
        [Tooltip("If you want to add more Dynamic obstacle prefabs, please implement more types")]
        public List<ObstaclePrefabPair> list;
        [Tooltip("This syncTimeInterval affects all dynamic obstacle position async")]
        public float syncTimeInterval = 1f;
        
        private Dictionary<ObstaclePrefabType, GameObject> _typePrefabMap ;
        private class Baker : Baker<ObstacleSystemAuthoring>
        {
            public override void Bake(ObstacleSystemAuthoring authoring)
            {
                authoring._typePrefabMap = new Dictionary<ObstaclePrefabType, GameObject>();
                foreach (var pair in authoring.list.Where(pair => !authoring._typePrefabMap.ContainsKey(pair.prefabType)))
                {
                    authoring._typePrefabMap.Add(pair.prefabType, pair.prefab);
                }
                var entity = GetEntity(TransformUsageFlags.None);
                AddComponentObject(entity, new ObstacleSystemConfig
                {
                    TypePrefabMap = authoring._typePrefabMap,
                    SyncTimeInterval = authoring.syncTimeInterval,
                });
            }
        }
    }

    [Serializable]
    public class ObstaclePrefabPair
    {
        public ObstaclePrefabType prefabType;
        public GameObject prefab;
    }
    
    public enum ObstaclePrefabType
    {
        Unit,
        Building1X,
        Building2X,
        Building4X,
    }

    public class ObstacleSystemConfig : IComponentData
    {
        public Dictionary<ObstaclePrefabType, GameObject> TypePrefabMap;
        public float SyncTimeInterval;
    }
    
}