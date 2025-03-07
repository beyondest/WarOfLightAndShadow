using Unity.Entities;
using UnityEngine;

namespace SparFlame.GamePlaySystem.Resource
{
    public class ResourceAttributesAuthoring : MonoBehaviour
    {
        private class ResourceAttributesAuthoringBaker : Baker<ResourceAttributesAuthoring>
        {
            public override void Bake(ResourceAttributesAuthoring authoring)
            {
            }
        }
    }

    public enum ResourceState
    {
        Available,
        Depleted,
        Harvesting,
    }

    public struct ResourceAttributes : IComponentData
    {
        public ResourceState State;
    }
}