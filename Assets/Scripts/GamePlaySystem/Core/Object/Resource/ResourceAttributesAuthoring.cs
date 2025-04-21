using SparFlame.Utils;
using Unity.Entities;
using UnityEngine;
namespace SparFlame.GamePlaySystem.Resource
{
    public class ResourceAttributesAuthoring : MonoBehaviour
    {
        public ResourceType resourceType;
        public bool renewable;
        public float regeneratingTime;
        public CustomDs.Range amountRange;
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
                        RegenerationTimeSeconds =  authoring.regeneratingTime,
                    });
                }
            }
        }
    }



    public struct ResourceAttr : IComponentData
    {
        public CustomDs.Range AmountRange; 
        public ResourceType Type;
    }

    public struct RenewableData : IComponentData
    {
        public float RegenerationTimeSeconds;
        public float RegeneratingLeftTime;
    }
    
    public struct RegeneratingTag : IComponentData
    {
        
    }
}