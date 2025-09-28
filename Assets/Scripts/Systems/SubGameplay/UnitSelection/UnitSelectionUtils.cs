using SparFlame.Components.General;
using SparFlame.Components.SubGameplay;
using Unity.Entities;

namespace SparFlame.Systems.SubGameplay.UnitSelection
{
    public struct UnitSelectionUtils
    {
        /// <summary>
        /// This method only works for single click select.
        /// If drag select, use query to make it faster
        /// </summary>
        /// <param name="entityManager"></param>
        /// <param name="playerFactionData"></param>
        /// <param name="entity"></param>
        /// <returns></returns>
        public static bool IsSelectable(EntityManager entityManager,
            in PlayerFactionData playerFactionData, Entity entity)
        {
            if (entity == Entity.Null) return false;
            if (!entityManager.HasComponent<Selected>(entity)) return false;
            if (!entityManager.HasComponent<SubGameplayGeneralAttr>(entity))
                return false;
            var attr = entityManager.GetComponentData<SubGameplayGeneralAttr>(entity);
            var relationship = FactionUtils.GetRelationship(playerFactionData.faction,
                playerFactionData.subFaction, attr.Faction, attr.SubFaction);
            if (relationship != Relationship.Self && relationship != Relationship.Ally) return false;
            if (entityManager.HasComponent<InGarrison>(entity)) return false;
            return true;
        }
    }
}