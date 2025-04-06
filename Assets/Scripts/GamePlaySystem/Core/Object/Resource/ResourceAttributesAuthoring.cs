using Unity.Entities;
using UnityEngine;
using UnityEngine.Serialization;

namespace SparFlame.GamePlaySystem.Resource
{
    public class ResourceAttributesAuthoring : MonoBehaviour
    {
        public ResourceState state = ResourceState.Available;
        public float harvestAmountMultiplier;
        public ResourceType resourceType;
        private class ResourceAttributesAuthoringBaker : Baker<ResourceAttributesAuthoring>
        {
            public override void Bake(ResourceAttributesAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.WorldSpace);
                AddComponent(entity, new ResourceAttr
                {
                    State = authoring.state,
                    Type = authoring.resourceType,
                    HarvestAmountMultiplier = authoring.harvestAmountMultiplier,
                });
            }
        }
    }

    public enum ResourceState
    {
        Available = 0,
        Depleted = 1,
        Harvesting = 2
    }

    public struct ResourceAttr : IComponentData
    {
        public ResourceState State;
        public ResourceType Type;
        public float HarvestAmountMultiplier;   // This is used for making different resource harvested in different difficulties
    }
}