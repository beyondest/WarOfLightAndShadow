// using Unity.Burst;
// using Unity.Collections;
// using Unity.Entities;
// using Unity.Physics;
// using UnityEngine;
// using BoxCollider = Unity.Physics.BoxCollider;
//
// namespace DefaultNamespace
// {
//     public partial struct tt : ISystem
//     {
//         
//
//         [BurstCompile]
//         public void OnUpdate(ref SystemState state)
//         {
//             var ecb = new EntityCommandBuffer(Allocator.Temp);
//             foreach (var (reqRo, collider, entity) in SystemAPI.Query<RefRO<GhostRequest>, RefRW<PhysicsCollider>>()
//                          .WithEntityAccess())
//             {
//                 var request = reqRo.ValueRO;
//                 Debug.Log($"Entity : {request.Ghost}");
//                 unsafe
//                 {
//                     var bxPtr = (BoxCollider*)collider.ValueRW.ColliderPtr;
//                     var box = bxPtr->Geometry;
//                     box.Size = request.NewSize;
//                     bxPtr->Geometry = box;
//                 }
//                 ecb.RemoveComponent<GhostRequest>(entity);
//             }
//             ecb.Playback(state.EntityManager);
//             ecb.Dispose();
//         }
//
//         
//     }
// }