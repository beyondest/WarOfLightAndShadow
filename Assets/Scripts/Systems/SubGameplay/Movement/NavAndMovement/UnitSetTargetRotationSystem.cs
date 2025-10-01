// using SparFlame.Components.General;
// using SparFlame.Components.SubGameplay;
// using Unity.Burst;
// using Unity.Collections;
// using Unity.Entities;
// using Unity.Jobs;
// using Unity.Mathematics;
// using Unity.Transforms;
// using UnityEngine;
// using UnityEngine.Experimental.AI;
//
// #pragma warning disable CS0618 // Type or member is obsolete
//
// namespace SparFlame.Systems.SubGameplay.Movement
// {
//     public partial struct UnitSetTargetRotationSystem : ISystem
//     {
//         private NavMeshWorld _navMeshWorld;
//         private NativeList<NavMeshQuery> _navMeshQueries;
//         private EntityQuery _entityQuery;
//
//         [BurstCompile]
//         public void OnCreate(ref SystemState state)
//         {
//             state.RequireForUpdate<EndSimulationEntityCommandBufferSystem.Singleton>();
//             state.RequireForUpdate<CalculateRotationConfig>();
//             state.RequireForUpdate<NavAgentSystemConfig>();
//             state.RequireForUpdate<SubGamingTag>();
//             _entityQuery = SystemAPI.QueryBuilder().WithAll<LocalTransform>().WithAll<Velocity>()
//                 .WithAll<NavAgentComponent>().WithAllRW<TargetRotation>().Build();
//         }
//
//         [BurstCompile]
//         public void OnUpdate(ref SystemState state)
//         {
//             var config = SystemAPI.GetSingleton<NavAgentSystemConfig>();
//             if (!_navMeshQueries.IsCreated)
//                 InitNavMeshQueries(config);
//             if (_entityQuery.IsEmpty) return;
//
//             var entities = _entityQuery.ToEntityArray(Allocator.TempJob);
//             if (entities.Length > _navMeshQueries.Length)
//             {
//                 ExtendNavMeshQueries(entities.Length - _navMeshQueries.Length, config);
//             }
//
//             var jobHandles = new NativeArray<JobHandle>(entities.Length, Allocator.TempJob);
//             var localTransforms = _entityQuery.ToComponentDataArray<LocalTransform>(Allocator.TempJob);
//             var navAgents = _entityQuery.ToComponentDataArray<NavAgentComponent>(Allocator.TempJob);
//             var velocity = _entityQuery.ToComponentDataArray<Velocity>(Allocator.TempJob);
//
//             var calConfig = SystemAPI.GetSingleton<CalculateRotationConfig>();
//             var ecbSingleton = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>();
//             for (int i = 0; i < entities.Length; i++)
//             {
//                 var job = new CalculateTargetRotationJob
//                 {
//                     ECB = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged),
//
//                     Config = calConfig,
//                     NavAgent = navAgents[i],
//                     Velocity = velocity[i],
//                     Transform = localTransforms[i],
//                     Query = _navMeshQueries[i],
//                     Entity = entities[i]
//                 };
//                 jobHandles[i] = job.Schedule();
//             }
//
//             state.Dependency = JobHandle.CombineDependencies(jobHandles);
//
//             entities.Dispose(state.Dependency);
//             localTransforms.Dispose(state.Dependency);
//             navAgents.Dispose(state.Dependency);
//             velocity.Dispose(state.Dependency);
//             jobHandles.Dispose(state.Dependency);
//         }
//
//
//         [BurstCompile]
//         public void OnDestroy(ref SystemState state)
//         {
//             if (_navMeshQueries.IsCreated)
//             {
//                 DisposeNavMeshQueries();
//             }
//         }
//
//         private void ExtendNavMeshQueries(int extendSize, in NavAgentSystemConfig config)
//         {
//             for (var i = 0; i < extendSize; i++)
//             {
//                 _navMeshQueries.Add(new NavMeshQuery(_navMeshWorld, Allocator.Persistent, config.PathNodePoolSize));
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
//         private void InitNavMeshQueries(in NavAgentSystemConfig config)
//         {
//             _navMeshWorld = NavMeshWorld.GetDefaultWorld();
//             _navMeshQueries = new NativeList<NavMeshQuery>(config.InitialNavMeshQueriesCapacity, Allocator.Persistent);
//             for (int i = 0; i < config.InitialNavMeshQueriesCapacity; i++)
//             {
//                 _navMeshQueries.Add(new NavMeshQuery(_navMeshWorld, Allocator.Persistent, config.PathNodePoolSize));
//             }
//         }
//     }
//
//
//     [BurstCompile]
//     public struct CalculateTargetRotationJob : IJob
//     {
//         public EntityCommandBuffer ECB;
//         public Entity Entity;
//         public NavMeshQuery Query;
//         [ReadOnly] public CalculateRotationConfig Config;
//         [ReadOnly] public NavAgentComponent NavAgent;
//         [ReadOnly] public LocalTransform Transform;
//         [ReadOnly] public Velocity Velocity;
//
//         public void Execute()
//         {
//             var targetRotation = new TargetRotation
//             {
//                 Value = Transform.Rotation
//             };
//
//             if (math.lengthsq(Velocity.Value) > 0.001f)
//             {
//                 var vertices = new NativeArray<Vector3>(Config.VerticesPreparationCount, Allocator.Temp);
//                 var neighbors = new NativeArray<PolygonId>(Config.NeighborsPreparationCount, Allocator.Temp);
//                 var edgeIndices = new NativeArray<byte>(neighbors.Length, Allocator.Temp);
//                 var location = Query.MapLocation(Transform.Position, Config.ExtentsForNormal, NavAgent.agentId);
//                 var queryStatus = Query.GetEdgesAndNeighbors(
//                     location.polygon, vertices, neighbors, edgeIndices,
//                     out var totalVertices, out _);
//                 if ((queryStatus & PathQueryStatus.Success) != 0 && totalVertices >= 3)
//                 {
//                     var p0 = (float3)vertices[0];
//                     var p1 = (float3)vertices[1];
//                     var p2 = (float3)vertices[2];
//
//                     // float3 groundNormal = hit.normal;
//                     float3 groundNormal = math.normalizesafe(math.cross(p1 - p0, p2 - p0));
//                     ECB.SetComponent(Entity, new GroundNormal{Value = groundNormal});
//                     float3 forward = math.normalizesafe(Velocity.Value);
//                     forward = Config.value1 ? -forward : forward;
//
//                     float3 right = math.normalizesafe(math.cross(groundNormal, forward));
//                     forward = math.cross(right, groundNormal);
//                     forward = Config.value2 ? -forward : forward;
//
//                     targetRotation.Value = quaternion.LookRotationSafe(forward, groundNormal);
//                 }
//                 else
//                 {
//                     var dir = math.normalizesafe(Velocity.Value);
//                     targetRotation.Value = quaternion.LookRotationSafe(-dir, math.up());
//                 }
//             }
//
//             ECB.SetComponent(Entity, targetRotation);
//         }
//     }
// }