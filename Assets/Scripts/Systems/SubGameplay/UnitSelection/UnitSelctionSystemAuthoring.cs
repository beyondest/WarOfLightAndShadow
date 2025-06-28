using SparFlame.Components.General;
using SparFlame.Components.SubGameplay;
using UnityEngine;
using Unity.Entities;
using UnityEngine.Serialization;


namespace SparFlame.Systems.SubGameplay.UnitSelection
{
    public class UnitSelectionSystemAuthoring : MonoBehaviour
    {
        public float dragMinDistance = 0.01f;
        public bool enableDebugSwitch;
        [Tooltip("When a game object use 2 physics shape, the second one will be placed in child list first, " +
                 "so if indicator is the first child in hierarchy, actually it is the second child in entity linked group")]
        public int selectedIndicatorIndex = 2;
        [FormerlySerializedAs("initSelectableTeam")] public FactionTag initSelectableFaction = FactionTag.Light;

        class Baker : Baker<UnitSelectionSystemAuthoring>
        {
            public override void Bake(UnitSelectionSystemAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.Dynamic);
                AddComponent(entity, new UnitSelectionData
                {
                    CurrentSelectCount = 0,
                    CurrentSelectFaction = authoring.initSelectableFaction,
                    IsDragSelecting = false
                });
                AddComponent(entity, new UnitSelectionConfig
                {
                    DragMinDistanceSq = authoring.dragMinDistance * authoring.dragMinDistance,
                });
            }
        }
    }

    public struct UnitSelectionConfig : IComponentData
    {
        public float DragMinDistanceSq;
    }

  
 




}


