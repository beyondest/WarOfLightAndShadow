using System;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
namespace SparFlame.GamePlaySystem.State
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
                var boxCollider = authoring.GetComponent<BoxCollider>();
                AddComponent(entity, new InteractBasicData
                {
                    BoxColliderSize = boxCollider.size,
                });
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
                        });
                        break;
                    case InteractType.Heal:
                        AddComponent<HealStateTag>(entity);
                        SetComponentEnabled<HealStateTag>(entity, false);
                        AddComponent(entity, new HealingAbility
                        {
                            Speed = authoring.interactSpeed,
                            Count = authoring.interactCount,
                            RangeSq = authoring.interactRange * authoring.interactRange,
                            BasicAmount = authoring.interactBasicAmount, 
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
                        });
                        break;
                    case InteractType.Garrison:
                        AddComponent<GarrisonStateTag>(entity);
                        SetComponentEnabled<GarrisonStateTag>(entity, false);
                        break;
                    
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
        }
    }
    

    
    
 
    
    public struct AttackAbility : IComponentData
    {
        public float Speed;
        public int Count;
        public float RangeSq;
        public int BasicAmount;
        public int CurCounter;
    }
    
    
    public struct AttackStateTag : IComponentData,IEnableableComponent
    {
        
    }

    // public struct AttackAbility : IComponentData
    // {
    //     public float AttackSpeed;
    //     public int AttackCount;
    //     public float AttackRangeSq;
    //     public int AttackBasicAmount;
    // }
    
    public struct HarvestStateTag : IComponentData, IEnableableComponent
    {
        
    }
    public struct HarvestAbility : IComponentData
    {
        public float RangeSq;
        public float BasicAmount;
        public float Speed ;
        public int Count ;
    }
    
    public struct HealStateTag : IComponentData,IEnableableComponent
    {
        
    }
    
    public struct HealingAbility : IComponentData
    {
        public float RangeSq ;
        public float BasicAmount ;
        public float Speed ;
        public int Count ;
    }

    public struct GarrisonStateTag : IComponentData, IEnableableComponent
    {
        
    }

    public struct InteractBasicData : IComponentData
    {
        public float3 BoxColliderSize;
    }


    
}