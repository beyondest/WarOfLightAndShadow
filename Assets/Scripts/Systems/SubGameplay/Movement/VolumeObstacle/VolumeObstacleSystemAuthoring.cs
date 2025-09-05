using System;
using System.Collections.Generic;
using System.Linq;
using SparFlame.Components.General;
using Unity.Entities;
using UnityEngine;
using UnityEngine.AI;

namespace SparFlame.Systems.SubGameplay.Movement
{
    public class VolumeObstacleSystemAuthoring : MonoBehaviour
    {
        [Tooltip("If you want to add more Dynamic obstacle prefabs, please implement more types")]
        public List<VolumeObstaclePrefabPair> obstaclePrefabs;
        public List<VolumeObstaclePrefabPair> volumePrefabs;

        [Tooltip("This syncTimeInterval affects all dynamic obstacle position async")]
        public float syncTimeInterval = 1f;


        public NavMeshAgent allyAgent;
        public NavMeshAgent enemyAgent;
        // private Dictionary<VolumeObstacleShapeType, GameObject> _obstaclePrefabMap;
        // private Dictionary<VolumeObstacleShapeType, GameObject> _volumePrefabMap;

        private class Baker : Baker<VolumeObstacleSystemAuthoring>
        {
            public override void Bake(VolumeObstacleSystemAuthoring authoring)
            {
                var obstaclePrefabMap = authoring.obstaclePrefabs.ToDictionary(obstaclePrefab => obstaclePrefab.affectAgentType, obstaclePrefab => obstaclePrefab.prefab);
                var volumePrefabMap = authoring.volumePrefabs.ToDictionary(volumePrefab => volumePrefab.affectAgentType, volumePrefab => volumePrefab.prefab);
                
                var entity = GetEntity(TransformUsageFlags.None);
                AddComponentObject(entity, new VolumeObstacleSystemConfig
                {
                    ObstacleTypePrefabMap = obstaclePrefabMap,
                    VolumeTypePrefabMap = volumePrefabMap,
                    SyncTimeInterval = authoring.syncTimeInterval,
                    AllyAgentRadius = authoring.allyAgent.radius,
                    EnemyAgentRadius = authoring.enemyAgent.radius
                });
            }
        }
            
        [Serializable]
        public class VolumeObstaclePrefabPair
        {
            public FactionTag affectAgentType;
            public GameObject prefab;
        }
    }

    
    
  

    internal class VolumeObstacleSystemConfig : IComponentData
    {
        public Dictionary<FactionTag, GameObject> ObstacleTypePrefabMap;
        public Dictionary<FactionTag, GameObject> VolumeTypePrefabMap;
        public float SyncTimeInterval;
        public float AllyAgentRadius;
        public float EnemyAgentRadius;
    }

 
    


}