using Unity.Entities;
using UnityEngine;

namespace SparFlame.Systems.SubGameplay.StateMachine
{
    public class MovingStateMachineAuthoring : MonoBehaviour
    {
        [Tooltip("When compromise times > this, will try solve stuck; If compromise times > 2*this, will detect surrounding enemy")]
        public int maxAllowedCompromiseTimesForStuck = 20;

        [Tooltip("This dis must be bigger than sight")]
        public float maxDistanceFollowForAITag = 20;
        public float maxDistanceUnitToBuildingForGarrison = 20;
        
        
        private class MovingStateMachineAuthoringBaker : Baker<MovingStateMachineAuthoring>
        {
            public override void Bake(MovingStateMachineAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.None);
                AddComponent(entity, new MovingStateMachineConfig
                {
                    MaxAllowedCompromiseTimesForStuck = authoring.maxAllowedCompromiseTimesForStuck,
                    MaxDistanceSqFollowForAITag = authoring.maxDistanceFollowForAITag * authoring.maxDistanceFollowForAITag,
                    MaxDisSqUnitToBuildingForGarrison =  authoring.maxDistanceUnitToBuildingForGarrison * authoring.maxDistanceUnitToBuildingForGarrison,
                });
            }
        }
    }

    public struct MovingStateMachineConfig : IComponentData
    {
        public int MaxAllowedCompromiseTimesForStuck;
        public float MaxDistanceSqFollowForAITag;
        public float MaxDisSqUnitToBuildingForGarrison;
    }
    
}