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
    


}