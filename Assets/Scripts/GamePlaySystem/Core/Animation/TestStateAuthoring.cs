
using SparFlame.GamePlaySystem.State;
using Unity.Entities;
using UnityEngine;

namespace SparFlame.GamePlaySystem.Animation
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