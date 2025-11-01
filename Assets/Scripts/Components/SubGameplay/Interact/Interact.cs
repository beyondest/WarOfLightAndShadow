using System;
using Unity.Entities;
namespace SparFlame.Components.SubGameplay
{
    public enum InteractState
    {
        Idle = 0,
        Attacking = 1,
        Moving = 2,
        Garrison = 3,
        Harvesting =4,
        Healing = 5,
        CastSkill = 6
    }
    public struct BasicStateData : IComponentData
    {
        public Entity TargetEntity;
        public int InteractCounter;
        public InteractState CurState;
        public InteractState TargetState;
        public bool Focus;
    }
    
     public enum InteractType
    {
        Attack=0,
        Heal =1,
        Harvest = 2,
    }
    
    public interface IInteractAbility
    {
        int Amount { get; set; }
        float Range { get; set; }
        float Speed { get; set; }
        int Targets { get; set; }
        // This is rangeSq for real, remaining range for better show
        InteractType InteractType { get; set; }
    }

    public struct InteractAbilityBonus : IComponentData
    {
        public float SpeedBonus;
        public float RangeBonus;
        public int AmountBonus;
        public int TargetsBonus;
        public float MoveSpeedBonus;
    }
 

    [Serializable]
    public struct AttackAbility : IComponentData,IInteractAbility
    {
        public float Speed { get; set; }
        public int Targets { get; set; }
        public float Range { get; set; }
        public int Amount { get; set; }
        public InteractType InteractType { get; set; }
    }
    
    [Serializable]
    public struct HealAbility : IComponentData,IInteractAbility
    {
        public float Speed { get; set; }
        public int Targets { get; set; }
        public float Range { get; set; }
        public int Amount { get; set; }
        public InteractType InteractType { get; set; }
    }
    
    [Serializable]
    public struct HarvestAbility : IComponentData,IInteractAbility
    {
        public float Speed { get; set; }
        public int Targets { get; set; }
        public float Range { get; set; }
        public int Amount { get; set; }
        public InteractType InteractType { get; set; }
    }


    

    public struct AttackStateTag : IComponentData, IEnableableComponent
    {
        
    }


    public struct HarvestStateTag : IComponentData, IEnableableComponent
    {
    }



    public struct HealStateTag : IComponentData, IEnableableComponent
    {
    }



    public struct GarrisonStateTag : IComponentData, IEnableableComponent
    {
    }
    
    public struct IdleStateTag : IComponentData, IEnableableComponent
    {
        
    }

    public struct CastSkillStateTag : IComponentData, IEnableableComponent
    {
        
    }




}