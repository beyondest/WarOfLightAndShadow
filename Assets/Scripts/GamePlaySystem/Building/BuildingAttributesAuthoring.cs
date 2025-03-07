using Unity.Entities;
using UnityEngine;

namespace SparFlame.GamePlaySystem.Building
{
    public class BuildingAttributesAuthoring : MonoBehaviour
    {
        private class BuildingAttributesAuthoringBaker : Baker<BuildingAttributesAuthoring>
        {
            public override void Bake(BuildingAttributesAuthoring authoring)
            {
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
    public struct BuildingAttributes : IComponentData
    {
        public BuildingState State;
    }
}