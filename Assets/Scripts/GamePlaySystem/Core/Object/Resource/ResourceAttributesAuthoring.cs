using SparFlame.GamePlaySystem.General;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Serialization;

namespace SparFlame.GamePlaySystem.Resource
{
    public class ResourceAttributesAuthoring : MonoBehaviour
    {
        public float harvestAmountMultiplier;
        public ResourceType resourceType;
        public bool renewable;
        public float regeneratingTime;
        public GeneralDS.Range amountRange;
        private class ResourceAttributesAuthoringBaker : Baker<ResourceAttributesAuthoring>
        {
            public override void Bake(ResourceAttributesAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.WorldSpace);
                AddComponent(entity, new ResourceAttr
                {
                    Type = authoring.resourceType,
                    AmountRange = authoring.amountRange
            
                });
                if (authoring.renewable)
                {
                    AddComponent(entity, new RenewableData
                    {
                        RegeneratingLeftTime = 0f,
                        RegeneratingTime =  authoring.regeneratingTime,
                    });
                }
            }
        }
    }

    public enum ResourceState
    {
        Available = 0,
        Regenerating = 1,
    }

    public struct ResourceAttr : IComponentData
    {
        public GeneralDS.Range AmountRange; 
        public ResourceType Type;
    }

    public struct RenewableData : IComponentData
    {
        public float RegeneratingTime;
        public float RegeneratingLeftTime;
    }
    
    public struct RegeneratingTag : IComponentData
    {
        
    }
}