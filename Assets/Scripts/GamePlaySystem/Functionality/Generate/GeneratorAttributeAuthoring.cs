using SparFlame.GamePlaySystem.Resource;
using UnityEngine;
using Unity.Entities;

namespace SparFlame.GamePlaySystem.Generate
{
    public class GeneratorAttributeAuthoring : MonoBehaviour
    {
        public ResourceType generateResourceType;
        public float generateInitialSpeed;
        private class GenerateAttributeAuthoringBaker : Baker<GeneratorAttributeAuthoring>
        {
            public override void Bake(GeneratorAttributeAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.Dynamic);
                AddComponent(entity, new GenerateAttr
                {
                    GenerateResourceType = authoring.generateResourceType,
                    CurGenerateSpeed = authoring.generateInitialSpeed,
                });
            }
        }
    }

    public struct GenerateAttr : IComponentData
    {
        public ResourceType GenerateResourceType;
        public int MinCultivatorsRequireToGenerate;
        public float GenerateInitialSpeed;
        public float CurGenerateSpeed;
        public float MaxGenerateSpeed;
    }
    /// <summary>
    /// InternalData
    /// </summary>
    public struct GenerateData : IComponentData
    {
        public float GenerateTime;
    }

}