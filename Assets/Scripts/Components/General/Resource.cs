using System;
using Sirenix.OdinInspector;
using Unity.Entities;
using Range = SparFlame.Core.Structs.Range;

namespace SparFlame.Components.General
{
    public interface IEntityPrefabData<T> : IBufferElementData where T : Enum
    {
        public Entity Prefab { get; set; }
        public T Type { get; set; }
        public int PrefabId { get; set; }
    }

    /// <summary>
    /// Every IResourceManager should be put in bootStrapper scene,
    /// and if it is to be destroyed, you have to manually release resource and unregister
    /// </summary>
    public interface IResourceManager
    {
        float InitProgress { get; }
        bool IsInitialized { get; }
        void LoadResources();
        void UnloadResources();
    }

    [Serializable]
    public struct CostResourceTypeAmountPair
    {
        [HideLabel] public int amount;
        [HideLabel] public ResourceType type;
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
        public float amountPerHour;
        public int availableAmount;
        public int storage;
        public ResourceType resourceType;
    }

    [Serializable]
    public struct PopulationResourceData : IComponentData
    {
        public int storage;
        public int occupiedCount;
        public int virtualOccupiedCount;
        public ResourceType populationResourceType;
    }

    [Serializable]
    public struct PopulationStorageAddTask : IBufferElementData
    {
        public long fromBuildingSingleId;
        public float finishTotalHours;
        public int addAmount;
    }

    [Serializable]
    public struct PopulationConjureTask : IBufferElementData
    {
        public long fromBuildingSingleId;
        public float hoursPerUnit;
        public float accumulatedHours;
        public int remainingConjuredUnitCount;
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
    }


    public struct ResourceChangeRequest : IComponentData
    {
        public Entity City;
        public long FromBuildingSingleId;
        public float HoursPerUnit;
        public float FinishTotalHours;

        /// <summary>
        /// This value MUST be POSITIVE
        /// </summary>
        public int AbsAmount;

        public ResourceRequestType RequestType;
        public ResourceType ResourceType;
    }
}