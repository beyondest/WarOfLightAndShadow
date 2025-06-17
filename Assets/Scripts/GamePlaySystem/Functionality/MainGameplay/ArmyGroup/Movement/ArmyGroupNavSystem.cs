using System;
using SparFlame.GamePlaySystem.General;
using SparFlame.GamePlaySystem.Movement;
using UnityEngine;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Burst;
using Unity.Jobs;
using Unity.Collections;
using UnityEngine.Experimental.AI;
// ReSharper disable UseIndexFromEndExpression


namespace SparFlame.GamePlaySystem.Functionality.MainGameplay.ArmyGroup
{
    [BurstCompile]
    [Obsolete("Obsolete")]
    public partial struct ArmyGroupNavSystem : ISystem
    {
        private NavMeshWorld _navMeshWorld;
        private NativeList<NavMeshQuery> _navMeshQueries;
        private EntityQuery _entityQuery;
        private NativeHashMap<int,float> _navAgentRadius;
        

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<ArmyGroupNavConfig>();
            state.RequireForUpdate<MainGamingTag>();
            _entityQuery = SystemAPI.QueryBuilder()
                .WithAllRW<NavAgentComponent>()
                .WithAll<ArmyGroupCalculatePathData>()
                .WithAll<ArmyGroupCalculateEnable>()
                .Build();
            
        }


        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var config = SystemAPI.GetSingleton<ArmyGroupNavConfig>();
            if (!_navAgentRadius.IsCreated)
            {
                InitNavMeshQueries( config);
                var buffer = SystemAPI.GetSingletonBuffer<AgentIdRadiusPair>();
                _navAgentRadius = new NativeHashMap<int, float>(buffer.Length + 1,Allocator.Persistent);
                foreach (var pair in buffer)
                {
                    _navAgentRadius[pair.Id] = pair.Radius;
                }
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
            var calculationPathDatas =
                _entityQuery.ToComponentDataArray<ArmyGroupCalculatePathData>(Allocator.TempJob);
            var navAgents =
            _entityQuery.ToComponentDataArray<NavAgentComponent>(Allocator.TempJob);
            
            for (var i = 0; i < entities.Length; i++)
            {
                var calculatePathJob = new CalculatePathJob
                {
                    Entity = entities[i],
                    NavAgent = navAgents[i],
                    FromPosition = calculationPathDatas[i].StartPosition,
                    ECB = ecbs[i],
                    Query = _navMeshQueries[i],
                    Iterations = config.maxIterations,
                    MaxPathSize = config.maxPathSize,
                    ExtentsOffset = config.extentsOffset
                };
                jobHandles[i] = calculatePathJob.Schedule();
            }
            
            JobHandle.CompleteAll(jobHandles);
            for (var i = 0; i < entities.Length; i++)
            {
                ecbs[i].Playback(state.EntityManager);
                ecbs[i].Dispose();
            }
            entities.Dispose();
            navAgents.Dispose();
            calculationPathDatas.Dispose();
            jobHandles.Dispose();
            ecbs.Dispose();
        }

        [BurstCompile]
        public void OnDestroy(ref SystemState state)
        {
            if(_navMeshQueries.IsCreated)
                DisposeNavMeshQueries();

            _navAgentRadius.Dispose();
        }


        [BurstCompile]
        private struct CalculatePathJob : IJob
        {
            public Entity Entity;
            public EntityCommandBuffer ECB;
            public NavMeshQuery Query;
            public NavAgentComponent NavAgent;
            [ReadOnly] public float3 FromPosition;
            [ReadOnly] public int MaxPathSize;
            [ReadOnly] public int Iterations;
            [ReadOnly] public float3 ExtentsOffset;

            public void Execute()
            {

                NavAgent.CalculationComplete = false;
                ECB.SetComponent(Entity, NavAgent);
                
                var toPosition = NavAgent.TargetPosition;
                var extents = NavAgent.Extents;
                extents += ExtentsOffset;
                var fromLocation = Query.MapLocation(FromPosition, extents, NavAgent.AgentId);
                var toLocation = Query.MapLocation(toPosition, extents, NavAgent.AgentId);
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
                    ECB.SetBuffer<WaypointBuffer>(Entity);

                    foreach (var location in result)
                    {
                        if (location.position != Vector3.zero)
                        {
                            var newWayPoint = new WaypointBuffer
                            {
                                Position = location.position
                            };
                            ECB.AppendToBuffer(Entity,newWayPoint);
                        }
                    }

                    NavAgent.CurrentWaypoint = 0;
                    NavAgent.CalculationComplete = true;

                    ECB.SetComponentEnabled<ArmyGroupCalculateEnable>(Entity,false);
                    ECB.SetComponent(Entity, NavAgent);
                }

                result.Dispose();
                straightPathFlag.Dispose();
                polygonIds.Dispose();
                vertexSide.Dispose();
            }
        }

        #region NavMeshQueriesPool

        private void InitNavMeshQueries(in ArmyGroupNavConfig config)
        {
            _navMeshWorld = NavMeshWorld.GetDefaultWorld();
            _navMeshQueries = new NativeList<NavMeshQuery>(config.initialNavMeshQueriesCapacity, Allocator.Persistent);
            for (int i = 0; i < config.initialNavMeshQueriesCapacity; i++)
            {
                _navMeshQueries.Add(new NavMeshQuery(_navMeshWorld, Allocator.Persistent, config.pathNodePoolSize));
            }
        }

        private void ExtendNavMeshQueries(int extendSize, in ArmyGroupNavConfig config)
        {
            for (var i = 0; i < extendSize; i++)
            {
                _navMeshQueries.Add(new NavMeshQuery(_navMeshWorld, Allocator.Persistent, config.pathNodePoolSize));
            }
        }


        private void DisposeNavMeshQueries()
        { 
            foreach (var query in _navMeshQueries)
            {
                query.Dispose();
            }
            _navMeshQueries.Dispose();
        }

        #endregion
    }
}