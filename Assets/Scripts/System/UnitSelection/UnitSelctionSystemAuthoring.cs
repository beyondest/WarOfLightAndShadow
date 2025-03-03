using UnityEngine;
using Unity.Entities;
using Unity.Mathematics;
using SparFlame.System.General;


namespace SparFlame.System.UnitSelection
{
    public class UnitSelectionSystemAuthoring : MonoBehaviour
    {
        public KeyCode addUnitKey = KeyCode.LeftShift;
        public float dragMinDistance;

        /// <summary>
        /// If Indicator is first child object, then index is 1
        /// </summary>
        public int selectedIndicatorIndex;
        public TeamTag initSelectableTeam;

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


