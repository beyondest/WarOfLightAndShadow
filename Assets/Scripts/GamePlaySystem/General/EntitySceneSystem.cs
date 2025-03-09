// TODO Subscene is currently loaded via autoloaded mode to load with parent scene, you may need subscene load management in the future

// using Unity.Burst;
// using Unity.Entities;
// using Unity.Scenes;
// namespace SparFlame.BootStrapper
// {
//     public partial struct EntitySceneSystem : ISystem
//     {
//         [BurstCompile]
//         public void OnCreate(ref SystemState state)
//         {
//             state.RequireForUpdate<LoadEntitySceneRequest>();
//         }
//
//         [BurstCompile]
//         public void OnUpdate(ref SystemState state)
//         {
//
//         }
//
//         [BurstCompile]
//         public void OnDestroy(ref SystemState state)
//         {
//
//         }
//         
//         // private void LoadScene(ref SystemState state)
//         
//         
//     }
// }