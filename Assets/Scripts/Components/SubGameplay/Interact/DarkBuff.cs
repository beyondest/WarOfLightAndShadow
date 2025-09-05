using Unity.Entities;

namespace SparFlame.Components.SubGameplay
{
    public struct DarkShieldTauntBuff : IComponentData
    {
    }


    public struct DarkShieldTauntedBuff : IComponentData,IEnableableComponent
    {
        public float TauntTime;
        public Entity TauntedBy;
    }
    
    public struct DarkMagicDamageBuff : IComponentData, IEnableableComponent
    {
        public float HealingReductionPercent;
        public float LastTime;
    }
     
    public struct DarkClericBuff : IComponentData,IEnableableComponent
    {
        public float AttackAmountBonusScale;
        public float LastTime;
    }
    public struct DarkCavalryBuff : IComponentData
    {
    }
    public struct DarkArcherBuff : IComponentData
    {
    } 
}