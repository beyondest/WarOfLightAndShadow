using Unity.Entities;
using UnityEngine;

namespace SparFlame.GamePlaySystem.Generate
{
    public class GeneratorSystemAuthoring : MonoBehaviour
    {
        public float generateIntervalSeconds;
        private class BuildingGenerateSystemAuthoringBaker : Baker<GeneratorSystemAuthoring>
        {
            public override void Bake(GeneratorSystemAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.None);
                AddComponent(entity, new BuildingGenerateSystemConfig
                {
                    GenerateIntervalSeconds = authoring.generateIntervalSeconds,
                });
            }
        }
    }

    public struct BuildingGenerateSystemConfig : IComponentData
    {
        public float GenerateIntervalSeconds;
    }

    public struct GeneratingTag : IComponentData
    {
        
    }
    
}