using System;
using Sirenix.OdinInspector;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace SparFlame.Systems.MainGameplay.ArmyGroup
{
    public class ArmyGroupMovingSystemAuthoring : MonoBehaviour
    {
        public ArmyGroupMovingSystemConfig config;


        [Header("Visualize config")] 

        public float armyGroupPathVisualizeInterval;
        [AssetsOnly]
        public GameObject reachableRef;
        [AssetsOnly]
        public GameObject unreachableRef;

        public ArmyGroupVolumeObstacleConfig volumeObstacleConfig;
        
        private class ArmyGroupMovingSystemAuthoringBaker : Baker<ArmyGroupMovingSystemAuthoring>
        {
            public override void Bake(ArmyGroupMovingSystemAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.None);
                AddComponent(entity, authoring.config);
                AddComponent(entity, new ArmyGroupPathVisualizeConfig
                {
                    ReachableRef = GetEntity(authoring.reachableRef,TransformUsageFlags.Dynamic),
                    UnreachableRef = GetEntity(authoring.unreachableRef,TransformUsageFlags.Dynamic),
                    Interval = authoring.armyGroupPathVisualizeInterval
                });
                var entity2 = CreateAdditionalEntity(TransformUsageFlags.None);
                AddComponentObject(entity2, authoring.volumeObstacleConfig);
            }
        }
    }

    [Serializable]
    public struct ArmyGroupMovingSystemConfig : IComponentData
    {
        public float finalReachRange;
        public float3 extents;
        public float waypointReachRange;
        public float rotationSpeed;
        public float moveSpeedScale;
        public int pathRecordingCountInterval;
    }

    public struct ArmyGroupPathVisualizeConfig : IComponentData
    {
        public Entity ReachableRef;
        public Entity UnreachableRef;
        public float Interval;
    }

    [Serializable]
    public class ArmyGroupVolumeObstacleConfig : IComponentData
    {
        [AssetsOnly]
        public GameObject lightCityObstacle;
        [AssetsOnly]
        public GameObject darkCityObstacle;
        [AssetsOnly]
        public GameObject neutralCityObstacle;
    }

 

    
}