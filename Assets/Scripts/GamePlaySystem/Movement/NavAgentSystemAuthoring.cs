using Unity.Entities;
using UnityEngine;
using UnityEngine.Serialization;

namespace SparFlame.GamePlaySystem.Movement
{
    public class NavAgentSystemAuthoring : MonoBehaviour
    {
        [Tooltip("This is the distance threshold to judge if target is reachable, without considering any collider." +
                 "Because collider radius will be set separately for each target")]
        public float reachableDistanceThreshold = 0.5f;

        public int maxPathSize = 100;
        [Tooltip("Calculation path will fail beyond the iterations count")]
        public int maxIterations = 100;

        private class NavAgentSystemAuthoringBaker : Baker<NavAgentSystemAuthoring>
        {
            public override void Bake(NavAgentSystemAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.None);
                AddComponent(entity, new NavAgentSystemConfig
                {
                    ReachableDistance = authoring.reachableDistanceThreshold,
                    MaxPathSize = authoring.maxPathSize,
                    MaxIterations = authoring.maxIterations
                });
            }
        }
    }
    public struct NavAgentSystemConfig : IComponentData
    {
        public float ReachableDistance;
        public int MaxPathSize;
        public int MaxIterations;

    }
}