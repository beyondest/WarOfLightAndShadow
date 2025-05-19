using Unity.Entities;
using Unity.Mathematics;

namespace Unity.Rendering
{
    [MaterialProperty("_HighLightColor")]
    public struct HighLightColorVector4Override : IComponentData
    {
        public float4 Value;
    }
}
