using SparFlame.GamePlaySystem.Units;
using Unity.Entities;

namespace SparFlame.GamePlaySystem.Interact
{
    // TODO These datas are in wrong assembly, try restructure your project
    
    public struct RemoveFromTeamRequest : IComponentData
    {
        public Entity UnitToRemove;
        public UnitAttr UnitAttr; // For check if this is special unit
        public Entity BelongsToTeam;
    }
    
    // When enemy base is destroyed, building pack of that base level is destroyed;
    // If all base level is destroyed, then destroy the building pack;
    // If all building pack is destroyed, then next wave point can start
    public struct DestroyEnemyBaseRequest : IComponentData
    {
        public Entity Base;
    }

}