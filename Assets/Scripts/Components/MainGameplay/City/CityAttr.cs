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
        public int maxGarrisonCount;
        public float3 garrisonOutBias;
    }

    public struct CityData : IComponentData
    {
        
    }

    public struct CityNeedInitModelTag : IComponentData
    {
        
    }

    public struct CityLightModelRoot : IComponentData
    {
        
    }
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
        public ResourceType resourceType;
        public int storageAddAmount;
        public float finishTotalHours;
        public long fromBuildingSingleId;
        public float hoursPerUnit;
        
        public CityTaskType taskType;
    }
    
    
    [Serializable]
    public struct CityResourceEntry : IBufferElementData
    {
        public ResourceData resourceData;
        public float accumulatedHours;
    }


    
    
   

    

    

}