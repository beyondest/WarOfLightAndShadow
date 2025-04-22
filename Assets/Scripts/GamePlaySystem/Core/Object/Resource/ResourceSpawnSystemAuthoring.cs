using System;
using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;
using Random = Unity.Mathematics.Random;
namespace SparFlame.GamePlaySystem.Resource
{
    public class ResourceSpawnSystemAuthoring : MonoBehaviour
    {
        public uint seed = 1;
        public int timePointsCount;
        public int resourceTypeCount;
        private class ResourceSpawnSystemBaker : Baker<ResourceSpawnSystemAuthoring>
        {
            public override void Bake(ResourceSpawnSystemAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.None);
                AddComponent(entity, new ResourceSpawnSystemConfig
                {
                    TimePointsCount = authoring.timePointsCount,
                    ResourceTypeCount = authoring.resourceTypeCount,
                });
                AddComponent(entity, new ResourceSpawnRnd
                {
                    Rnd = new Random(authoring.seed)
                });
            }
        }
    }

    public struct ResourceSpawnSystemConfig : IComponentData
    {
        public int TimePointsCount;
        public int ResourceTypeCount;
    }

    public struct ResourceSpawnRnd : IComponentData
    {
        public Random Rnd;
    }
    
    public struct ResourceSpawnData : IBufferElementData
    {
        public ResourceTimePoints TimePoints;
        public ResourceType ResourceType;
        public int Amount;
    }
    
    public enum ResourceTimePoints
    {
        M1 = 1,
        M2 = 2,
        M3 = 3,
        M4 = 4,
        M5 = 5
    }
    
}