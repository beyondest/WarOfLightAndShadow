// using System;
// using UnityEngine;
// using Unity.AI.Navigation;
// using Unity.Entities;
// using UnityEngine.AI;
//
// namespace SparFlame.GamePlaySystem.Movement
// {
//     public class AddNavMesh : MonoBehaviour
//     {
//         private EntityManager _em;
//
//         private void OnEnable()
//         {
//             _em = World.DefaultGameObjectInjectionWorld.EntityManager;
//         }
//
//         private void Start()
//         {
//             if (!_em.CreateEntityQuery(typeof(NavSurfaceSpawn)).TryGetSingleton<NavSurfaceSpawn>(out var surface))
//             {
//                 return;
//             }
//             NavMesh.AddNavMeshData(surface.Surface.navMeshData);
//         }
//     }
// }