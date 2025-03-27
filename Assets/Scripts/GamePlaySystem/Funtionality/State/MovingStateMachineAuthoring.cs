using SparFlame.GamePlaySystem.Movement;
using Unity.Entities;
using UnityEngine;
using UnityEngine.Serialization;

namespace SparFlame.GamePlaySystem.State
{
    public class MovingStateMachineAuthoring : MonoBehaviour
    {
        [Tooltip("This times is increased when a unit always fails to go front. This is used for circle detection")]
        public int maxAllowedCompromiseTimes = 3;
        private class MovingStateMachineAuthoringBaker : Baker<MovingStateMachineAuthoring>
        {
            public override void Bake(MovingStateMachineAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.None);
                AddComponent(entity, new MovingStateMachineConfig
                {
                    MaxAllowedCompromiseTimes = authoring.maxAllowedCompromiseTimes
                });
            }
        }
    }

    public struct MovingStateMachineConfig : IComponentData
    {
        public int MaxAllowedCompromiseTimes;
    }
    
}