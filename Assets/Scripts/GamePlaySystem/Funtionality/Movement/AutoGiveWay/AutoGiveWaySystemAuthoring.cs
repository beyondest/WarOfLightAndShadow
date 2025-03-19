using UnityEngine;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine.Serialization;

namespace SparFlame.GamePlaySystem.Movement
{
    public class AutoGiveWaySystemAuthoring : MonoBehaviour
    {
        [Tooltip("This duration controls how long it takes to auto give way in one direction")]
        public float duration = 1f;
        
        [Tooltip("This ratio * being squeezed max collider shapeXz is the detect collider radius")]
        public float squeezeColliderDetectionRatio = 1.5f;

        public LayerMask obstacleLayerMask;
        public LayerMask detectRayBelongsTo;
        private class Baker : Baker<AutoGiveWaySystemAuthoring>
        {
            public override void Bake(AutoGiveWaySystemAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.None);
                AddComponent(entity, new AutoGiveWaySystemConfig
                {
                    Duration = authoring.duration,
                    ObstacleLayerMask = (uint)authoring.obstacleLayerMask.value,
                    DetectRayBelongsTo = (uint)authoring.detectRayBelongsTo.value,
                    SqueezeColliderDetectionRatio = authoring.squeezeColliderDetectionRatio
                });
            }
        }
    }

    public struct AutoGiveWaySystemConfig : IComponentData
    {
        public float Duration;
        public uint ObstacleLayerMask;
        public uint DetectRayBelongsTo;
        public float SqueezeColliderDetectionRatio;
    }


    public struct AutoGiveWayData : IComponentData
    {
        public float3 MoveVector;
        public float ElapsedTime;
        public bool IfGoBack;
    }

    public struct SqueezeData : IComponentData
    {
        public float3 MoveVector;
    }
    
}