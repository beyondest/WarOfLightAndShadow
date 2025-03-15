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
        
        private Dictionary<ObstacleShapeType, GameObject> _typePrefabMap ;
        private class Baker : Baker<ObstacleSystemAuthoring>
        {
            public override void Bake(ObstacleSystemAuthoring authoring)
            {
                authoring._typePrefabMap = new Dictionary<ObstacleShapeType, GameObject>();
                foreach (var pair in authoring.list.Where(pair => !authoring._typePrefabMap.ContainsKey(pair.shapeType)))
                {
                    authoring._typePrefabMap.Add(pair.shapeType, pair.prefab);
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
        [FormerlySerializedAs("prefabType")] public ObstacleShapeType shapeType;
        public GameObject prefab;
    }
    
    public enum ObstacleShapeType
    {
        Cube,
        Others
    }

    public class ObstacleSystemConfig : IComponentData
    {
        public Dictionary<ObstacleShapeType, GameObject> TypePrefabMap;
        public float SyncTimeInterval;
    }
    
}