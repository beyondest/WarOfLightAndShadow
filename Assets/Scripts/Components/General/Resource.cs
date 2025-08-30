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
        SoulPact = 0, // Basic Population
        Essence = 1, // Everywhere, generate and harvest 

        LightEnergy = 2, // Only generate
        DarkEnergy = 3, // Only generate

        Luminite = 4, //Only Harvest
        Obsidian = 5, // Only Harvest

        Aetherium = 6, // Only Harvest

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


    /// <summary>
    /// In sequence of (int)resourceType, can get through index
    /// </summary>
    public struct ResourceTypeToAvailableAmount : IBufferElementData
    {
        public ResourceType ResourceType;
        public int Amount;
    }

    public struct ResourceTypeToInitAmount : IBufferElementData
    {
        public ResourceType ResourceType;
        public int Amount;
    }

    public struct PopulationSpecialData : IComponentData
    {
        public int OccupiedAmount;
        public int TotalAmount;
    }

    public struct LightResourceDataTag : IComponentData
    {
    }

    public struct DarkResourceDataTag : IComponentData
    {
    }

    public struct GlobalResourceDataTag : IComponentData
    {
    }


    public enum ResourceRequestType
    {
        Harvest = 0,
        Generate = 1,
        Consume = 2,
        Release = 3,
        DwellingDestroyConsume = 4
    }

    public struct ResourceChangeRequest : IComponentData
    {
        public ResourceType Type;
        public FactionTag FromFaction;

        /// <summary>
        /// This value must be positive
        /// </summary>
        public int AbsAmount;

        public ResourceRequestType RequestType;
    }


    public struct ResourceUtils
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool GetCorrespondingResource(ResourceType resourceType,
            out ResourceType correspondingResourceType)
        {
            correspondingResourceType = resourceType;
            switch (resourceType)
            {
                case ResourceType.SoulPact:
                case ResourceType.Essence:
                case ResourceType.Aetherium:
                    return false;
                case ResourceType.LightEnergy:
                    correspondingResourceType = ResourceType.DarkEnergy;
                    return true;
                case ResourceType.DarkEnergy:
                    correspondingResourceType = ResourceType.LightEnergy;
                    return true;
                case ResourceType.Luminite:
                    correspondingResourceType = ResourceType.Obsidian;
                    return true;
                case ResourceType.Obsidian:
                    correspondingResourceType = ResourceType.Luminite;
                    return true;
              
                default:
                    throw new ArgumentOutOfRangeException(nameof(resourceType), resourceType, null);
            }
        }
    }
}