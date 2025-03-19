using Unity.Entities;
using UnityEngine;
using UnityEngine.Serialization;

namespace SparFlame.GamePlaySystem.Movement
{
    public class MovementSystemAuthoring : MonoBehaviour
    {
        [Tooltip("This is the distance to judge if agent arrives at the waypoint")]
        public float waypointDistanceThreshold = 0.5f;
        
        [Tooltip("This is the extent float for the march movement, considering march movement as the target position is void")]
        public float marchExtent = 0.5f;
        public float rotationSpeed = 5f;
        
        [FormerlySerializedAs("clickableLayerMask")] public LayerMask obstacleLayerMask;
        public LayerMask movementRayBelongsToLayerMask;
        private class MovementSystemAuthoringBaker : Baker<MovementSystemAuthoring>
        {
            public override void Bake(MovementSystemAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.None);
                AddComponent(entity, new MovementConfig
                {
                    WayPointDistanceSq = authoring.waypointDistanceThreshold * authoring.waypointDistanceThreshold,
                    MarchExtent = authoring.marchExtent,
                    RotationSpeed = authoring.rotationSpeed,
                    ClickableLayerMask = (uint)authoring.obstacleLayerMask.value,
                    MovementRayBelongsToLayerMask =(uint) authoring.movementRayBelongsToLayerMask.value,
                });
            }
        }
    }

    public struct MovementConfig : IComponentData
    {
        public float WayPointDistanceSq;
        public float MarchExtent;
        public float RotationSpeed;
        public uint ClickableLayerMask;
        public uint MovementRayBelongsToLayerMask;
    }


    
    
}