using System;
using System.Collections.Generic;
using NUnit.Framework;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.AI;

namespace SparFlame.GamePlaySystem.Movement
{
    public class NavAgentSystemAuthoring : MonoBehaviour
    {


        public int maxPathSize = 100;
        [Tooltip("Calculation path will fail beyond the iterations count")]
        public int maxIterations = 100;
        
  
        public int pathNodePoolSize = 1000;
        public int initialNavMeshQueriesCapacity = 100;
        public float3 extentOffset;
        public List<GameObject> differentAgents;
        
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
                    ExtentsOffset = authoring.extentOffset,
                });
                var buffer = AddBuffer<AgentIdRadiusPair>(entity);
                foreach (var go in authoring.differentAgents)
                {
                    var agent = go.GetComponent<NavMeshAgent>();
                    buffer.Add(new AgentIdRadiusPair
                    {
                        Id = agent.agentTypeID,
                        Radius = agent.radius
                    });
                }
            }
        }
    }
    public struct NavAgentSystemConfig : IComponentData
    {
        public int MaxPathSize;
        public int MaxIterations;
        public int PathNodePoolSize;
        public int InitialNavMeshQueriesCapacity;
        public bool IsInitialized;
        public float3 ExtentsOffset;
    }

    public struct AgentIdRadiusPair : IBufferElementData
    {
        public int Id;
        public float Radius;
    }
    
}