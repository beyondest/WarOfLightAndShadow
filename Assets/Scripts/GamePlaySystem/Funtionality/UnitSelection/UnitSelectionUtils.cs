using SparFlame.GamePlaySystem.General;
using Unity.Entities;

namespace SparFlame.GamePlaySystem.UnitSelection
{
    public struct UnitSelectionUtils
    {
        public static bool IsSelectable(EntityManager entityManager, in UnitSelectionData data, Entity entity)
        {
            if (entity == Entity.Null) return false;

            if (!entityManager.HasComponent<InteractableAttr>(entity))
                return false;
            var attr = entityManager.GetComponentData<InteractableAttr>(entity);
            if(attr.FactionTag != data.CurrentSelectFaction)return false;
            return true;
        }
    }
}