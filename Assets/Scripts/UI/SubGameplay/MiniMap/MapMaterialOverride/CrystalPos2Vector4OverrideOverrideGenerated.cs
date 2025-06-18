using Unity.Entities;
using Unity.Mathematics;

namespace Unity.Rendering
{
    [MaterialProperty("_CrystalPos2")]
    public struct CrystalPos2Vector4Override : IComponentData
    {
        public float4 Value;
    }
}
