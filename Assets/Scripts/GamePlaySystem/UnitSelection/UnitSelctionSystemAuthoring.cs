using UnityEngine;
using Unity.Entities;
using Unity.Mathematics;
using SparFlame.GamePlaySystem.General;


namespace SparFlame.GamePlaySystem.UnitSelection
{
    public class UnitSelectionSystemAuthoring : MonoBehaviour
    {
        public KeyCode addUnitKey = KeyCode.LeftShift;
        public float dragMinDistance = 0.01f;

        /// <summary>
        /// If Indicator is first child object, then index is 1
        /// </summary>
        public int selectedIndicatorIndex = 1;
        public TeamTag initSelectableTeam = TeamTag.Ally;

        class Baker : Baker<UnitSelectionSystemAuthoring>
        {
            public override void Bake(UnitSelectionSystemAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.Dynamic);
                AddComponent(entity, new UnitSelectionData
                {
                    CurrentSelectCount = 0,
                    CurrentSelectTeam = authoring.initSelectableTeam
                });
                AddComponent(entity, new UnitSelectionConfig
                {
                    AddUnitKey = authoring.addUnitKey,
                    DragMinDistanceSq = authoring.dragMinDistance * authoring.dragMinDistance,
                    SelectedIndicatorIndex = authoring.selectedIndicatorIndex
                });
            }
        }
    }

    public struct UnitSelectionConfig : IComponentData
    {
        public KeyCode AddUnitKey;
        public float DragMinDistanceSq;
        public int SelectedIndicatorIndex;
    }


    public struct UnitSelectionData : IComponentData
    {
        public int CurrentSelectCount;
        public TeamTag CurrentSelectTeam;
        public float2 SelectionBoxStartPos;
        public float2 SelectionBoxEndPos;
    }

}


