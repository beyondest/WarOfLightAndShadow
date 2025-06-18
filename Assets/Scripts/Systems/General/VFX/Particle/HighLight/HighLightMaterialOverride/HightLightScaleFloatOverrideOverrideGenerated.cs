using Unity.Entities;
using Unity.Mathematics;

namespace Unity.Rendering
{
    [MaterialProperty("_HightLightScale")]
    public struct HightLightScaleFloatOverride : IComponentData
    {
        public float Value;
    }
}
