// using Unity.Burst;
// using Unity.Collections;
// using Unity.Entities;
//
// namespace SparFlame.Test
// {
//
//
//     public struct After : IComponentData
//     {
//         public int ID;
//     }
//
//     public struct After2 : IComponentData
//     {
//     }
//     public partial struct TestECB : ISystem
//     {
//         private NativeParallelHashMap<int, Entity> _map;
//         [BurstCompile]
//         public void OnCreate(ref SystemState state)
//         {
//             _map = new NativeParallelHashMap<int, Entity>(10, Allocator.Persistent);
//         }
//
//         [BurstCompile]
//         public void OnUpdate(ref SystemState state)
//         {
//             var ecb = new EntityCommandBuffer(Allocator.TempJob);
//             var job = new TestJob
//             {
//                 ECB = ecb.AsParallelWriter(),
//                 Map = _map
//             }.ScheduleParallel(state.Dependency);
//             job.Complete();
//             ecb.Playback(state.EntityManager);
//             ecb.Dispose();
//             if (!_map.IsEmpty)
//             {
//                 foreach (var pair in _map)
//                 {
//                     state.EntityManager.AddComponent<After2>(pair.Value);
//                 }
//                 _map.Clear();
//             }
//             
//         }
//
//         [BurstCompile]
//         public void OnDestroy(ref SystemState state)
//         {
//             if(_map.IsCreated)
//                 _map.Dispose();
//         }
//     }
//     [BurstCompile]
//     public partial struct TestJob : IJobEntity
//     {
//         public EntityCommandBuffer.ParallelWriter ECB;
//         [NativeDisableParallelForRestriction] public NativeParallelHashMap<int, Entity> Map;
//         private void Execute([ChunkIndexInQuery] int index, in TestTag testTag, Entity selfEntity
//         )
//         {
//             ECB.DestroyEntity(index, selfEntity);
//             var entity = ECB.CreateEntity(index);
//             ECB.AddComponent<After>(index,entity);
//             Map.Add(testTag.ID, entity);
//         }
//     }
// }