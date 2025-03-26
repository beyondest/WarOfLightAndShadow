using Unity.Entities;
using Unity.Physics.Authoring;
using UnityEngine;
using UnityEngine.Serialization;

namespace SparFlame.GamePlaySystem.Movement
{
    public class MovementSystemAuthoring : MonoBehaviour
    {
        [Tooltip("This is the distance to judge if agent arrives at the waypoint;" +
                 "This is the distance to judge if agent not get stuck")]
        public float waypointDistanceThreshold = 0.5f;

        [Tooltip("This bias will affect the interact movement complete judgement, " +
                 "only when (disSqPointToRect < Range - Bias) will be judged as complete." +
                 "If this value is too large, then any target is not reachable" +
                 "If too small, then any target may not be in interact range even movement complete")]
        public float interactRangeSqBias = 0.3f;
        
        [Tooltip("This is the extent float for the march movement, considering march movement as the target position is void")]
        public float marchExtent = 0.5f;
        public float rotationSpeed = 5f;
        
        
        public PhysicsCategoryTags obstacleLayerMask;
        public PhysicsCategoryTags movementRayBelongsToLayerMask;

        [Tooltip("This is used for judging if get stuck")]
        public float recordPosInterval = 1.0f;

        public float detectLengthRatio = 0.1f;
        public float detectFrontBiasRatio = 0.6f;
        private class MovementSystemAuthoringBaker : Baker<MovementSystemAuthoring>
        {
            public override void Bake(MovementSystemAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.None);
                AddComponent(entity, new MovementConfig
                {
                    WayPointDistanceSq = authoring.waypointDistanceThreshold * authoring.waypointDistanceThreshold,
                    MarchExtent = authoring.marchExtent,
                    InteractRangeSqBias = authoring.interactRangeSqBias,
                    RotationSpeed = authoring.rotationSpeed,
                    ObstacleLayerMask = authoring.obstacleLayerMask.Value,
                    DetectRaycastBelongsTo =authoring.movementRayBelongsToLayerMask.Value,
                    RecordPosInterval = authoring.recordPosInterval,
                    DetectLengthRatio = authoring.detectLengthRatio,
                    DetectFrontBiasRatio = authoring.detectFrontBiasRatio
                });
            }
        }
    }

    public struct MovementConfig : IComponentData
    {
        public float WayPointDistanceSq;
        public float MarchExtent;
        public float InteractRangeSqBias;
        public float RotationSpeed;
        public uint ObstacleLayerMask;
        public uint DetectRaycastBelongsTo;
        public float RecordPosInterval;
        public float DetectLengthRatio;
        public float DetectFrontBiasRatio;
    }


    
    
}