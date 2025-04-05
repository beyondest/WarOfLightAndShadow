using System;
using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;

namespace SparFlame.GamePlaySystem.Resource
{
    public class CostAttributesAuthoring : MonoBehaviour
    {
        public List<CostResourceTypeAmountPair> costResourceTypeAmount;
        private class CostAttributesAuthoringBaker : Baker<CostAttributesAuthoring>
        {
            public override void Bake(CostAttributesAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.Dynamic);
                var buffer = AddBuffer<CostList>(entity);
                foreach (var pair in authoring.costResourceTypeAmount)
                {
                    buffer.Add(new CostList
                    {
                        Type = pair.costResourceType,
                        Amount = pair.amount
                    });
                }
            }
        }
    }

    [Serializable]
    public struct CostResourceTypeAmountPair
    {
        public ResourceType costResourceType;
        public int amount;
    }
}