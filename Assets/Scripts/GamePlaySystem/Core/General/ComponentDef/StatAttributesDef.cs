using SparFlame.GamePlaySystem.General;
using Unity.Entities;

namespace SparFlame.GamePlaySystem.Interact
{
  
    public struct StatData : IComponentData
    {
        public int MaxValue;
        public float CurValue;
        public int Bonus;
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
        public GeneralAttr InteractorGeneralAttr;
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
 
}