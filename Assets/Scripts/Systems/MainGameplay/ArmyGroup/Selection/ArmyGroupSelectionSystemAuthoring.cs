using SparFlame.Components.MainGameplay;
using Unity.Entities;
using UnityEngine;

namespace SparFlame.Systems.MainGameplay.ArmyGroup
{
    public class ArmyGroupSelectionSystemAuthoring : MonoBehaviour
    {
        public float dragMinDistanceSq;
        private class ArmyGroupSelectionSystemAuthoringBaker : Baker<ArmyGroupSelectionSystemAuthoring>
        {
            public override void Bake(ArmyGroupSelectionSystemAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.None);
                AddComponent<ArmyGroupSelectionData>(entity);
                AddComponent(entity, new ArmyGroupSelectionConfig { DragMinDistanceSq = authoring.dragMinDistanceSq });
            }
        }
    }


    public struct ArmyGroupSelectionConfig : IComponentData
    {
        public float DragMinDistanceSq;

    }

 
    public struct LockArmyGroupSelectedWorkForDrag : IComponentData,IEnableableComponent
    {
        
    }
}