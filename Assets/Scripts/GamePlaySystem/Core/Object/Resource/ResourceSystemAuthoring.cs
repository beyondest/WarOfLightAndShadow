using System;
using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;

namespace SparFlame.GamePlaySystem.Resource
{
    public class ResourceSystemAuthoring : MonoBehaviour
    {
        public List<InitResourceAmountPair> initResourceAmount;
        private class ResourceSystemAuthoringBaker : Unity.Entities.Baker<ResourceSystemAuthoring>
        {
            public override void Bake(ResourceSystemAuthoring authoring)
            {

                var entity = GetEntity(TransformUsageFlags.None);
                var buffer = AddBuffer<ResourceData>(entity);
                var count = 0;
                foreach (var pair in authoring.initResourceAmount)
                {
                    if (count != (int)pair.resourceType)
                    {
                        Debug.LogError("Init error, list must obey the sequence of enum");
                        return;
                    }
                    count++;
                    buffer.Add(new ResourceData
                    {
                        ResourceType = pair.resourceType,
                        Amount = pair.amount
                    });
                }
            }
        }
    }

    [Serializable]
    public struct InitResourceAmountPair
    {
        public ResourceType resourceType;
        public int amount;
    }
    
    public enum ResourceType
    {
        // Total
        Essence = 0, 
        
        // Summon
        LightEnergy = 1, 
        DarkEnergy = 2,
        
        // Building
        Luminite = 3,
        Obsidian = 4
    }

    public struct ResourceData : IBufferElementData
    {
        public ResourceType ResourceType;
        public int Amount;
    }
    
    
    public struct CostList : IBufferElementData
    {
        public ResourceType Type;
        public int Amount;
    }
}