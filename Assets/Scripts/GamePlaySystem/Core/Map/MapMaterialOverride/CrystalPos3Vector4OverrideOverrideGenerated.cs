using Unity.Entities;
using Unity.Mathematics;

namespace Unity.Rendering
{
    [MaterialProperty("_CrystalPos3")]
    public struct CrystalPos3Vector4Override : IComponentData
    {
        public float4 Value;
    }
}
