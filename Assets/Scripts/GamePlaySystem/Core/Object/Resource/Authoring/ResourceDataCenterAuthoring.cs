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
                
                var entity = GetEntity(TransformUsageFlags.None);
                var initBuffer = AddBuffer<ResourceTypeToInitAmount>(entity);
                var dataBuffer = AddBuffer<ResourceTypeToAvailableAmount>(entity);
                var dict = new Dictionary<ResourceType, int>();
                foreach (var pair in authoring.initResourceAmount)
                {
                    if (!dict.TryAdd(pair.resourceType, pair.amount))
                        throw new ArgumentException("Init resource data center has duplicated resource types");
                }

                foreach (ResourceType type in Enum.GetValues(typeof(ResourceType)))
                {
                    if (!dict.ContainsKey(type))
                    {
                        initBuffer.Add(new ResourceTypeToInitAmount
                        {
                            Amount = 0,
                            ResourceType = type
                        });
                        
                    }
                    else
                    {
                        initBuffer.Add(new ResourceTypeToInitAmount
                        {
                            ResourceType = type,
                            Amount = dict[type]
                        });
                    }
                    dataBuffer.Add(new ResourceTypeToAvailableAmount
                    {
                        ResourceType = type,
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
                        AddComponent<PopulationSpecialData>(entity);
                        break;
                    case FactionTag.Enemy:
                        AddComponent<EnemyResourceDataTag>(entity);
                        AddComponent<PopulationSpecialData>(entity);
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