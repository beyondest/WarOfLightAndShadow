using SparFlame.Components.MainGameplay;
using Unity.Entities;
using UnityEngine;

namespace SparFlame.Systems.MainGameplay.ArmyGroup
{
    public class ArmyGroupWalkableTagAuthoring : MonoBehaviour
    {
        private class ArmyGroupWalkableTagAuthoringBaker : Baker<ArmyGroupWalkableTagAuthoring>
        {
            public override void Bake(ArmyGroupWalkableTagAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.Dynamic);
                AddComponent<ArmyGroupWalkableTag>(entity);
            }
        }
    }
}