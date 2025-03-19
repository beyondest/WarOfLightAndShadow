using System;
using Unity.Entities;
using UnityEngine;
using UnityEngine.Serialization;

namespace SparFlame.GamePlaySystem.Interact
{
    public class AttackAttributesAuthoring : MonoBehaviour
    {
        public InteractType interactType;
        
        [Tooltip("This is damage dealt times/seconds")]
        public float interactSpeed = 1;

        [Tooltip("How many targets it can attack at one time")]
        public int interactCount = 1;

        public float interactRange = 1f;
        
        public int interactBasicAmount = 10;
        private class AttackAttributesAuthoringBaker : Baker<AttackAttributesAuthoring>
        {
            public override void Bake(AttackAttributesAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.Dynamic);
                
                switch (authoring.interactType)
                {
                    case InteractType.Attack:
                        AddComponent<AttackStateTag>(entity);
                        SetComponentEnabled<AttackStateTag>(entity, false);
                        AddComponent(entity, new AttackAbility
                        {
                            AttackSpeed = authoring.interactSpeed,
                            AttackCount = authoring.interactCount,
                            AttackRangeSq = authoring.interactRange * authoring.interactRange,
                            AttackBasicAmount = authoring.interactBasicAmount,
                        });
                        break;
                    case InteractType.Heal:
                        AddComponent<HealStateTag>(entity);
                        SetComponentEnabled<HealStateTag>(entity, false);
                        AddComponent(entity, new HealingAbility
                        {
                            HealingSpeed = authoring.interactSpeed,
                            HealingCount = authoring.interactCount,
                            HealingRangeSq = authoring.interactRange * authoring.interactRange,
                            HealingBasicAmount = authoring.interactBasicAmount, 
                        });
                        break;
                    case InteractType.Harvest:
                        AddComponent<HarvestStateTag>(entity);
                        SetComponentEnabled<HarvestStateTag>(entity, false);
                        AddComponent(entity, new HarvestAbility
                        {
                            HarvestSpeed = authoring.interactSpeed,
                            HarvestCount = authoring.interactCount,
                            HarvestRangeSq = authoring.interactRange * authoring.interactRange,
                            HarvestBasocAmount = authoring.interactBasicAmount,
                        });
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
        }
    }
    public struct AttackStateTag : IComponentData,IEnableableComponent
    {
        
    }

    public struct AttackAbility : IComponentData
    {
        public float AttackSpeed;
        public int AttackCount;
        public float AttackRangeSq;
        public int AttackBasicAmount;
    }
    
    public struct HarvestStateTag : IComponentData, IEnableableComponent
    {
        
    }
    public struct HarvestAbility : IComponentData
    {
        public float HarvestRangeSq;
        public float HarvestBasocAmount;
        public float HarvestSpeed ;
        public int HarvestCount ;
    }
    
    public struct HealStateTag : IComponentData,IEnableableComponent
    {
        
    }
    
    public struct HealingAbility : IComponentData
    {
        public float HealingRangeSq ;
        public float HealingBasicAmount ;
        public float HealingSpeed ;
        public int HealingCount ;
    }
    


    
}