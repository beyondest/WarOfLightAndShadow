using Unity.Entities;
using Unity.Mathematics;
using Unity.Rendering;
using UnityEngine;

namespace SparFlame.Systems.General.VFX
{
    public class HighLightableMeshAuthoring : MonoBehaviour
    {
        private class HighLightableMeshAuthoringBaker : Baker<HighLightableMeshAuthoring>
        {
            public override void Bake(HighLightableMeshAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.Renderable);
                AddComponent(entity,new HightLightScaleFloatOverride
                {
                    Value = 0
                });
                AddComponent(entity, new HighLightColorVector4Override
                {
                    Value = float4.zero
                });
                AddComponent<HighLightableTag>(entity);
            }
        }
    }

    public struct HighLightableTag : IComponentData
    {
        
    }
}