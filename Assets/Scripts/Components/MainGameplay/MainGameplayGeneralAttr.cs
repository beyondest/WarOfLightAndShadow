using System;
using SparFlame.Components.General;
using Unity.Entities;

namespace SparFlame.Components.MainGameplay
{
    public enum MainGameBaseTag
    {
        City = 0,
        ArmyGroup = 1,
        
    }
    
    [Serializable]
    public struct MainGameplayGeneralAttr : IComponentData
    {
        public FactionTag faction;
        public SubFactionTag subFaction;
        public MainGameBaseTag baseTag;
    }
}