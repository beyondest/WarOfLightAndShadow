using Unity.Entities;
using Unity.Mathematics;

namespace Unity.Rendering
{
    [MaterialProperty("_IsLight")]
    public struct IsLightFloatOverride : IComponentData
    {
        public float Value;
    }
}
