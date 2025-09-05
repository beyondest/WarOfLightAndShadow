using Unity.Entities;
using Unity.Mathematics;

namespace Unity.Rendering
{
    [MaterialProperty("_PopNumberColor")]
    public struct PopNumberColorVector4Override : IComponentData
    {
        public float4 Value;
    }
}
