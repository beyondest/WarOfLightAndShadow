using Sirenix.OdinInspector;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics.Authoring;
using UnityEngine;

namespace SparFlame.Systems.SubGameplay.Movement
{
    public class CbrAuthoring : MonoBehaviour
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
        
        public float rotationSpeed = 5f;
        
        
        public PhysicsCategoryTags obstacleLayerMask;
        public PhysicsCategoryTags movementRayBelongsToLayerMask;

        [Tooltip("This is used for judging if get stuck")]
        public float recordPosInterval = 1.0f;

        public float surroundingRayDetectLength = 0.5f;
        [Tooltip("position + detectDirection * boxColliderSize.x/z * this ratio will be the start point of the surrounding detect ray")]
        public float surroundingDetectRayStartBiasRatio = 0.6f;
      
        [Header("CBR Algorithm")]
        public float separationWeight = 0.2f;
        public float avoidanceWeight = 0.5f;
        public float cohesionWeight;
        public float alignmentWeight;
        public float seekTargetWeight = 0.8f;
        
        public float maxAcceleration = 3f;
        
        [Header("Avoidance config")]
        public float interactStateAddAvoidValue = 3f;
        
        [Header("Separation config")]
        public float separationValueWhenTotallyOverlapped = 1f;
        
        [Header("Raycast to terrain config")]
        public PhysicsCategoryTags terrainLayerMask;
        public PhysicsCategoryTags detectTerrainRaycastBelongsToLayerMask;
        public float downDistance = 2f;
        public float liftDistance = 1f;
        
        private class MovementSystemAuthoringBaker : Baker<CbrAuthoring>
        {
            public override void Bake(CbrAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.None);
                AddComponent(entity, new MovementConfig
                {
                    WayPointDistanceSq = authoring.waypointDistanceThreshold * authoring.waypointDistanceThreshold,
                    MarchExtent = authoring.playerMarchExtent,
                    InteractRangeSqBias = authoring.interactRangeSqBias,
                    ObstacleLayerMask = authoring.obstacleLayerMask.Value,
                    DetectRaycastBelongsTo =authoring.movementRayBelongsToLayerMask.Value,
                    RecordPosInterval = authoring.recordPosInterval,
                    SurroundingRayDetectLength = authoring.surroundingRayDetectLength,
                    SurroundingDetectRayStartBiasRatio = authoring.surroundingDetectRayStartBiasRatio,
                });
                
                AddComponent(entity, new GetGroundNormalConfig
                {
                    DownDistance = authoring.downDistance,
                    TerrainLayerMask = authoring.terrainLayerMask.Value,
                    DetectTerrainRaycastBelongsTo = authoring.detectTerrainRaycastBelongsToLayerMask.Value,
                    LiftDistance = authoring.liftDistance,
                });
                AddComponent(entity,new CbrConfig
                {
                    AlignmentWeight = authoring.alignmentWeight,
                    AvoidanceWeight = authoring.avoidanceWeight,
                    CohesionWeight = authoring.cohesionWeight,
                    SeparationWeight = authoring.separationWeight,
                    SeekTargetWeight = authoring.seekTargetWeight,
                    RotationSpeed = authoring.rotationSpeed,
                    MaxAcceleration = authoring.maxAcceleration,
                   
                });
               AddComponent(entity, new AvoidanceConfig
               {
                   AddAvoidanceValueForInteractState = authoring.interactStateAddAvoidValue,
                   
               });
               AddComponent(entity, new SeparationConfig
               {
                   ValueWhenTotallyOverlapped = authoring.separationValueWhenTotallyOverlapped,
               });
            }
        }
    }

    public struct MovementConfig : IComponentData
    {
        public float WayPointDistanceSq;
        public float3 MarchExtent;
        public float InteractRangeSqBias;
        public uint ObstacleLayerMask;
        public uint DetectRaycastBelongsTo;
        public float RecordPosInterval;
        public float SurroundingRayDetectLength;
        public float SurroundingDetectRayStartBiasRatio;
  
    }

    public struct CbrConfig : IComponentData
    {
        public float SeparationWeight;
        public float AvoidanceWeight;
        public float CohesionWeight;
        public float AlignmentWeight;
        public float SeekTargetWeight;
        public float RotationSpeed;
        
        public float MaxAcceleration;
    }

    public struct AvoidanceConfig : IComponentData
    {
        public float AddAvoidanceValueForInteractState;
        public float CompromiseTimesToAvoidValue;
    }

    public struct SeparationConfig : IComponentData
    {
        public float ValueWhenTotallyOverlapped;
    }
  

    public struct GetGroundNormalConfig : IComponentData
    {
        public uint TerrainLayerMask;
        public uint DetectTerrainRaycastBelongsTo;
        public float DownDistance;
        public float LiftDistance;
    }


}