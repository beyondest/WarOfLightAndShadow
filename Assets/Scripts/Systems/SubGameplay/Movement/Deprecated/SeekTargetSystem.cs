// using SparFlame.Components.General;
// using SparFlame.Components.SubGameplay;
// using Unity.Entities;
// using Unity.Burst;
// using Unity.Jobs;
// using Unity.Mathematics;
// using Unity.Physics;
// using UnityEngine;
//
// // ReSharper disable UseIndexFromEndExpression
//
//
// namespace SparFlame.Systems.SubGameplay.Movement
// {
//     [BurstCompile]
//     public partial struct SeekTargetSystem : ISystem
//     {
//         private ComponentLookup<MovingStateTag> _movingStateLookup;
//         [BurstCompile]
//         public void OnCreate(ref SystemState state)
//         {
//             state.RequireForUpdate<GameTimeData>();
//             state.RequireForUpdate<PhysicsWorldSingleton>();
//             state.RequireForUpdate<SubGamingTag>();
//             state.RequireForUpdate<MovableData>();
//             state.RequireForUpdate<MovementConfig>();
//             _movingStateLookup = state.GetComponentLookup<MovingStateTag>(true);
//         }
//
//         [BurstCompile]
//         public void OnUpdate(ref SystemState state)
//         {
//             _movingStateLookup.Update(ref state);
//             var config = SystemAPI.GetSingleton<MovementConfig>();
//             state.Dependency =  new SeekTargetJobPlus
//             {
//                 ElapsedTime = SystemAPI.GetSingleton<GameTimeData>().ElapsedTime,
//                 Config = config,
//                 MovingStateLookup = _movingStateLookup,
//             }.ScheduleParallel(state.Dependency);
//            
//         }
//     }
//
//
//     
// }