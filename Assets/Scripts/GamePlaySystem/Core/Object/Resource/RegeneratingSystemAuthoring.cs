using Unity.Entities;
using UnityEngine;

namespace SparFlame.GamePlaySystem.Resource
{
    public class RegeneratingSystemAuthoring : MonoBehaviour
    {
        public float regeneratingTimeScale = 1.0f;
        private class RegeneratingSystemAuthoringBaker : Baker<RegeneratingSystemAuthoring>
        {
            public override void Bake(RegeneratingSystemAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.None);
                AddComponent(entity, new RegeneratingSystemConfig
                {
                    RegeneratingTimeScale = authoring.regeneratingTimeScale,
                });
            }
        }
    }

    public struct RegeneratingSystemConfig : IComponentData
    {
        public float RegeneratingTimeScale;
    }
    
}