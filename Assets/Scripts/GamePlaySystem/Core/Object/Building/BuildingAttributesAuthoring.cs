using Unity.Entities;
using UnityEngine;
using Unity.Mathematics;
namespace SparFlame.GamePlaySystem.Building
{
    public class BuildingAttributesAuthoring : MonoBehaviour
    {
        public Tier tier = Tier.Tier1;
        private class BuildingAttributesAuthoringBaker : Baker<BuildingAttributesAuthoring>
        {
            public override void Bake(BuildingAttributesAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.WorldSpace);
                AddComponent(entity, new BuildingAttr
                {
                    State = BuildingState.Constructing,
                    Tier = authoring.tier,
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
        public BuildingState State;
        public Tier Tier;
    }
}