using Unity.Entities;
using Unity.Mathematics;

namespace Unity.Rendering
{
    [MaterialProperty("_MiniMapColor")]
    struct MiniMapColorVector4Override : IComponentData
    {
        public float4 Value;
    }
}
