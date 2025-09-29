using Sirenix.OdinInspector;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics.Authoring;
using UnityEngine;
using UnityEngine.Serialization;

namespace SparFlame.Systems.SubGameplay.Movement
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
        
        [InfoBox("This is the extent float for the march movement, considering march movement as the target position is void")]
        public float3 playerMarchExtent =new(1f,1f,1f);
        public float3 aiMarchExtent = new(10f, 1f, 10f);
        
        public float rotationSpeed = 5f;
        
        
        public PhysicsCategoryTags obstacleLayerMask;
        public PhysicsCategoryTags movementRayBelongsToLayerMask;

        [Tooltip("This is used for judging if get stuck")]
        public float recordPosInterval = 1.0f;

        public float detectLengthRatio = 0.1f;
        public float detectFrontBiasRatio = 0.6f;
      
        [Header("CBR Algorithm")]
        public float separationWeight = 0.2f;
        public float cohesionWeight = 0.0f;
        public float alignmentWeight = 0.0f;
        public float seekTargetWeight = 0.8f;
        
        public float maxAcceleration = 3f;
        
        private class MovementSystemAuthoringBaker : Baker<MovementSystemAuthoring>
        {
            public override void Bake(MovementSystemAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.None);
                AddComponent(entity, new MovementConfig
                {
                    WayPointDistanceSq = authoring.waypointDistanceThreshold * authoring.waypointDistanceThreshold,
                    PlayerMarchExtent = authoring.playerMarchExtent,
                    AIMarchExtent = authoring.aiMarchExtent,
                    InteractRangeSqBias = authoring.interactRangeSqBias,
                    ObstacleLayerMask = authoring.obstacleLayerMask.Value,
                    DetectRaycastBelongsTo =authoring.movementRayBelongsToLayerMask.Value,
                    RecordPosInterval = authoring.recordPosInterval,
                    DetectLengthRatio = authoring.detectLengthRatio,
                    DetectFrontBiasRatio = authoring.detectFrontBiasRatio,
                    
                   
                });
                AddComponent(entity,new CbrConfig
                {
                    AlignmentWeight = authoring.alignmentWeight,
                    CohesionWeight = authoring.cohesionWeight,
                    SeparationWeight = authoring.separationWeight,
                    SeekTargetWeight = authoring.seekTargetWeight,
                    RotationSpeed = authoring.rotationSpeed,
                    MaxAcceleration = authoring.maxAcceleration,
                });
            }
        }
    }

    public struct MovementConfig : IComponentData
    {
        public float WayPointDistanceSq;
        public float3 PlayerMarchExtent;
        public float3 AIMarchExtent;
        public float InteractRangeSqBias;
        public uint ObstacleLayerMask;
        public uint DetectRaycastBelongsTo;
        public float RecordPosInterval;
        public float DetectLengthRatio;
        public float DetectFrontBiasRatio;
        
        // CBR Algorithm
    
    }

    public struct CbrConfig : IComponentData
    {
        public float SeparationWeight;
        public float CohesionWeight;
        public float AlignmentWeight;
        public float SeekTargetWeight;
        public float RotationSpeed;
        
        public float MaxAcceleration;
    }

    
    
}