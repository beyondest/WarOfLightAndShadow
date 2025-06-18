using SparFlame.Components.General;
using Unity.Entities;

namespace SparFlame.Components.MainGameplay
{
    public enum MainGameBaseTag
    {
        City = 0,
        Army = 1,
        
    }
    public struct MainGameplayGeneralAttr : IComponentData
    {
        public FactionTag Faction;
        public SubFaction SubFaction;
        public MainGameBaseTag BaseTag;
    }
}