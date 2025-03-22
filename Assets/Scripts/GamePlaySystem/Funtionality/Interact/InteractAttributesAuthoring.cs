using System;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace SparFlame.GamePlaySystem.Interact
{
    public class InteractAttributesAuthoring : MonoBehaviour
    {
        public InteractType interactType;

        [Tooltip("This is damage dealt times/seconds")]
        public float interactSpeed = 1;

        [Tooltip("How many targets it can attack at one time")]
        public int interactCount = 1;

        public float interactRange = 1f;

        public int interactBasicAmount = 10;

        private class AttackAttributesAuthoringBaker : Baker<InteractAttributesAuthoring>
        {
            public override void Bake(InteractAttributesAuthoring authoring)
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
                            Count = authoring.interactCount,
                            RangeSq = authoring.interactRange * authoring.interactRange,
                            BasicAmount = authoring.interactBasicAmount,
                            InteractType = InteractType.Attack
                        });
                        break;
                    case InteractType.Heal:
                        AddComponent<HealStateTag>(entity);
                        SetComponentEnabled<HealStateTag>(entity, false);
                        AddComponent(entity, new HealAbility
                        {
                            Speed = authoring.interactSpeed,
                            Count = authoring.interactCount,
                            RangeSq = authoring.interactRange * authoring.interactRange,
                            BasicAmount = authoring.interactBasicAmount,
                            InteractType = InteractType.Heal
                        });
                        break;
                    case InteractType.Harvest:
                        AddComponent<HarvestStateTag>(entity);
                        SetComponentEnabled<HarvestStateTag>(entity, false);
                        AddComponent(entity, new HarvestAbility
                        {
                            Speed = authoring.interactSpeed,
                            Count = authoring.interactCount,
                            RangeSq = authoring.interactRange * authoring.interactRange,
                            BasicAmount = authoring.interactBasicAmount,
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
        float Speed { get; set; }
        float Count { get; set; }
        float RangeSq { get; set; }
        int BasicAmount { get; set; }
        int CurCounter { get; set; }
        InteractType InteractType { get; set; }
    }

    public struct AttackAbility : IComponentData,IInteractAbility
    {
        public float Speed { get; set; }
        public float Count { get; set; }
        public float RangeSq { get; set; }
        public int BasicAmount { get; set; }
        public int CurCounter { get; set; }
        public InteractType InteractType { get; set; }
    }
    
    
    public struct HealAbility : IComponentData,IInteractAbility
    {
        public float Speed { get; set; }
        public float Count { get; set; }
        public float RangeSq { get; set; }
        public int BasicAmount { get; set; }
        public int CurCounter { get; set; }
        public InteractType InteractType { get; set; }
    }
    public struct HarvestAbility : IComponentData,IInteractAbility
    {
        public float Speed { get; set; }
        public float Count { get; set; }
        public float RangeSq { get; set; }
        public int BasicAmount { get; set; }
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