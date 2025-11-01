using System;
using SparFlame.Components.General;
using Unity.Entities;
using Unity.Mathematics;

namespace SparFlame.Components.MainGameplay
{
    [Serializable]
    public struct CityAttr : IComponentData
    {
      
    }

    [Serializable]
    public struct CityGarrisonAttr : IComponentData
    {
        public float3 garrisonOutBias;
        public int maxGarrisonCount;
    }

    public struct CityNeedInitModelTag : IComponentData {}
    public struct CityLightModelRoot : IComponentData { }
    public struct CityDarkModelRoot : IComponentData{}
    
    //-----------------Army Group--------------------------//
    public struct CityGarrisonEntity : IBufferElementData, ICityArmyGroupElement
    {
        public Entity ArmyGroup { get; set; }
        public long SingleId { get; set; }
    }

    // [Serializable]
    // public struct CityGarrisonTypeData : IBufferElementData
    // {
    //     public ArmyGroupIconType iconType;
    //     public int count;
    // }

    public struct CityEntityPrefabData : IBufferElementData
    {
        public Entity Prefab;
        public int PrefabId;
    }

    //----------------------- Resource -------------------------//
    
    public enum CityTaskType
    {
        StorageAdd = 0,
        PlantGenerator = 1,
    }

    [Serializable]
    public struct CityTask : IBufferElementData
    {
        public long fromBuildingSingleId;
        public float finishTotalHours;
        public float hoursPerUnit;
        public int storageAddAmount;
        
        public ResourceType resourceType;
        public CityTaskType taskType;
    }
    
    [Serializable]
    public struct CityResourceEntry : IBufferElementData
    {
        public ResourceData resourceData;
        public float accumulatedHours;
    }


    
    
   

    

    

}