using System;
using Unity.Entities;

namespace SparFlame.Components.MainGameplay
{
    [Serializable]
    public struct CityAttr : IComponentData
    {
        public int globalId;
        public int maxGarrisonCount;
    }

    public struct CityData : IComponentData
    {
        
    }

    public struct CityGarrisonEntity : IBufferElementData
    {
        public Entity ArmyGroup;
    }

    [Serializable]
    public struct CityGarrisonTypeData : IBufferElementData
    {
        public ArmyGroupIconType iconType;
        public int count;
    }

    public struct CityEntityPrefabData : IBufferElementData
    {
        public Entity Prefab;
        public int GlobalIdx;
    }
}