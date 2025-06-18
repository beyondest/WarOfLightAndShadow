using Unity.Entities;
using Unity.Mathematics;

namespace Unity.Rendering
{
    [MaterialProperty("_NoiseScale")]
    public struct NoiseScaleFloatOverride : IComponentData
    {
        public float Value;
    }
}
