using Unity.Entities;

namespace SparFlame.Components.SubGameplay
{
    public struct DarkShieldTauntBuff : IComponentData
    {
        public float StopTime;
    }


    public struct DarkShieldTauntedBuff : IComponentData,IEnableableComponent
    {
        public Entity TauntedBy;
        public float TauntTime;
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
        public float StopTime;
    }
    public struct DarkArcherBuff : IComponentData
    {
    } 
}