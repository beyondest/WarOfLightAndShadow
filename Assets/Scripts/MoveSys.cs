// using Unity.Burst;
// using Unity.Entities;
// using Unity.Physics;
// using Unity.Physics.Systems;
// using UnityEngine;
//
// namespace DefaultNamespace
// {
//     [UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
//     [UpdateAfter(typeof(PhysicsSystemGroup))]
//     public partial struct MoveSys : ISystem
//     {
//         [BurstCompile]
//         public void OnCreate(ref SystemState state)
//         {
//             state.RequireForUpdate<SimulationSingleton>();
//         }
//
//         [BurstCompile]
//         public void OnUpdate(ref SystemState state)
//         {
//             new TriggerEventsTest
//             {
//
//             }.ScheduleParallel();
//         }
//
//         [BurstCompile]
//         public void OnDestroy(ref SystemState state)
//         {
//
//         }
//
//         private partial struct TriggerEventsTest : IJobEntity
//         {
//             public void Execute(ref DynamicBuffer<StatefulTriggerEvent> triggerEvents, Entity entity)
//             {
//                 if (triggerEvents.Length > 0)
//                 {
//                     Debug.Log($"Trigger A {entity}");
//                     Debug.Log($"Trigger counts {triggerEvents.Length}");
//                 }
//
//             }
//         }
//     }
// }