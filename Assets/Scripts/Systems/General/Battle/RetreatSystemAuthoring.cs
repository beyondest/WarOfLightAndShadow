using System;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace SparFlame.Systems.General.Battle
{
    public class RetreatSystemAuthoring : MonoBehaviour
    {
        public RetreatSystemConfig config;
        private class RetreatSystemAuthoringBaker : Baker<RetreatSystemAuthoring>
        {
            public override void Bake(RetreatSystemAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.None);
                AddComponent(entity,authoring.config);
            }
        }
    }

    [Serializable]
    public struct RetreatSystemConfig : IComponentData
    {
        public float retreatSquareInterval;
        public float3 firstBias;
    }
}