using Unity.Entities;
using Unity.Mathematics;

namespace Unity.Rendering
{
    [MaterialProperty("_NoiseWeight")]
    public struct NoiseWeightFloatOverride : IComponentData
    {
        public float Value;
    }
}