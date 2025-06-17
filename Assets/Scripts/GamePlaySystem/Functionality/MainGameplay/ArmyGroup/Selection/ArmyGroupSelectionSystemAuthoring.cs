using SparFlame.GamePlaySystem.General;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace SparFlame.GamePlaySystem.Functionality.MainGameplay.ArmyGroup
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

    public struct ArmyGroupSelectionData : IComponentData
    {
                
        public int CurrentSelectCount;
        public FactionTag CurrentSelectFaction;
        public float2 SelectionBoxStartPos;
        public float2 SelectionBoxEndPos;
        public bool DragSelectStart;
        public bool IsDragSelecting;
    }

    public struct ArmyGroupSelectionConfig : IComponentData
    {
        public float DragMinDistanceSq;

    }

    public struct ArmyGroupSelected : IComponentData, IEnableableComponent{}

    public struct LockArmyGroupSelectedWorkForDrag : IComponentData,IEnableableComponent
    {
        
    }
}