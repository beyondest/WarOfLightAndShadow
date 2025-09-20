using Unity.Entities;
using Unity.Mathematics;

namespace Unity.Rendering
{
    [MaterialProperty("_ImageID")]
    public struct ArmyGroupBillboardImageIDFloatOverride : IComponentData
    {
        public float Value;
    }

    [MaterialProperty("_BaseColor")]
    public struct ArmyGroupBillboardBaseColorOverride : IComponentData
    {
        public float4 Value;
    }
}

namespace SparFlame.Components.MainGameplay
{
    public struct ArmyGroupBillboardConfig : IComponentData
    {
        public int IconChildIndex;
        public int SelectChildIndex;
        public float4 LightColor;
        public float4 DarkColor;
    }
}
