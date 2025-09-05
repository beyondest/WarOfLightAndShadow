using System;
using Unity.Entities;
using Unity.Mathematics;

namespace SparFlame.Components.MainGameplay
{
    public struct ArmyGroupInGarrison : IComponentData
    {
        public Entity City;
    }
    public struct ArmyGroupGarrisonRequest : IComponentData
    {
        public Entity ArmyGroup;
        public Entity City;
        public ArmyGroupIconType IconType;
        public bool IfGarrisonIn;
        public bool IfGarrisonOutAllSameIcon;
    }
    [Serializable]
    public struct ArmyGroupGarrisonSystemConfig : IComponentData
    {
        public float3 hidePositionBias;
    }
}