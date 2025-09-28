using System;
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
        SoulPact = 0, // Population
        Mana = 1, // Conjuring Resource
        Crystal = 2, // Building Resource
        Essence = 3, // Money
        Aetherium = 4, // Rare Resource
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
        public int PrefabId { get; set; }
        public float Probability;
        public Range AmountRange;
    }


    [Serializable]
    public struct ResourceData : IBufferElementData
    {
        public ResourceType resourceType;
        public int availableAmount;
        public int storage;
        public float amountPerHour;
    }

    [Serializable]
    public struct PopulationResourceData : IComponentData
    {
        public ResourceType populationResourceType;
        public int storage;
        public int occupiedCount;
        public int virtualOccupiedCount;
    }

    [Serializable]
    public struct PopulationStorageAddTask : IBufferElementData
    {
        public long fromBuildingSingleId;
        public int addAmount;
        public float finishTotalHours;
    }

    [Serializable]
    public struct PopulationConjureTask : IBufferElementData
    {
        public long fromBuildingSingleId;
        public float hoursPerUnit;
        
        public int remainingConjuredUnitCount;
        public float accumulatedHours;
    }


    public enum ResourceRequestType
    {
        Generate = 0,
        Consume = 1,
        PopulationRelease = 2,
        ConstructingBuildingDestroyedAndRemoveTask = 4,
        DecreaseStorage = 5,
        StorageAddByTask = 6,
        GenerateSpeedAddByTask = 7,
        DecreaseGenerateSpeed = 8,
        IncreaseGenerateSpeed = 9,
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

        public long FromBuildingSingleId;

        public float FinishTotalHours;
        // public ResourceBuildingDestroyType DestroyType;
    }
}