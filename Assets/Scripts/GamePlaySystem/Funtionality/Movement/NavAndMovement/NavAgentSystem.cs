﻿using System;
using SparFlame.GamePlaySystem.General;
using UnityEngine;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Burst;
using Unity.Jobs;
using Unity.Transforms;
using Unity.Collections;
using UnityEngine.Experimental.AI;
// ReSharper disable UseIndexFromEndExpression


namespace SparFlame.GamePlaySystem.Movement
{
    // [UpdateAfter(typeof(MovementSystem))]
    [BurstCompile]
    [Obsolete("Obsolete")]
    public partial struct NavAgentSystem : ISystem
    {
        private NavMeshWorld _navMeshWorld;
        private NativeList<NavMeshQuery> _navMeshQueries;
        private EntityQuery _entityQuery;
        
        
        // private BufferLookup<WaypointBuffer> _waypointLookup;
        // private ComponentLookup<NavAgentComponent> _navAgentLookup;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<NotPauseTag>();
            state.RequireForUpdate<NavAgentSystemConfig>();
            // _navMeshWorld = NavMeshWorld.GetDefaultWorld();
            // _waypointLookup = state.GetBufferLookup<WaypointBuffer>();
            // _navAgentLookup = state.GetComponentLookup<NavAgentComponent>();
            
            _entityQuery = SystemAPI.QueryBuilder()
                .WithAllRW<NavAgentComponent>()
                .WithAll<MovingStateTag>()
                .WithAll<LocalTransform>().Build();

            // _navMeshQueries = new NativeList<NavMeshQuery>(Allocator.Persistent);
        }


        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            ref var config =ref SystemAPI.GetSingletonRW<NavAgentSystemConfig>().ValueRW;
            if (!config.IsPoolInitialized)
            {
                InitNavMeshQueries(ref config);
            }
            
            if (_entityQuery.IsEmpty) return;
            var entities = _entityQuery.ToEntityArray(Allocator.TempJob);
            if (entities.Length > _navMeshQueries.Length)
            {
                ExtendNavMeshQueries(entities.Length - _navMeshQueries.Length, in config);
            }
            var ecbs = new NativeArray<EntityCommandBuffer>(entities.Length, Allocator.TempJob);
            for (var i = 0; i < entities.Length; i++)
            {
                ecbs[i] = new EntityCommandBuffer(Allocator.TempJob);
            }
            var jobHandles = new NativeArray<JobHandle>(entities.Length, Allocator.TempJob);
            var localTransforms =
                _entityQuery.ToComponentDataArray<LocalTransform>(Allocator.TempJob);
            var navAgents =
            _entityQuery.ToComponentDataArray<NavAgentComponent>(Allocator.TempJob);
            
            for (var i = 0; i < entities.Length; i++)
            {
                var calculatePathJob = new CalculatePathJob
                {
                    Entity = entities[i],
                    NavAgent = navAgents[i],
                    FromPosition = new float3(localTransforms[i].Position.x, 0f, localTransforms[i].Position.z),
                    ECB = ecbs[i],
                    Query = _navMeshQueries[i],
                    ElapsedTime = (float)SystemAPI.Time.ElapsedTime,
                    Iterations = config.MaxIterations,
                    MaxPathSize = config.MaxPathSize
                };
                jobHandles[i] = calculatePathJob.Schedule();
            }

            if (entities.Length < _navMeshQueries.Length)
            {
                DisposeRedundantNavMeshQueries(_navMeshQueries.Length - _navMeshQueries.Length, in config);
            }

            JobHandle.CompleteAll(jobHandles);
            for (var i = 0; i < entities.Length; i++)
            {
                ecbs[i].Playback(state.EntityManager);
                ecbs[i].Dispose();
            }
            entities.Dispose();
            navAgents.Dispose();
            localTransforms.Dispose();
            jobHandles.Dispose();
            ecbs.Dispose();
        }

        [BurstCompile]
        public void OnDestroy(ref SystemState state)
        {
            // _entityQuery.Dispose();
            DisposeNavMeshQueries();
        }


        [BurstCompile]
        private struct CalculatePathJob : IJob
        {
            public Entity Entity;
            public NavAgentComponent NavAgent;
            public EntityCommandBuffer ECB;
            public NavMeshQuery Query;
            
            [ReadOnly] public float3 FromPosition;
            [ReadOnly] public float ElapsedTime;
            [ReadOnly] public int MaxPathSize;
            [ReadOnly] public int Iterations;

            public void Execute()
            {
                // ref var navAgent = ref NavAgentLookup.GetRefRW(Entity).ValueRW;
                // Only calculate for the enable calculation agents
                if (!NavAgent.EnableCalculation) return;
                // Only recalculate the path once in an interval OR the target is updated
                if (!(NavAgent.ForceCalculate || NavAgent.NextPathCalculateTime < ElapsedTime)) return;
                
                NavAgent.NextPathCalculateTime = ElapsedTime + NavAgent.CalculateInterval;
                NavAgent.CalculationComplete = false;
                NavAgent.ForceCalculate = false;
                ECB.SetComponent(Entity, NavAgent);
                
                var toPosition = NavAgent.TargetPosition;

                var fromLocation = Query.MapLocation(FromPosition, NavAgent.Extents, NavAgent.AgentId);
                var toLocation = Query.MapLocation(toPosition, NavAgent.Extents, NavAgent.AgentId);
                if (!Query.IsValid(fromLocation) || !Query.IsValid(toLocation)) return;

                var status = Query.BeginFindPath(fromLocation, toLocation);

                // Notice : If target is not reachable, and extents is also not reachable, it will return Failure this step
                // The status only return one main status binding with a detailed status
                // Main Status : InProgress, Success, Failure
                if (status is not (PathQueryStatus.InProgress or PathQueryStatus.Success)) return;
                status = Query.UpdateFindPath(Iterations, out _);

                if ((status & PathQueryStatus.Success) == 0) return;

                Query.EndFindPath(out var pathSize);

                var result =
                    new NativeArray<NavMeshLocation>(pathSize + 1, Allocator.Temp);
                var straightPathFlag =
                    new NativeArray<StraightPathFlags>(MaxPathSize, Allocator.Temp);
                var vertexSide = new NativeArray<float>(MaxPathSize, Allocator.Temp);
                var polygonIds =
                    new NativeArray<PolygonId>(pathSize + 1, Allocator.Temp);
                var straightPathCount = 0;

                Query.GetPathResult(polygonIds);

                var returningStatus = PathUtils.FindStraightPath
                (
                    Query,
                    FromPosition,
                    toPosition,
                    polygonIds,
                    pathSize,
                    ref result,
                    ref straightPathFlag,
                    ref vertexSide,
                    ref straightPathCount,
                    MaxPathSize
                );

                if (returningStatus == PathQueryStatus.Success)
                {
                    // WaypointLookup.TryGetBuffer(Entity, out var waypointBuffer);
                    // waypointBuffer.Clear();
                    ECB.SetBuffer<WaypointBuffer>(Entity);

                    foreach (var location in result)
                    {
                        if (location.position != Vector3.zero)
                        {
                            var newWayPoint = new WaypointBuffer
                            {
                                WayPoint = new float3(location.position.x, 0f, location.position.z),
                            };
                            // waypointBuffer.Add(newWayPoint);   
                            ECB.AppendToBuffer(Entity,newWayPoint);
                        }
                    }

                    NavAgent.CurrentWaypoint = 0;
                    NavAgent.CalculationComplete = true;
                    ECB.SetComponent(Entity, NavAgent);
                }

                result.Dispose();
                straightPathFlag.Dispose();
                polygonIds.Dispose();
                vertexSide.Dispose();
            }
        }


        #region NavMeshQueriesPool

        private void InitNavMeshQueries(ref NavAgentSystemConfig config)
        {
            config.IsPoolInitialized = true;
            _navMeshWorld = NavMeshWorld.GetDefaultWorld();
            _navMeshQueries = new NativeList<NavMeshQuery>(config.InitialNavMeshQueriesCapacity, Allocator.Persistent);
            for (int i = 0; i < config.InitialNavMeshQueriesCapacity; i++)
            {
                _navMeshQueries.Add(new NavMeshQuery(_navMeshWorld, Allocator.Persistent, config.PathNodePoolSize));
            }
        }

        private void ExtendNavMeshQueries(int extendSize, in NavAgentSystemConfig config)
        {
            for (var i = 0; i < extendSize; i++)
            {
                _navMeshQueries.Add(new NavMeshQuery(_navMeshWorld, Allocator.Persistent, config.PathNodePoolSize));
            }
        }

  
        private void DisposeRedundantNavMeshQueries(int redundantSize, in NavAgentSystemConfig config)
        {
            var thresh = math.max(config.InitialNavMeshQueriesCapacity, _navMeshQueries.Length - redundantSize);
            for (var i = _navMeshQueries.Length - 1; i >= thresh; i--)
            {
                var query = _navMeshQueries[i];
                query.Dispose();
                _navMeshQueries.RemoveAt(i);
            }
        }

        private void DisposeNavMeshQueries()
        { 
            if(!_navMeshQueries.IsCreated)return;
            foreach (var query in _navMeshQueries)
            {
                query.Dispose();
            }

            _navMeshQueries.Dispose();
        }

        #endregion
    }
}