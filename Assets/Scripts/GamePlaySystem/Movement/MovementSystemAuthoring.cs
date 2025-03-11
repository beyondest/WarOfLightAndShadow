using Unity.Entities;
using UnityEngine;

namespace SparFlame.GamePlaySystem.Movement
{
    public class MovementSystemAuthoring : MonoBehaviour
    {
        [Tooltip("This is the distance to judge if agent can move to next waypoint when the current waypoint is close enough")]
        public float waypointDistanceThreshold = 0.5f;
        private class MovementSystemAuthoringBaker : Baker<MovementSystemAuthoring>
        {
            
            public override void Bake(MovementSystemAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.None);
                AddComponent(entity, new MovementConfig
                {
                    WayPointDistanceSq = authoring.waypointDistanceThreshold * authoring.waypointDistanceThreshold,
                });
            }
        }
    }

    public struct MovementConfig : IComponentData
    {
        public float WayPointDistanceSq;
    }

    
}