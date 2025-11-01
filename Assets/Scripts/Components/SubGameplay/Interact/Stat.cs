using System;
using Unity.Entities;

namespace SparFlame.Components.SubGameplay
{
  
    [Serializable]
    public struct StatData : IComponentData
    {
        public float curValue;
        public int maxValue;
    }
        
    /// <summary>
    /// This request is handled by stat system
    /// AbsAmount is always positive
    /// If Upgrade, kill by unnormal must be true
    /// </summary>
    public struct StatChangeRequest : IComponentData
    {
        public Entity Interactor;
        public Entity Interactee;
        public int AbsAmount;
        public StatChangeType Type;
        public SubGameplayGeneralAttr InteractorSubGameplayGeneralAttr;
        public DamageType DamageType;
    }
    
    public enum StatChangeType
    {
        None = 0,
        Attack = 1,
        Heal = 2,
        Harvest = 3,
        UnNormalKill = 4,
        SimpleCleanUsedAsUpgrade = 5
    }

    public enum DamageType
    {
        None = 0,
        Physical = 1,
        Magic = 2,
        BuffDamage = 3,
    }
    public struct UnitDeadTag : IComponentData
    {
    }
    
    // Every armyGroup and city has one
    [Serializable]
    public struct HpRegenerateTimer : IComponentData
    {
        public float lastCheckTotalHours;
    }
}