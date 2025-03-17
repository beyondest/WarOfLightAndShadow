using System;
using SparFlame.GamePlaySystem.General;
using UnityEngine;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Burst;
using Unity.Jobs;
using Unity.Transforms;
using Unity.Collections;
using UnityEngine.Experimental.AI;


namespace SparFlame.GamePlaySystem.Movement
{
    
    [BurstCompile]
    [UpdateAfter(typeof(MovementSystem))]
    [Obsolete("Obsolete")]
    public partial struct NavAgentSystem : ISystem
    {
        private NavMeshWorld _navMeshWorld;
        private BufferLookup<WaypointBuffer> _waypointLookup;
        private NativeArray<Entity> _entities;
        private NativeList<NavMeshQuery> _navMeshQueries;
        private const int PathNodePoolSize = 1000;


        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<NotPauseTag>();
            state.RequireForUpdate<NavAgentSystemConfig>();
            _navMeshWorld = NavMeshWorld.GetDefaultWorld();
            _waypointLookup = state.GetBufferLookup<WaypointBuffer>(true);
            _navMeshQueries = new NativeList<NavMeshQuery>(Allocator.Persistent);
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var  config = SystemAPI.GetSingleton<NavAgentSystemConfig>();

            var entityQuery = SystemAPI.QueryBuilder()
                .WithAll<NavAgentComponent>()
                .WithAll<LocalTransform>().Build();
            _entities = entityQuery.ToEntityArray(Allocator.TempJob);
            var ecbs = new NativeArray<EntityCommandBuffer>(_entities.Length, Allocator.TempJob);
            for (var i = 0; i < _entities.Length; i++)
            {
                ecbs[i] = new EntityCommandBuffer(Allocator.TempJob);
            }
            _waypointLookup.Update(ref state);
            var jobHandles = new NativeArray<JobHandle>(_entities.Length, Allocator.TempJob);
            var navAgents =
                entityQuery.ToComponentDataArray<NavAgentComponent>(Allocator.TempJob);
            var localTransforms =
                entityQuery.ToComponentDataArray<LocalTransform>(Allocator.TempJob);

            for (var i = 0; i < _entities.Length; i++)
            {
                // Only recalculate navMeshQueries when entity's navMeshQuery is not set yet
                // This code must be executed before the enableCalculation check, or will throw index wrong
                if (!navAgents[i].IsNavQuerySet)
                {
                    var navAgent = navAgents[i];
                    _navMeshQueries.Add(new NavMeshQuery(_navMeshWorld, Allocator.Persistent, PathNodePoolSize));
                    navAgent.IsNavQuerySet = true;
                    navAgents[i] = navAgent;
                }
                
                // Only calculate for the enable calculation agents
                if(!navAgents[i].EnableCalculation)continue;
                // Only recalculate the path once in an interval OR the target is updated
                var curTime = SystemAPI.Time.ElapsedTime;
                if (!(navAgents[i].ForceCalculate || navAgents[i].NextPathCalculateTime < curTime)) continue;
                // Start calculate                
                var calculatePathJob = new CalculatePathJob
                {
                    Entity = _entities[i],
                    NavAgent = navAgents[i],
                    FromPosition = new float3(localTransforms[i].Position.x, 0f, localTransforms[i].Position.z),
                    ECB = ecbs[i],
                    Query = _navMeshQueries[i],
                    ElapsedTime = (float)SystemAPI.Time.ElapsedTime,
                    Extents = navAgents[i].Extents,
                    Iterations = config.MaxIterations,
                    MaxPathSize = config.MaxPathSize
                };
                jobHandles[i] = calculatePathJob.Schedule();
            }
            
            JobHandle.CompleteAll(jobHandles);
            for (var i = 0; i < _entities.Length; i++)
            {
                ecbs[i].Playback(state.EntityManager);
                ecbs[i].Dispose();
            }
            
            _entities.Dispose();
            navAgents.Dispose();
            localTransforms.Dispose();
            jobHandles.Dispose();
            ecbs.Dispose();
        }

        [BurstCompile]
        public void OnDestroy(ref SystemState state)
        {
            foreach (var query in _navMeshQueries)
            {
                query.Dispose();
            }
            _navMeshQueries.Dispose();
        }

       

        [BurstCompile]
        private struct CalculatePathJob : IJob
        {
            public Entity Entity;
            public NavAgentComponent NavAgent;
            public EntityCommandBuffer ECB;
            public NavMeshQuery Query;
            
            [ReadOnly] public float3 FromPosition;
            [ReadOnly] public float3 Extents;
            [ReadOnly] public float ElapsedTime;
            [ReadOnly] public int MaxPathSize;
            [ReadOnly] public int Iterations;

            public void Execute()
            {
                NavAgent.NextPathCalculateTime = ElapsedTime + NavAgent.CalculateInterval;
                NavAgent.CalculationComplete = false;
                NavAgent.ForceCalculate = false;
                ECB.SetComponent(Entity, NavAgent);
                var toPosition = NavAgent.TargetPosition;

                var fromLocation = Query.MapLocation(FromPosition, Extents, NavAgent.AgentId);
                var toLocation = Query.MapLocation(toPosition, Extents, NavAgent.AgentId);
                if (!Query.IsValid(fromLocation) || !Query.IsValid(toLocation)) return;
                Debug.Log($"is valid ");

                var status = Query.BeginFindPath(fromLocation, toLocation);
                
                // Notice : If target is not reachable, and extents is also not reachable, it will return Failure this step
                // The status only return one main status binding with a detailed status
                // Main Status : InProgress, Success, Failure
                if(status is not (PathQueryStatus.InProgress or PathQueryStatus.Success) )return;
                Debug.Log("Begin Find Path");
                status = Query.UpdateFindPath(Iterations, out _);
                
                if ((status & PathQueryStatus.Success) == 0) return;
                Debug.Log("Find success");
                
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
                    // waypointBuffer.Clear();
                    ECB.SetBuffer<WaypointBuffer>(Entity);
                    
                    foreach (var location in result)
                    {
                        if (location.position != Vector3.zero)
                        {
                            ECB.AppendToBuffer(Entity, new WaypointBuffer
                            {
                                WayPoint = new float3(location.position.x, 0f, location.position.z),
                            });
                        }
                    }

                    NavAgent.CurrentWaypoint = 0;
                    NavAgent.CalculationComplete = true;
                    ECB.SetComponent(Entity, NavAgent);
                    Debug.Log("Strait Path success");
                }

                straightPathFlag.Dispose();
                polygonIds.Dispose();
                vertexSide.Dispose();
            }
        }
    }
}