using Unity.Entities;
using Unity.Mathematics;

namespace Unity.Rendering
{
    [MaterialProperty("_CrystalPos")]
    public struct CrystalPosVector4Override : IComponentData
    {
        public float4 Value;
    }
}
