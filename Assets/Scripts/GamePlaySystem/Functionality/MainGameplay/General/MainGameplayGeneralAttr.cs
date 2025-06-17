using SparFlame.GamePlaySystem.General;
using Unity.Entities;

namespace GamePlaySystem.Functionality.MainGameplay.General
{
    public enum MainGameBaseTag
    {
        City = 0,
        Army = 1,
        
    }
    public struct MainGameplayGeneralAttr : IComponentData
    {
        public FactionTag Faction;
        public MainGameBaseTag BaseTag;
    }
}