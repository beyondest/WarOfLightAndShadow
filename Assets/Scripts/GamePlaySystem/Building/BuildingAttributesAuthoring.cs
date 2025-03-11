using Unity.Entities;
using UnityEngine;
using Unity.Mathematics;
namespace SparFlame.GamePlaySystem.Building
{
    public class BuildingAttributesAuthoring : MonoBehaviour
    {
        private class BuildingAttributesAuthoringBaker : Baker<BuildingAttributesAuthoring>
        {
            public override void Bake(BuildingAttributesAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.WorldSpace);
                var boxCollider = authoring.GetComponent<BoxCollider>();
                AddComponent(entity, new BuildingAttr
                {
                    State = BuildingState.Constructing,
                    BoxColliderSize = boxCollider.size,
                });

            }
        }
    }
    public enum BuildingState
    {
        Constructing,
        Constructed,
        Producing,
        Produced,
        Idle
    }
    
    public struct BuildingAttr : IComponentData
    {
        public BuildingState State;
        public float3 BoxColliderSize;
    }
}