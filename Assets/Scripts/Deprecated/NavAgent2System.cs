// using System;
// using Unity.Burst;
// using Unity.Collections;
// using Unity.Entities;
// using Unity.Jobs;
// using Unity.Mathematics;
// using Unity.Transforms;
// using UnityEngine;
// using UnityEngine.Experimental.AI;
// using UnityEngine.UIElements;
//
// namespace SparFlame.GamePlaySystem.Movement
// {
//     [Obsolete("Obsolete")]
//     [BurstCompile]
//     [UpdateAfter(typeof(MovementSystem))]
//     public partial struct NavAgent2System : ISystem
//     {
//         
//         private NativeList<NavMeshQuery> _navMeshQueries;
//         private NavMeshWorld _navMeshWorld;
//             
//         private BufferLookup<WaypointBuffer> _waypointLookup;
//         private ComponentLookup<NavAgentComponent> _navAgentLookup;
//         
//         private EntityQuery _entityQuery;
//
//         [BurstCompile]
//         public void OnCreate(ref SystemState state)
//         {
//             state.RequireForUpdate<NavAgentSystemConfig>();
//             _waypointLookup = state.GetBufferLookup<WaypointBuffer>();
//             _navAgentLookup = state.GetComponentLookup<NavAgentComponent>();
//             _entityQuery = SystemAPI.QueryBuilder()
//                 .WithAllRW<NavAgentComponent>()
//                 .WithAll<MovingStateTag>()
//                 .WithAll<LocalTransform>().Build();
//         }
//
//         [BurstCompile]
//         public void OnUpdate(ref SystemState state)
//         {
//             _waypointLookup.Update(ref state);
//             _navAgentLookup.Update(ref state);
//             
//             var config = SystemAPI.GetSingletonRW<NavAgentSystemConfig>();
//             if (!config.ValueRW.IsPoolInitialized)
//             {
//                 InitNavMeshQueries(ref config.ValueRW);
//             }
//             var entities = _entityQuery.ToEntityArray(Allocator.TempJob);
//                 _entityQuery.ToComponentDataArray<NavAgentComponent>(Allocator.TempJob);
//             var localTransforms =
//                 _entityQuery.ToComponentDataArray<LocalTransform>(Allocator.TempJob);
//             if (entities.Length > _navMeshQueries.Length)
//             {
//                 ExtendNavMeshQueries(entities.Length - _navMeshQueries.Length, ref config.ValueRW);
//             }
//             
//
//             var pathFindingJob = new PathFindingJob
//             {
//                 Entities = entities,
//                 LocalTransforms = localTransforms,
//                 NavMeshQueries = _navMeshQueries,
//                 NavAgentLookup = _navAgentLookup,
//                 WaypointLookup = _waypointLookup,
//                 ElapsedTime = (float)SystemAPI.Time.ElapsedTime,
//                 Iterations = config.ValueRO.MaxIterations,
//                 MaxPathSize = config.ValueRO.MaxPathSize
//             }.Schedule(entities.Length, config.ValueRO.ParallelJobBatchSize);
//             
//             if (entities.Length < _navMeshQueries.Length)
//             {
//                 DisposeRedundantNavMeshQueries(entities.Length - _navMeshQueries.Length, ref config.ValueRW );
//             }
//             pathFindingJob.Complete();
//             entities.Dispose();
//             localTransforms.Dispose();
//         }
//
//         [BurstCompile]
//         public void OnDestroy(ref SystemState state)
//         {
//             DisposeNavMeshQueries();
//         }
//
//
//         [BurstCompile]
//         public struct PathFindingJob : IJobParallelFor
//         {
//             [ReadOnly] public NativeArray<Entity> Entities;
//             [ReadOnly] public NativeArray<LocalTransform> LocalTransforms;
//             [NativeDisableParallelForRestriction] public NativeList<NavMeshQuery> NavMeshQueries;
//             [NativeDisableParallelForRestriction] public BufferLookup<WaypointBuffer> WaypointLookup;
//             [NativeDisableParallelForRestriction] public ComponentLookup<NavAgentComponent> NavAgentLookup;
//             
//             [ReadOnly] public float ElapsedTime;
//             [ReadOnly] public int Iterations;
//             [ReadOnly] public int MaxPathSize;
//
//             public void Execute(int index)
//             {
//                 // Check if calculate
//                 var entity = Entities[index];
//                 ref var navAgent = ref NavAgentLookup.GetRefRW(entity).ValueRW;
//                 if(!navAgent.EnableCalculation)return;
//                 if(!navAgent.ForceCalculate || navAgent.NextPathCalculateTime < ElapsedTime) return;
//                 
//                 // Prepare for calculation
//                 var query = NavMeshQueries[index];
//                 var transform = LocalTransforms[index];
//                 var fromPosition = new float3(transform.Position.x, 0f, transform.Position.z);
//                 var toPosition = navAgent.TargetPosition;
//                 WaypointLookup.TryGetBuffer(entity, out var waypointBuffer);
//                 
//                 // Set Next Calculation Settings
//                 navAgent.NextPathCalculateTime = ElapsedTime + navAgent.CalculateInterval;
//                 navAgent.CalculationComplete = false;
//                 navAgent.ForceCalculate = false;
//                 
//                 // Begin finding path
//                 var fromLocation = query.MapLocation(fromPosition, navAgent.Extents, navAgent.AgentId);
//                 var toLocation = query.MapLocation(toPosition, navAgent.Extents, navAgent.AgentId);
//                 if (!query.IsValid(fromLocation) || !query.IsValid(toLocation)) return;
//                 
//                 var status = query.BeginFindPath(fromLocation, toLocation);
//                 
//                 // Notice : If target is not reachable, and extents is also not reachable, it will return Failure this step
//                 // The status only return one main status binding with a detailed status
//                 // Main Status : InProgress, Success, Failure
//                 if(status is not (PathQueryStatus.InProgress or PathQueryStatus.Success) )return;
//                 status = query.UpdateFindPath(Iterations, out _);
//                 
//                 if ((status & PathQueryStatus.Success) == 0) return;
//                 
//                 query.EndFindPath(out var pathSize);
//
//                 var result =
//                     new NativeArray<NavMeshLocation>(pathSize + 1, Allocator.Temp);
//                 var straightPathFlag =
//                     new NativeArray<StraightPathFlags>(MaxPathSize, Allocator.Temp);
//                 var vertexSide = new NativeArray<float>(MaxPathSize, Allocator.Temp);
//                 var polygonIds =
//                     new NativeArray<PolygonId>(pathSize + 1, Allocator.Temp);
//                 var straightPathCount = 0;
//                 
//                 query.GetPathResult(polygonIds);
//
//                 var returningStatus = PathUtils.FindStraightPath
//                 (
//                     query,
//                     fromPosition,
//                     toPosition,
//                     polygonIds,
//                     pathSize,
//                     ref result,
//                     ref straightPathFlag,
//                     ref vertexSide,
//                     ref straightPathCount,
//                     MaxPathSize
//                 );
//
//                 if (returningStatus == PathQueryStatus.Success)
//                 {
//                     waypointBuffer.Clear();
//                     foreach (var location in result)
//                     {
//                         if (location.position != Vector3.zero)
//                         {
//                             var newElem = new WaypointBuffer
//                             {
//                                 WayPoint = new float3(location.position.x, 0f, location.position.z),
//                             };
//                             waypointBuffer.Add(newElem);
//                         }
//                     }
//                     navAgent.CurrentWaypoint = 0;
//                     navAgent.CalculationComplete = true;
//                 }
//
//                 straightPathFlag.Dispose();
//                 polygonIds.Dispose();
//                 vertexSide.Dispose();
//             }
//         }
//         
//         
//         #region NavMeshQueriesPool
//
//         private void InitNavMeshQueries(ref NavAgentSystemConfig config)
//         {
//             config.IsPoolInitialized = true;
//             _navMeshWorld = NavMeshWorld.GetDefaultWorld();
//             _navMeshQueries = new NativeList<NavMeshQuery>(config.InitialNavMeshQueriesCapacity, Allocator.Persistent);
//             for (int i = 0; i < config.InitialNavMeshQueriesCapacity; i++)
//             {
//                 _navMeshQueries.Add(new NavMeshQuery(_navMeshWorld, Allocator.Persistent, config.PathNodePoolSize));
//             }
//         }
//
//         private void ExtendNavMeshQueries(int extendSize, ref NavAgentSystemConfig config)
//         {
//             for (int i = 0; i < extendSize; i++)
//             {
//                 _navMeshQueries.Add(new NavMeshQuery(_navMeshWorld, Allocator.Persistent, config.PathNodePoolSize));
//             }
//         }
//
//
//         private void DisposeRedundantNavMeshQueries(int redundantSize, ref NavAgentSystemConfig config)
//         {
//             var thresh = math.max(config.InitialNavMeshQueriesCapacity, _navMeshQueries.Length - redundantSize);
//             for (int i = _navMeshQueries.Length - 1; i >= thresh; i--)
//             {
//                 var query = _navMeshQueries[i];
//                 query.Dispose();
//                 _navMeshQueries.RemoveAt(i);
//             }
//         }
//
//         private void DisposeNavMeshQueries()
//         {
//             foreach (var query in _navMeshQueries)
//             {
//                 query.Dispose();
//             }
//
//             _navMeshQueries.Dispose();
//         }
//
//         #endregion
//
//     }
// }