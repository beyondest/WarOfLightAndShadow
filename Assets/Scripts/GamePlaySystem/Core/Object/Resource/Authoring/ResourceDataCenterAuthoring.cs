using System;
using System.Collections.Generic;
using SparFlame.GamePlaySystem.General;
using Unity.Entities;
using UnityEngine;

namespace SparFlame.GamePlaySystem.Resource
{
    public class ResourceDataCenterAuthoring : MonoBehaviour
    {
        [Header("All type init resource")]
        public List<InitResourceAmountPair> initResourceAmount;
        public FactionTag factionTag;
        
        private class ResourceDataCenterAuthoringBaker : Baker<ResourceDataCenterAuthoring>
        {
            public override void Bake(ResourceDataCenterAuthoring authoring)
            {
                if (authoring.initResourceAmount.Count != Enum.GetValues(typeof(ResourceType)).Length)
                {
                    throw new ArgumentException("Init resource counts must contain all resource types.");
                }
                var entity = GetEntity(TransformUsageFlags.None);
                var initBuffer = AddBuffer<InitResourceData>(entity);
                var dataBuffer = AddBuffer<ResourceAvailableData>(entity);
                var count = 0;
                foreach (var pair in authoring.initResourceAmount)
                {
                    if (count != (int)pair.resourceType)
                    {
                        Debug.LogError("Init error, list must obey the sequence of enum");
                        return;
                    }
                    count++;
                    initBuffer.Add(new InitResourceData
                    {
                        ResourceType = pair.resourceType,
                        Amount = pair.amount
                    });
                    dataBuffer.Add(new ResourceAvailableData
                    {
                        ResourceType = pair.resourceType,
                        Amount = 0
                    });
                }
                
                switch (authoring.factionTag)
                {
                    case FactionTag.Neutral:
                        AddComponent<GlobalResourceDataTag>(entity);
                        break;
                    case FactionTag.Ally:
                        AddComponent<AllyResourceDataTag>(entity);
                        AddComponent<PopulationOccupiedData>(entity);
                        break;
                    case FactionTag.Enemy:
                        AddComponent<EnemyResourceDataTag>(entity);
                        AddComponent<PopulationOccupiedData>(entity);
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
        }
            
        [Serializable]
        public struct InitResourceAmountPair
        {
            public ResourceType resourceType;
            public int amount;
        }
    }
    /// <summary>
    /// In sequence of (int)resourceType, can get through index
    /// </summary>
    public struct ResourceAvailableData : IBufferElementData
    {
        public ResourceType ResourceType;
        public int Amount;
    }

    public struct InitResourceData : IBufferElementData
    {
        public ResourceType ResourceType;
        public int Amount;
    }

    public struct PopulationOccupiedData : IComponentData
    {
        public int Value;
    }

    public struct AllyResourceDataTag : IComponentData
    {
        
    }

    public struct EnemyResourceDataTag : IComponentData
    {
        
    }

    public struct GlobalResourceDataTag : IComponentData
    {
        
    }

}