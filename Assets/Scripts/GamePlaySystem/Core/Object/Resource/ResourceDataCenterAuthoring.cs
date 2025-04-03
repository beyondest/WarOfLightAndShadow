using System;
using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;

namespace SparFlame.GamePlaySystem.Resource
{
    public class ResourceDataCenterAuthoring : MonoBehaviour
    {
        public List<InitResourceAmountPair> initResourceAmount;

        private class ResourceDataCenterAuthoringBaker : Baker<ResourceDataCenterAuthoring>
        {
            
            public override void Bake(ResourceDataCenterAuthoring authoring)
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
        [Serializable]
        public struct InitResourceAmountPair
        {
            public ResourceType resourceType;
            public int amount;
        }
    }
    public struct ResourceData : IBufferElementData
    {
        public ResourceType ResourceType;
        public int Amount;
    }

}