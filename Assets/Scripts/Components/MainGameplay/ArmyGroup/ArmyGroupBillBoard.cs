using Unity.Entities;
using Unity.Mathematics;
using Unity.Rendering;


namespace SparFlame.Components.MainGameplay
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
    public struct ArmyGroupBillboardConfig : IComponentData
    {
        public float4 LightColor;
        public float4 DarkColor;
        public int IconChildIndex;
        public int SelectChildIndex;
    }
}
