using Unity.Entities;
using UnityEngine;

namespace GamePlaySystem.Functionality.MainGameplay.ArmyGroup.Sight
{
    public class ArmyGroupSightSystemAuthoring : MonoBehaviour
    {
        private class ArmyGroupSightSystemAuthoringBaker : Baker<ArmyGroupSightSystemAuthoring>
        {
            public override void Bake(ArmyGroupSightSystemAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.None);
                AddComponent<ArmyGroupSightConfig>(entity);
            }
        }
    }

    public struct ArmyGroupSightConfig : IComponentData
    {
        
    }
}