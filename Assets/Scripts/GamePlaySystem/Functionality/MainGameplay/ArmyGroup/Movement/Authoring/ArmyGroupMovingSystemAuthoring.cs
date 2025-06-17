using System;
using System.ComponentModel;
using Sirenix.OdinInspector;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace SparFlame.GamePlaySystem.Functionality.MainGameplay.ArmyGroup
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
    }

    public struct ArmyGroupPathVisualizeConfig : IComponentData
    {
        public Entity ReachableRef;
        public Entity UnreachableRef;
        public float Interval;
        
    }


    
}