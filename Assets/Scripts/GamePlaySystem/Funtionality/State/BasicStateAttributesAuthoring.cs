﻿using Unity.Entities;
using UnityEngine;

namespace SparFlame.GamePlaySystem.State
{
    public class BasicStateAttributesAuthoring : MonoBehaviour
    {
        private class StateAttributesAuthoringBaker : Baker<BasicStateAttributesAuthoring>
        {
            public override void Bake(BasicStateAttributesAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.Dynamic);
                AddComponent(entity, new BasicStateData
                {
                    CurState = UnitState.Idle,
                    Focus = false,
                    TargetEntity = Entity.Null,
                    TargetState = UnitState.Idle,
                    InteractCounter = 0
                });
                AddComponent<IdleStateTag>(entity);
                SetComponentEnabled<IdleStateTag>(entity,true);
             
            }
        }
    }
    
    public struct BasicStateData : IComponentData
    {
        public UnitState CurState;
        public bool Focus;
        public Entity TargetEntity;
        public UnitState TargetState;
        public int InteractCounter;
    }
    
    public struct IdleStateTag : IComponentData, IEnableableComponent
    {
        
    }


    
    
}