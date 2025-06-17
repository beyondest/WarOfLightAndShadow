using Unity.Entities;
using UnityEngine;

namespace SparFlame.GamePlaySystem.Functionality.MainGameplay.ArmyGroup
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
    public struct ArmyGroupWalkableTag : IComponentData{}
}