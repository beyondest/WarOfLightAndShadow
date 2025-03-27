using Unity.Entities;
using UnityEngine;

namespace SparFlame.GamePlaySystem.State
{
    public class UnitStateAttributesAuthoring : MonoBehaviour
    {
        private class StateAttributesAuthoringBaker : Baker<UnitStateAttributesAuthoring>
        {
            public override void Bake(UnitStateAttributesAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.Dynamic);
                AddComponent(entity, new UnitBasicStateData
                {
                    CurState = UnitState.Idle,
                    Focus = false,
                    TargetEntity = Entity.Null,
                    TargetState = UnitState.Idle
                });
                AddComponent<IdleStateTag>(entity);
                SetComponentEnabled<IdleStateTag>(entity,true);
            }
        }
    }
    
    public struct UnitBasicStateData : IComponentData
    {
        public UnitState CurState;
        public bool Focus;
        public Entity TargetEntity;
        public UnitState TargetState;
    }
    
    public struct IdleStateTag : IComponentData, IEnableableComponent
    {
        
    }
    
    
}