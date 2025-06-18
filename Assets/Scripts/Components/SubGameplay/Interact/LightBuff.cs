using Unity.Entities;

namespace SparFlame.Components.SubGameplay
{
        
    public struct LightMagicDamageBuff : IComponentData,IEnableableComponent
    {
        public float SpeedNegativeBonus;
        public float MoveSpeedNegativeBonusScale;
        public float LastTime;
    }
    
    public struct LightClericBuff : IComponentData
    {
    }
   
    public struct LightCavalryBuff : IComponentData
    {
    }
    public struct LightCavalryUnderBonus : IComponentData,IEnableableComponent
    {
        public Entity Provider;
        public float LastTime;
    }
    
    public struct LightArcherBuff : IComponentData
    {

    }

    public struct LightShieldBuff : IComponentData
    {

    }


    public struct LightShieldUnderDefend : IComponentData,IEnableableComponent
    {
        public Entity DefendBy;
        // Tier3 shield will make ally not hurt from aoe, until itself dead
        public float DefendTime;
    }

}