using System;
using SparFlame.Components.General;
using SparFlame.Components.SubGameplay;
using SparFlame.Core.Utils;
using UnityEngine;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Burst;
using Unity.Jobs;
using Unity.Transforms;
using Unity.Collections;
using UnityEngine.Experimental.AI;

// ReSharper disable UseIndexFromEndExpression


namespace SparFlame.Systems.SubGameplay.Movement
{
    [BurstCompile]
    [Obsolete("Obsolete")]
    public partial struct NavAgentSystem : ISystem
    {
        private NavMeshWorld _navMeshWorld;
        private NativeList<NavMeshQuery> _navMeshQueries;
        private EntityQuery _entityQuery;
        private NativeHashMap<int, float> _navAgentRadius;


        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<EndSimulationEntityCommandBufferSystem.Singleton>();
            state.RequireForUpdate<GameTimeData>();
            state.RequireForUpdate<SubGamingTag>();
            state.RequireForUpdate<NavAgentSystemConfig>();
            _entityQuery = SystemAPI.QueryBuilder()
                .WithAllRW<NavAgentComponent>()
                .WithAll<MovingStateTag>()
                .WithAll<LocalTransform>().Build();
        }


        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var config = SystemAPI.GetSingleton<NavAgentSystemConfig>();
            if (!_navAgentRadius.IsCreated)
            {
                InitNavMeshQueries(ref config);
                var buffer = SystemAPI.GetSingletonBuffer<AgentIdRadiusPair>();
                _navAgentRadius = new NativeHashMap<int, float>(buffer.Length + 1, Allocator.Persistent);
                foreach (var pair in buffer)
                {
                    _navAgentRadius[pair.Id] = pair.Radius;
                }
            }

            if (_entityQuery.IsEmpty) return;
            var entities = _entityQuery.ToEntityArray(Allocator.TempJob);
            if (entities.Length > _navMeshQueries.Length)
            {
                ExtendNavMeshQueries(entities.Length - _navMeshQueries.Length, config);
            }

            var jobHandles = new NativeArray<JobHandle>(entities.Length, Allocator.TempJob);
            var localTransforms = _entityQuery.ToComponentDataArray<LocalTransform>(Allocator.TempJob);
            var navAgents = _entityQuery.ToComponentDataArray<NavAgentComponent>(Allocator.TempJob);

            var elapsedTime = SystemAPI.GetSingleton<GameTimeData>().ElapsedTime;
            var ecbSingleton = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>();
            for (var i = 0; i < entities.Length; i++)
            {
                var calculatePathJob = new CalculatePathJob
                {
                    ECB = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged),
                    Entity = entities[i],
                    NavAgent = navAgents[i],
                    NavAgentRadius = _navAgentRadius,
                    // FromPosition = new float3(localTransforms[i].Position.x, 0f, localTransforms[i].Position.z),
                    FromPosition = localTransforms[i].Position,
                    Query = _navMeshQueries[i],
                    ElapsedTime = elapsedTime,
                    Iterations = config.MaxIterations,
                    MaxPathSize = config.MaxPathSize,
                    ExtentsOffset = config.ExtentsOffset
                };
                jobHandles[i] = calculatePathJob.Schedule();
            }

            state.Dependency = JobHandle.CombineDependencies(jobHandles);
    

            entities.Dispose(state.Dependency);
            navAgents.Dispose(state.Dependency);
            localTransforms.Dispose(state.Dependency);
            jobHandles.Dispose(state.Dependency);
        }

        [BurstCompile]
        public void OnDestroy(ref SystemState state)
        {
            if (_navMeshQueries.IsCreated)
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
            [ReadOnly] public NativeHashMap<int, float> NavAgentRadius;
            [ReadOnly] public float3 FromPosition;
            [ReadOnly] public float ElapsedTime;
            [ReadOnly] public int MaxPathSize;
            [ReadOnly] public int Iterations;
            [ReadOnly] public float3 ExtentsOffset;

            public void Execute()
            {
                // Only calculate for the enable calculation agents
                if (!NavAgent.enableCalculation) return;
                // Only recalculate the path once in an interval OR the target is updated
                if (!(NavAgent.forceCalculate || NavAgent.nextPathCalculateTime < ElapsedTime)) return;
                NavAgent.nextPathCalculateTime = ElapsedTime + NavAgent.calculateInterval;
                NavAgent.calculationComplete = false;
                NavAgent.forceCalculate = false;
                ECB.SetComponent(Entity, NavAgent);

                var toPosition = NavAgent.targetPosition;
                var radius = NavAgentRadius[NavAgent.agentId];
                var extents = new float3(NavAgent.extents.x + radius, NavAgent.extents.y, NavAgent.extents.z + radius);
                extents += ExtentsOffset;
                var fromLocation = Query.MapLocation(FromPosition, extents, NavAgent.agentId);
                var toLocation = Query.MapLocation(toPosition, extents, NavAgent.agentId);
                if (!Query.IsValid(fromLocation) || !Query.IsValid(toLocation))
                {
                    NavAgent.calculationInfo = NavAgentCalculateInfo.FailedAtQuery;
                    ECB.SetComponent(Entity, NavAgent);
                    return;
                }

                var status = Query.BeginFindPath(fromLocation, toLocation);

                // Notice : If target is not reachable, and extents is also not reachable, it will return Failure this step
                // The status only return one main status binding with a detailed status
                // Main Status : InProgress, Success, Failure
                if (status is not (PathQueryStatus.InProgress or PathQueryStatus.Success))
                {
                    NavAgent.calculationInfo = NavAgentCalculateInfo.FailedAtStartingCalculation;
                    ECB.SetComponent(Entity, NavAgent);

                    return;
                }

                status = Query.UpdateFindPath(Iterations, out _);

                if ((status & PathQueryStatus.Success) == 0)
                {
                    NavAgent.calculationInfo = NavAgentCalculateInfo.FailedAfterCalculation;
                    ECB.SetComponent(Entity, NavAgent);

                    return;
                }

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
                                position = new float3(location.position.x, location.position.y, location.position.z),
                            };
                            ECB.AppendToBuffer(Entity, newWayPoint);
                        }
                    }
                    NavAgent.currentWaypoint = 0;
                    NavAgent.calculationComplete = result.Length != 0;
                    NavAgent.calculationInfo = result.Length != 0 ? NavAgentCalculateInfo.Success : NavAgentCalculateInfo.NoWaypointsAfterCalculation;
                }
                else
                {
                    NavAgent.calculationInfo = NavAgentCalculateInfo.FailedAfterFindingStraightPath;
                }

                ECB.SetComponent(Entity, NavAgent);
                result.Dispose();
                straightPathFlag.Dispose();
                polygonIds.Dispose();
                vertexSide.Dispose();
            }
        }

        #region NavMeshQueriesPool

        private void InitNavMeshQueries(ref NavAgentSystemConfig config)
        {
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


        // private void DisposeRedundantNavMeshQsueries(int redundantSize, in NavAgentSystemConfig config)
        // {
        //     var thresh = math.max(config.InitialNavMeshQueriesCapacity, _navMeshQueries.Length - redundantSize);
        //     for (var i = _navMeshQueries.Length - 1; i >= thresh; i--)
        //     {
        //         var query = _navMeshQueries[i];
        //         query.Dispose();
        //         _navMeshQueries.RemoveAt(i);
        //     }
        // }

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