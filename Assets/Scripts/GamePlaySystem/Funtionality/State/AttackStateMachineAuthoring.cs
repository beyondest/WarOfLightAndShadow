using Unity.Entities;
using UnityEngine;

namespace SparFlame.GamePlaySystem.State
{
    public class AttackStateMachineAuthoring : MonoBehaviour
    {
        private class AttackStateMachineAuthoringBaker : Baker<AttackStateMachineAuthoring>
        {
            public override void Bake(AttackStateMachineAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.None);
                AddComponent(entity, new AttackStateMachineConfig
                {
                    
                });
            }
        }
    }

    public struct AttackStateMachineConfig : IComponentData
    {
        
    }
}