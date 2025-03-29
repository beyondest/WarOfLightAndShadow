using System;
using Unity.Entities;
using UnityEngine;

namespace SparFlame.GamePlaySystem.Interact
{
    public class InteractAbilityAttributesAuthoring : MonoBehaviour
    {
        public InteractType interactType;

        [Tooltip("This is damage dealt times/seconds")]
        public float interactSpeed = 1;

        [Tooltip("How many targets it can attack at one time")]
        public int interactCount = 1;

        // TODO : Check if this is not movable game object, then it interact range should be bigger than sight range
        public float interactRange = 1f;

        public int interactBasicAmount = 10;

        private class Baker : Baker<InteractAbilityAttributesAuthoring>
        {
            public override void Bake(InteractAbilityAttributesAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.Dynamic);


                switch (authoring.interactType)
                {
                    case InteractType.Attack:
                        AddComponent<AttackStateTag>(entity);
                        SetComponentEnabled<AttackStateTag>(entity, false);
                        AddComponent(entity, new AttackAbility
                        {
                            Speed = authoring.interactSpeed,
                            Targets = authoring.interactCount,
                            Range = authoring.interactRange * authoring.interactRange,
                            Amount = authoring.interactBasicAmount,
                            InteractType = InteractType.Attack
                        });
                        break;
                    case InteractType.Heal:
                        AddComponent<HealStateTag>(entity);
                        SetComponentEnabled<HealStateTag>(entity, false);
                        AddComponent(entity, new HealAbility
                        {
                            Speed = authoring.interactSpeed,
                            Targets = authoring.interactCount,
                            Range = authoring.interactRange * authoring.interactRange,
                            Amount = authoring.interactBasicAmount,
                            InteractType = InteractType.Heal
                        });
                        break;
                    case InteractType.Harvest:
                        AddComponent<HarvestStateTag>(entity);
                        SetComponentEnabled<HarvestStateTag>(entity, false);
                        AddComponent(entity, new HarvestAbility
                        {
                            Speed = authoring.interactSpeed,
                            Targets = authoring.interactCount,
                            Range = authoring.interactRange * authoring.interactRange,
                            Amount = authoring.interactBasicAmount,
                            InteractType = InteractType.Harvest
                        });
                        break;
                    
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
        }
    }
    
    public enum InteractType
    {
        Attack,
        Heal,
        Harvest,
    }
    
    public interface IInteractAbility
    {
        int Amount { get; set; }
        float Range { get; set; }
        float Speed { get; set; }
        float Targets { get; set; }
        // This is rangeSq for real, remaining range for better show
        int CurCounter { get; set; }
        InteractType InteractType { get; set; }
    }

    public struct AttackAbility : IComponentData,IInteractAbility
    {
        public float Speed { get; set; }
        public float Targets { get; set; }
        public float Range { get; set; }
        public int Amount { get; set; }
        public int CurCounter { get; set; }
        public InteractType InteractType { get; set; }
    }
    
    
    public struct HealAbility : IComponentData,IInteractAbility
    {
        public float Speed { get; set; }
        public float Targets { get; set; }
        public float Range { get; set; }
        public int Amount { get; set; }
        public int CurCounter { get; set; }
        public InteractType InteractType { get; set; }
    }
    public struct HarvestAbility : IComponentData,IInteractAbility
    {
        public float Speed { get; set; }
        public float Targets { get; set; }
        public float Range { get; set; }
        public int Amount { get; set; }
        public int CurCounter { get; set; }
        public InteractType InteractType { get; set; }
    }



    

    public struct AttackStateTag : IComponentData, IEnableableComponent
    {
        
    }

    // public struct AttackAbility : IComponentData
    // {
    //     public float Speed;
    //     public float Count;
    //     public float RangeSq;
    //     public int BasicAmount;
    //     public int CurCounter;
    // }

    public struct HarvestStateTag : IComponentData, IEnableableComponent
    {
    }

    // public struct HarvestAbility : IComponentData
    // {
    //     public float RangeSq;
    //     public float BasicAmount;
    //     public float Speed;
    //     public int Count;
    // }

    public struct HealStateTag : IComponentData, IEnableableComponent
    {
    }

    // public struct HealingAbility : IComponentData
    // {
    //     public float RangeSq;
    //     public float BasicAmount;
    //     public float Speed;
    //     public int Count;
    // }

    public struct GarrisonStateTag : IComponentData, IEnableableComponent
    {
    }
}