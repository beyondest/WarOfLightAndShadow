using Unity.Entities;

namespace SparFlame.Components.MainGameplay
{
    public struct CityAttr : IComponentData
    {
        public int ID;
        public int MaxGarrisonCount;
    }

    public struct CityData : IComponentData
    {
        
    }

    public struct CityGarrisonEntity : IBufferElementData
    {
        public Entity ArmyGroup;
    }

    public struct CityGarrisonTypeData : IBufferElementData
    {
        public ArmyGroupIconType IconType;
        public int Count;
    }
}