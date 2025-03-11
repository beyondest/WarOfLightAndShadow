using UnityEngine;
using Unity.Entities;
using Unity.Mathematics;

namespace SparFlame.GamePlaySystem.Movement
{
    public class MovableAttributesAuthoring : MonoBehaviour
    {
        public Transform targetTransform;
        public float moveSpeed = 3f;

        [Tooltip("This interval determines how long the path calculation is executed once")]
        public float calculateInterval = 1.0f;

        [Tooltip(" Extents determines the map location in navMesh of target position, if too small, for buildings center as target position,\n " +
                 "then it will fail to find the path to the building  " +
                 " Suggest setting as the collider volume of the target")]
        public float3 extents = new float3(1, 1, 1);
        private class Baker : Baker<MovableAttributesAuthoring>
        {
            public override void Bake(MovableAttributesAuthoring authoring)
            {
                var authoringEntity = GetEntity(TransformUsageFlags.Dynamic);

                AddComponent(authoringEntity, new NavAgentComponent
                {
                    TargetPosition = authoring.targetTransform.position,
                    CalculateInterval = authoring.calculateInterval,
                    Extents = authoring.extents,
                    Reachable = true
                });
                
                AddComponent(authoringEntity, new MovableData
                {
                    MoveSpeed = authoring.moveSpeed,
                    ShouldMove = true
                });
                AddBuffer<WaypointBuffer>(authoringEntity);
            }
        }
    }


    public struct MovableData : IComponentData
    {
        public float MoveSpeed;
        public bool ShouldMove;

    }
        
    public struct NavAgentComponent : IComponentData
    {
        public float3 TargetPosition;
        public bool PathCalculated;
        public int CurrentWaypoint;
        public float NextPathCalculateTime;
        public bool IsNavQuerySet;
        public float CalculateInterval;
        public float3 Extents;
        public bool Reachable;
    }

    public struct WaypointBuffer : IBufferElementData
    {
        public float3 WayPoint;
    }
}