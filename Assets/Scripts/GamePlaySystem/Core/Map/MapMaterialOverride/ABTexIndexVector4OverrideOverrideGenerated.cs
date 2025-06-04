using Unity.Entities;
using Unity.Mathematics;

namespace Unity.Rendering
{
    [MaterialProperty("_ABTexIndex")]
    public struct AbTexIndexVector4Override : IComponentData
    {
        public float4 Value;
    }

    [MaterialProperty("_Tiling")]
    public struct TilingVector4Override : IComponentData
    {
        public float4 Value;
    }

    [MaterialProperty("_Offset")]
    public struct OffsetVector4Override : IComponentData
    {
        public float4 Value;
    }

    [MaterialProperty("_Rotation")]
    public struct RotationFloatOverride : IComponentData
    {
        public float Value;
    }
}
