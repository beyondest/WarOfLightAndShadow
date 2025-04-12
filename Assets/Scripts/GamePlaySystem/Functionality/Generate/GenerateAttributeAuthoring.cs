using SparFlame.GamePlaySystem.Resource;
using UnityEngine;
using Unity.Entities;

namespace SparFlame.GamePlaySystem.Generate
{
    public class GenerateAttributeAuthoring : MonoBehaviour
    {
        public ResourceType generateResourceType;
        private class GenerateAttributeAuthoringBaker : Baker<GenerateAttributeAuthoring>
        {
            public override void Bake(GenerateAttributeAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.Dynamic);
                AddComponent(entity, new GenerateAttr
                {
                    GenerateResourceType = authoring.generateResourceType
                });
            }
        }
    }

    public struct GenerateAttr : IComponentData
    {
        public ResourceType GenerateResourceType;
        public int TargetAmount;
        public int GeneratedAmount;
        public int RemainingTime;
    }
}