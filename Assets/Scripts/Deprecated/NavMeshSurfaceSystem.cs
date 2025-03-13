// using Unity.Entities;
// using UnityEngine;
// using Unity.AI.Navigation;
// using UnityEngine.AI;
//
// namespace SparFlame.GamePlaySystem.Movement
// {
//     [UpdateInGroup(
//         typeof(InitializationSystemGroup))] 
//     public partial struct NavSurfaceSpawnSystem : ISystem
//     {
//         private int _count;
//
//         public void OnCreate(ref SystemState state)
//         {
//         }
//
//         public void OnDestroy(ref SystemState state)
//         {
//         }
//
//         public void OnUpdate(ref SystemState state)
//         {
//             if(_count > 0)return;
//
//             foreach (var (surface, e) in SystemAPI.Query<NavMeshDataComponent>().WithEntityAccess())
//             {
//                 _count++;
//                 NavMesh.AddNavMeshData(surface.Data);
//             }
//         }
//     }
// }