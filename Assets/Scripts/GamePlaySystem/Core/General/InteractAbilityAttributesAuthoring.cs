using System;
using Unity.Entities;
using UnityEngine;

namespace SparFlame.GamePlaySystem.General
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
                            RangeSq = authoring.interactRange * authoring.interactRange,
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
                            RangeSq = authoring.interactRange * authoring.interactRange,
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
                            RangeSq = authoring.interactRange * authoring.interactRange,
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
        Attack=0,
        Heal =1,
        Harvest = 2,
    }
    
    public interface IInteractAbility
    {
        int Amount { get; set; }
        float RangeSq { get; set; }
        float Speed { get; set; }
        float Targets { get; set; }
        // This is rangeSq for real, remaining range for better show
        InteractType InteractType { get; set; }
    }

    public struct AttackAbility : IComponentData,IInteractAbility
    {
        public float Speed { get; set; }
        public float Targets { get; set; }
        public float RangeSq { get; set; }
        public int Amount { get; set; }
        public InteractType InteractType { get; set; }
    }
    
    
    public struct HealAbility : IComponentData,IInteractAbility
    {
        public float Speed { get; set; }
        public float Targets { get; set; }
        public float RangeSq { get; set; }
        public int Amount { get; set; }
        public InteractType InteractType { get; set; }
    }
    public struct HarvestAbility : IComponentData,IInteractAbility
    {
        public float Speed { get; set; }
        public float Targets { get; set; }
        public float RangeSq { get; set; }
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
}