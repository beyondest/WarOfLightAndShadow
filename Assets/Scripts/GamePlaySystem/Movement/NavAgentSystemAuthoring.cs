using Unity.Entities;
using UnityEngine;

namespace SparFlame.GamePlaySystem.Movement
{
    public class NavAgentSystemAuthoring : MonoBehaviour
    {


        public int maxPathSize = 100;
        [Tooltip("Calculation path will fail beyond the iterations count")]
        public int maxIterations = 100;

        [Tooltip("This is to ensure that the command is being dealt timely." +
                 " If current elapse time - the command sending time < realTimeResponseInterval," +
                 " then the calculation will begin," +
                 "ignored the calculation interval. But this timely calculation will only happen once for one command ")]
        public float realTimeResponseInterval = 0.3f;

        private class NavAgentSystemAuthoringBaker : Baker<NavAgentSystemAuthoring>
        {
            public override void Bake(NavAgentSystemAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.None);
                AddComponent(entity, new NavAgentSystemConfig
                {
                    MaxPathSize = authoring.maxPathSize,
                    MaxIterations = authoring.maxIterations,
                    RealTimeResponseInterval = authoring.realTimeResponseInterval,
                });
            }
        }
    }
    public struct NavAgentSystemConfig : IComponentData
    {
        public int MaxPathSize;
        public int MaxIterations;
        public float RealTimeResponseInterval;

    }
}