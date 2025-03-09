using System;
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
    [Obsolete("Obsolete")]
    public partial struct NavAgent2System : ISystem
    {
        private EntityQuery _entityQuery;
        private NavMeshWorld _navMeshWorld;
        private BufferLookup<WaypointBuffer> _waypointLookup;
        private NativeArray<Entity> _entities;
        private NativeList<NavMeshQuery> _navMeshQueries;
        private const int PathNodesCount = 1000;


        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            _entityQuery = new EntityQueryBuilder(Allocator.Persistent)
                .WithAll<NavAgentComponent>()
                .WithAll<LocalTransform>().Build(ref state);
            _navMeshWorld = NavMeshWorld.GetDefaultWorld();
            _waypointLookup = state.GetBufferLookup<WaypointBuffer>(true);
            _navMeshQueries = new NativeList<NavMeshQuery>(Allocator.Persistent);
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            _entities = _entityQuery.ToEntityArray(Allocator.TempJob);
            var ecbs = new NativeArray<EntityCommandBuffer>(_entities.Length, Allocator.TempJob);
            for (var i = 0; i < _entities.Length; i++)
            {
                ecbs[i] = new EntityCommandBuffer(Allocator.TempJob);
            }

            _waypointLookup.Update(ref state);
            var jobHandles = new NativeArray<JobHandle>(_entities.Length, Allocator.TempJob);
            var navAgents =
                _entityQuery.ToComponentDataArray<NavAgentComponent>(Allocator.TempJob);
            var localTransforms =
                _entityQuery.ToComponentDataArray<LocalTransform>(Allocator.TempJob);

            for (var i = 0; i < _entities.Length; i++)
            {
                
                // Only recalculate navMeshQueries when entity's navMeshQuery is not set yet
                if (!navAgents[i].IsNavQuerySet)
                {
                    var navAgent = navAgents[i];
                    _navMeshQueries.Add(new NavMeshQuery(_navMeshWorld, Allocator.Persistent, PathNodesCount));
                    navAgent.IsNavQuerySet = true;
                    navAgents[i] = navAgent;
                }
                
                // Only recalculate the path 
                if (navAgents[i].NextPathCalculateTime < SystemAPI.Time.ElapsedTime)
                {
                    var calculatePathJob = new CalculatePathJob
                    {
                        Entity = _entities[i],
                        NavAgent = navAgents[i],
                        FromPosition = localTransforms[i].Position,
                        ECB = ecbs[i],
                        Query = _navMeshQueries[i],
                    };
                    jobHandles[i] = calculatePathJob.Schedule();
                }
                else if (navAgents[i].PathCalculated)
                {
                    var moveJob = new MoveJob
                    {
                        DeltaTime = SystemAPI.Time.DeltaTime,
                        ECB = ecbs[i],
                        Entity = _entities[i],
                        NavAgent = navAgents[i],
                        Transform = localTransforms[i],
                        Waypoints = _waypointLookup,
                    };
                    jobHandles[i] = moveJob.Schedule();
                }
            }
            
            JobHandle.CompleteAll(jobHandles);
            
            for (var i = 0; i < _entities.Length; i++)
            {
                ecbs[i].Playback(state.EntityManager);
                ecbs[i].Dispose();
            }

            navAgents.Dispose();
            localTransforms.Dispose();
            jobHandles.Dispose();
            _entityQuery.Dispose();
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
        private struct MoveJob : IJob
        {
            public NavAgentComponent NavAgent;
            public LocalTransform Transform;
            public Entity Entity;
            public float DeltaTime;
            public EntityCommandBuffer ECB;
            [ReadOnly] public BufferLookup<WaypointBuffer> Waypoints;

            public void Execute()
            {
                // Check if entity is already reaching the target

                if (Waypoints.TryGetBuffer(Entity, out var waypointBuffer)
                    && math.distance(Transform.Position, waypointBuffer[NavAgent.CurrentWaypoint].WayPoint) <
                    0.4f)
                {
                    // Check if next waypoint is calculated already
                    if (NavAgent.CurrentWaypoint + 1 < waypointBuffer.Length)
                    {
                        NavAgent.CurrentWaypoint += 1;
                    }

                    ECB.SetComponent(Entity, NavAgent);
                }

                var direction = waypointBuffer[NavAgent.CurrentWaypoint].WayPoint - Transform.Position;
                var angle = math.degrees(math.atan2(direction.z, direction.x));

                Transform.Rotation = math.slerp(
                    Transform.Rotation,
                    quaternion.Euler(new float3(0, angle, 0)),
                    DeltaTime);

                Transform.Position +=
                    math.normalize(direction) * DeltaTime * NavAgent.MoveSpeed;
                ECB.SetComponent(Entity, Transform);
            }
        }

        [BurstCompile]
        private struct CalculatePathJob : IJob
        {
            public Entity Entity;
            public NavAgentComponent NavAgent;
            public EntityCommandBuffer ECB;
            public float3 FromPosition;
            public NavMeshQuery Query;


            public void Execute()
            {
                NavAgent.NextPathCalculateTime += 1;
                NavAgent.PathCalculated = false;
                ECB.SetComponent(Entity, NavAgent);
                
                
                var toPosition = NavAgent.TargetPosition;
                var extents = new float3(1, 1, 1);

                var fromLocation = Query.MapLocation(FromPosition, extents, 0);
                var toLocation = Query.MapLocation(toPosition, extents, 0);

                var maxPathSize = 100;
                var iterations = 100;

                if (!Query.IsValid(fromLocation) || !Query.IsValid(toLocation)) return;
                var status = Query.BeginFindPath(fromLocation, toLocation);
                if (!(status == PathQueryStatus.InProgress | status == PathQueryStatus.Success)) return;
                status = Query.UpdateFindPath(iterations, out _);
                if (status != PathQueryStatus.Success) return;
                Query.EndFindPath(out var pathSize);

                var result =
                    new NativeArray<NavMeshLocation>(pathSize + 1, Allocator.Temp);
                var straightPathFlag =
                    new NativeArray<StraightPathFlags>(maxPathSize, Allocator.Temp);
                var vertexSide = new NativeArray<float>(maxPathSize, Allocator.Temp);
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
                    maxPathSize
                );

                if (returningStatus == PathQueryStatus.Success)
                {
                    // waypointBuffer.Clear();
                    ECB.SetBuffer<WaypointBuffer>(Entity);
                    foreach (var location in result)
                    {
                        if (location.position != Vector3.zero)
                        {
                            //waypointBuffer.Add(new WaypointBuffer { wayPoint = location.position });
                            ECB.AppendToBuffer(Entity, new WaypointBuffer
                            {
                                WayPoint = location.position,
                            });
                        }
                    }

                    NavAgent.CurrentWaypoint = 0;
                    NavAgent.PathCalculated = true;
                    ECB.SetComponent(Entity, NavAgent);
                }

                straightPathFlag.Dispose();
                polygonIds.Dispose();
                vertexSide.Dispose();
            }
        }
    }
}