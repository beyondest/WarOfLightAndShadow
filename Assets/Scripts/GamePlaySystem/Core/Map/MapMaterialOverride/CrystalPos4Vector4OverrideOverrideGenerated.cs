using Unity.Entities;
using Unity.Mathematics;

namespace Unity.Rendering
{
    [MaterialProperty("_CrystalPos4")]
    public struct CrystalPos4Vector4Override : IComponentData
    {
        public float4 Value;
    }
}
