using System;
using SparFlame.Components.General;
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

    public enum CityTaskType
    {
        StorageAdd = 0,
        PlantGenerator = 1,
    }


    
    [Serializable]
    public struct CityTask : IBufferElementData
    {
        public ResourceType resourceType;
        public int storageAddAmount;
        public float finishTotalHours;
        public int fromBuildingUniqueId;
        public float hoursPerUnit;
        public int remainingConjuredUnitCount;
        public CityTaskType taskType;
    }
    
    
    [Serializable]
    public struct CityResourceEntry : IBufferElementData
    {
        public ResourceData resourceData;
        public float accumulatedHours;
    }
    
    

}