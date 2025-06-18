using System;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace SparFlame.Systems.MainGameplay.ArmyGroup
{
    
    public class ArmyGroupNavAgentSystemAuthoring : MonoBehaviour
    {
        public ArmyGroupNavConfig config;
        private class ArmyGroupNavAgentSystemAuthoringBaker : Baker<ArmyGroupNavAgentSystemAuthoring>
        {
            public override void Bake(ArmyGroupNavAgentSystemAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.None);
                AddComponent(entity, authoring.config);
            }
        }
    }
    
    [Serializable]
    public struct ArmyGroupNavConfig : IComponentData
    {
        public int maxPathSize;
        public int maxIterations;
        public int pathNodePoolSize;
        public int initialNavMeshQueriesCapacity;
        public float3 extentsOffset;
    }
}