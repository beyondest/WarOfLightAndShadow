using SparFlame.Components.General;
using SparFlame.Systems.SubGameplay.StateMachine;
using Unity.Entities;
using UnityEngine;

namespace SparFlame.Systems.General.Animation
{
    public class TestStateAuthoring : MonoBehaviour
    {
        
        private class TestStateBaker : Baker<TestStateAuthoring>
        {
            public override void Bake(TestStateAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.None);
                AddComponent(entity, new ChangeStateRequest
                {
                    TargetState = UnitAnimationState.Idle
                });
            }
        }
        
    }

    
}