// using System.Collections.Generic;
// using SparFlame.GamePlaySystem.Animation.GamePlaySystem.Core.Animation;
// using SparFlame.GamePlaySystem.State;
// using Unity.Collections;
// using Unity.Entities;
// using UnityEngine;
//
//
//
// namespace SparFlame.GamePlaySystem.Animation
// {
//
//     [UpdateInGroup(typeof(SimulationSystemGroup))]
//     public partial class AnimationEventHandlerSystem : SystemBase
//     {
//         private EntityQuery _eventQuery;
//         
//         private Dictionary<int, EventHandlerDelegate> _eventDict;
//         
//         protected override void OnCreate()
//         {
//             RequireForUpdate<EndSimulationEntityCommandBufferSystem.Singleton>();
//             _eventDict = new Dictionary<int, EventHandlerDelegate>(10);
//             _eventQuery = SystemAPI.QueryBuilder().WithAll<AnimationEventData>().Build();
//         }
//
//         protected override void OnStartRunning()
//         {
//             _eventDict.Add(AnimationUtils.Hash("LogTest"), TestPrint);
//         }
//
//         protected override void OnUpdate()
//         {
//             var entities = _eventQuery.ToEntityArray(Allocator.Temp);
//             var ecb = new EntityCommandBuffer(Allocator.Temp);
//             foreach (var entity in entities)
//             {
//                 var buffer = SystemAPI.GetBuffer<AnimationEventData>(entity);
//                 if (buffer.Length == 0) continue;
//                 foreach (var evt in buffer)
//                 {
//                     if (_eventDict.TryGetValue(evt.NameHash, out var handler))
//                     {
//                         handler.Invoke(entity, evt.Parameter, ecb);
//                     }
//                 }
//                 buffer.Clear();
//             }
//             ecb.Playback(EntityManager);
//             ecb.Dispose();
//
//         }
//
//
//         private delegate void EventHandlerDelegate(Entity entity, int parameter, EntityCommandBuffer ecb);
//
//
//         private void TestPrint(Entity entity, int value, EntityCommandBuffer ecb)
//         {
//             Debug.Log($"Int : {value}");
//         }
//
//         
//     }
// }
//
//
// // Cannot work in IjobEntity, maybe the dictionary managed type
//     
// // public partial struct CheckAnimationEvent : IJobEntity
// // {
// //     [ReadOnly] public Dictionary<int, EventHandlerDelegate> EventDict;
// //     public EntityCommandBuffer.ParallelWriter ECB;
// //     private void Execute([ChunkIndexInQuery] int index, ref DynamicBuffer<AnimationEventRequest> events, Entity entity)
// //     {
// //         if(events.Length == 0)return;
// //         foreach (var evt in events)
// //         {
// //             
// //                 // handler.Invoke(entity, evt.Parameter, ECB);
// //             Debug.Log($"NameHash : {evt.NameHash}, value : {evt.Parameter}");
// //         }
// //         ECB.SetBuffer<AnimationEventRequest>(index, entity);
// //
// //     }
// // }