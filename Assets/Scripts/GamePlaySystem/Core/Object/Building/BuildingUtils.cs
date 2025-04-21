
namespace SparFlame.GamePlaySystem.Building
{
    public struct BuildingUtils
    {
        public static BuildingState GetBuildingState(bool underAttack, bool constructing, bool conjuring,
            bool generating)
        {
            if (underAttack) return BuildingState.UnderAttack;
            if(constructing) return BuildingState.Constructing;
            if(conjuring || generating) return BuildingState.Working;
            return BuildingState.Idle;
        }
    }
}