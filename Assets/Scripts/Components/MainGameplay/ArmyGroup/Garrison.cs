using Unity.Entities;

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
        public bool IfGarrisonIn;
        public ArmyGroupIconType IconType;
    }
}