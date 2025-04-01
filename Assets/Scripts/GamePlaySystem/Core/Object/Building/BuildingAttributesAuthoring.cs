using SparFlame.GamePlaySystem.General;
using Unity.Entities;
using UnityEngine;
namespace SparFlame.GamePlaySystem.Building
{
    public class BuildingAttributesAuthoring : MonoBehaviour
    {
        public BuildingType buildingType;
        private class BuildingAttributesAuthoringBaker : Baker<BuildingAttributesAuthoring>
        {
            public override void Bake(BuildingAttributesAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.WorldSpace);
                AddComponent(entity, new BuildingAttr
                {
                    Type = authoring.buildingType,
                    State = BuildingState.Constructing,
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
        Idle,
        UnderAttack
    }
    
    public struct BuildingAttr : IComponentData
    {
        public BuildingType Type;
        public BuildingState State;
    }
}