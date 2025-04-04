﻿using Unity.Entities;
using UnityEngine;
namespace SparFlame.GamePlaySystem.Movement
{
    public class NavAgentSystemAuthoring : MonoBehaviour
    {


        public int maxPathSize = 100;
        [Tooltip("Calculation path will fail beyond the iterations count")]
        public int maxIterations = 100;

  
        public int pathNodePoolSize = 1000;
        public int initialNavMeshQueriesCapacity = 100;
        
        // public int parallelJobBatchSize = 2;
        
        private class NavAgentSystemAuthoringBaker : Baker<NavAgentSystemAuthoring>
        {
            public override void Bake(NavAgentSystemAuthoring authoring)
            {
                
                var entity = GetEntity(TransformUsageFlags.None);
                AddComponent(entity, new NavAgentSystemConfig
                {
                    MaxPathSize = authoring.maxPathSize,
                    MaxIterations = authoring.maxIterations,
                    PathNodePoolSize = authoring.pathNodePoolSize,
                    InitialNavMeshQueriesCapacity = authoring.initialNavMeshQueriesCapacity,
                    // ParallelJobBatchSize = authoring.parallelJobBatchSize,
                });
            }
        }
    }
    public struct NavAgentSystemConfig : IComponentData
    {
        public int MaxPathSize;
        public int MaxIterations;
        public int PathNodePoolSize;
        public int InitialNavMeshQueriesCapacity;
        public bool IsPoolInitialized;
        // public int ParallelJobBatchSize;
    }
}