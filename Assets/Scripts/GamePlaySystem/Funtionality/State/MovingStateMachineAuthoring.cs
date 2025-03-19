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

        [Tooltip("If Choose same side times > this, will choose another side")]
        public int chooseSideTimes = 30;

        [Tooltip("If squeeze failed times > this, will choose another side to move")]
        public int maxAllowedCompromiseTimesForSqueeze = 20;
        
        [Tooltip("will squeeze front ally unit squeezeRatio * max of front collider shapeXz")]
        public float squeezeRatio = 0.2f;
        private class MovingStateMachineAuthoringBaker : Baker<MovingStateMachineAuthoring>
        {
            public override void Bake(MovingStateMachineAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.None);
                AddComponent(entity, new MovingStateMachineConfig
                {
                    MaxAllowedCompromiseTimes = authoring.maxAllowedCompromiseTimesForStuck,
                    ChooseSideTimes = authoring.chooseSideTimes,
                    MaxAllowedCompromiseTimesForSqueeze = authoring.maxAllowedCompromiseTimesForSqueeze,
                    SqueezeRatio = authoring.squeezeRatio
                });
            }
        }
    }

    public struct MovingStateMachineConfig : IComponentData
    {
        public int MaxAllowedCompromiseTimes;
        public int ChooseSideTimes;
        public int MaxAllowedCompromiseTimesForSqueeze;
        public float SqueezeRatio;
    }
    
}