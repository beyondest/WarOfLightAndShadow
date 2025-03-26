using SparFlame.GamePlaySystem.Movement;
using Unity.Entities;
using UnityEngine;
using UnityEngine.Serialization;

namespace SparFlame.GamePlaySystem.State
{
    public class MovingStateMachineAuthoring : MonoBehaviour
    {
        [Tooltip("When compromise times > this, will try solve stuck; If compromise times > 2*this, will detect surrounding enemy")]
        public int maxAllowedCompromiseTimesForStuck = 20;



        private class MovingStateMachineAuthoringBaker : Baker<MovingStateMachineAuthoring>
        {
            public override void Bake(MovingStateMachineAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.None);
                AddComponent(entity, new MovingStateMachineConfig
                {
                    MaxAllowedCompromiseTimesForStuck = authoring.maxAllowedCompromiseTimesForStuck,
                });
            }
        }
    }

    public struct MovingStateMachineConfig : IComponentData
    {
        public int MaxAllowedCompromiseTimesForStuck;

    }
    
}