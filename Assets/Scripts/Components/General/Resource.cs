using System;
using System.Runtime.CompilerServices;
using Sirenix.OdinInspector;
using SparFlame.Core.Interfaces;
using Unity.Entities;
using Range = SparFlame.Core.Structs.Range;

namespace SparFlame.Components.General
{
    [Serializable]
    public struct CostResourceTypeAmountPair
    {
        [HideLabel] public ResourceType type;
        [HideLabel] public int amount;
    }

    public struct CostList : IBufferElementData
    {
        public ResourceType Type;
        public int Amount;
    }

    public enum ResourceType
    {
        SoulPact = 0, 
        Essence = 1,  
        Mana = 2,
        Crystal = 3,
        Aetherium = 4,
    }

    public struct ResourceAttr : IComponentData
    {
        public Range AmountRange;
        public ResourceType Type;
    }

    public struct RenewableData : IComponentData
    {
        public float RegeneratingTimeHours;
        public float RegeneratingLeftTime;
    }

    public struct RegeneratingTag : IComponentData
    {
    }

    public struct ResourceEntityPrefabData : IEntityPrefabData<ResourceType>
    {
        public Entity Prefab { get; set; }
        public ResourceType Type { get; set; }
        public int GlobalIdx { get; set; }
        public float Probability;
        public Range AmountRange;
    }


 

    [Serializable]
    public struct ResourceData : IBufferElementData
    {
        public ResourceType resourceType;
        public int availableAmount;
        public int storage;
        public float hoursPerUnit;
        
        // These 2 fields are for population resource type only
        public int virtualOccupiedCount;
        public int occupiedCount;
    }

  

 




    public enum ResourceRequestType
    {
        Generate = 0,
        Consume = 1,
        PopulationRelease = 2,
        ResourceBuildingDestroyedWhenConstructing  = 4,
        ResourceBuildingDestroyedAfterConstruction = 5,
        StorageAddByTask = 6,
        GenerateSpeedAddByTask = 7,
        DecreaseGenerateSpeedForResourceMine = 8,
        IncreaseGenerateSpeedForResourceMine = 9,
        ConjureUnitByTask = 10,
        ConjureBuildingDestroyed = 11,
        
        // Storage add will not be used because it is handled by city resource system, in tasks method
        // StorageAdd = 5,
    }

    // public enum ResourceBuildingDestroyType
    // {
    //     ConstructionStateDestroyed = 0,
    //     StorageDecreaseOnly = 1
    // }
    
    public struct ResourceChangeRequest : IComponentData
    {
        public ResourceType ResourceType;
        public Entity City;

        /// <summary>
        /// This value must be positive
        /// </summary>
        public int AbsAmount;
        public float HoursPerUnit;

        public ResourceRequestType RequestType;
        
        public int FromBuildingUniqueId;
        public float FinishTotalHours;
        // public ResourceBuildingDestroyType DestroyType;
    }
    
    
    public struct PopulationResourceType : IComponentData
    {
        public ResourceType Value;

    }


  
}