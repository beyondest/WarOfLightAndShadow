// using SparFlame.Components.General;
// using SparFlame.Components.SubGameplay;
// using Unity.Burst;
// using Unity.Collections;
// using Unity.Entities;
// using Unity.Mathematics;
// using Unity.Physics;
// using Unity.Transforms;
//
// namespace SparFlame.Systems.SubGameplay.Movement
// {
//     public partial struct GetGroundNormalSystem : ISystem
//     {
//         [BurstCompile]
//         public void OnCreate(ref SystemState state)
//         {
//             state.RequireForUpdate<GetGroundNormalConfig>();
//             state.RequireForUpdate<PhysicsWorldSingleton>();
//             state.RequireForUpdate<GroundInfo>();
//             state.RequireForUpdate<SubGamingTag>();
//         }
//
//         [BurstCompile]
//         public void OnUpdate(ref SystemState state)
//         {
//             state.Dependency = new GetGroundNormalJob
//             {
//                 PhysicsWorld = SystemAPI.GetSingleton<PhysicsWorldSingleton>(),
//                 Config = SystemAPI.GetSingleton<GetGroundNormalConfig>(),
//             }.ScheduleParallel(state.Dependency);
//         }
//
//     }
//
//
// }