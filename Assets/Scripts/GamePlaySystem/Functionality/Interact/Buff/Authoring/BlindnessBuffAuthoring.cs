using Unity.Entities;
using UnityEngine;

namespace SparFlame.GamePlaySystem.Interact.Blindness
{
    public class BlindnessBuffAuthoring : MonoBehaviour
    {
        public float lastDuration;
        private class BlindnessBuffAuthoringBaker : Baker<BlindnessBuffAuthoring>
        {
            public override void Bake(BlindnessBuffAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.Dynamic);
                AddComponent(entity, new BlindnessData
                {
                    
                });
                AddComponent(entity, new GeneralBuffData
                {
                    Duration = authoring.lastDuration,
                    StartTime = 0,
                    TrackTarget = Entity.Null
                });
                
            }
        }
    }

    public struct BlindnessData : IComponentData
    {
    }


    
}