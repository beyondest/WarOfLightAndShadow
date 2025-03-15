using Unity.Entities;
using UnityEngine;
using Unity.Mathematics;
namespace SparFlame.GamePlaySystem.Resource
{
    public class ResourceAttributesAuthoring : MonoBehaviour
    {
        
        private class ResourceAttributesAuthoringBaker : Baker<ResourceAttributesAuthoring>
        {
            public override void Bake(ResourceAttributesAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.WorldSpace);
                var boxColliderSize = authoring.GetComponent<BoxCollider>().size;
                AddComponent(entity, new ResourceAttr
                {
                    State = ResourceState.Available,
                    BoxColliderSize = boxColliderSize,
                });
            }
        }
    }

    public enum ResourceState
    {
        Available,
        Depleted,
        Harvesting
    }

    public struct ResourceAttr : IComponentData
    {
        public ResourceState State;
        public float3 BoxColliderSize;
    }
}