using UnityEngine;
using Unity.Entities;
using Unity.Mathematics;

namespace SparFlame.GamePlaySystem.Movement
{
    public class NavAgentAuthoring : MonoBehaviour
    {
        [SerializeField] private Transform targetTransform;
        [SerializeField] private float moveSpeed;

        private class Baker : Baker<NavAgentAuthoring>
        {
            public override void Bake(NavAgentAuthoring authoring)
            {
                Entity authoringEntity = GetEntity(TransformUsageFlags.Dynamic);

                AddComponent(authoringEntity, new NavAgentComponent
                {
                    TargetPosition = authoring.targetTransform.position,
                    MoveSpeed = authoring.moveSpeed
                });
                AddBuffer<WaypointBuffer>(authoringEntity);
            }
        }


    }

    public struct NavAgentComponent : IComponentData
    {
        public float3 TargetPosition;
        public bool PathCalculated;
        public int CurrentWaypoint;
        public float MoveSpeed;
        public float NextPathCalculateTime;
        public bool IsNavQuerySet;
    }

    public struct WaypointBuffer : IBufferElementData
    {
        public float3 WayPoint;
    }
}