using UnityEngine;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine.Serialization;

namespace SparFlame.GamePlaySystem.Movement
{
    public class AutoGiveWaySystemAuthoring : MonoBehaviour
    {
        public float duration = 1f;
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
                });
            }
        }
    }

    public struct AutoGiveWaySystemConfig : IComponentData
    {
        public float Duration;
        public uint ObstacleLayerMask;
        public uint DetectRayBelongsTo;
    }


    public struct AutoGiveWayData : IComponentData
    {
        public float3 MoveVector;
        public float ElapsedTime;
        public bool IfGoBack;
    }
    
}