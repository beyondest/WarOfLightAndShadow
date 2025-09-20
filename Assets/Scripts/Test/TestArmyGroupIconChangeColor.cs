// using Unity.Burst;
// using Unity.Entities;
// using UnityEngine;
//
// namespace SparFlame.Test
// {
//     public partial struct TestArmyGroupIconChangeColor : ISystem
//     {
//         public void OnCreate(ref SystemState state)
//         {
//             
//         }
//
//         public void OnUpdate(ref SystemState state)
//         {
//             if (Input.GetKeyDown(KeyCode.Space))
//             {
//                 Debug.Log("Pressed space");
//                 foreach (var (_, entity) in SystemAPI.Query<RefRO<TestTag>>().WithEntityAccess())
//                 {
//                     var renderer = SystemAPI.ManagedAPI.GetComponent<ParticleSystemRenderer>(entity);
//                     renderer.material.SetColor("_BaseColor", Color.red);
//                     
//                 }
//             }
//             if (Input.GetKeyDown(KeyCode.K))
//             {
//                 Debug.Log("Pressed k");
//                 foreach (var (_, entity) in SystemAPI.Query<RefRO<TestTag>>().WithEntityAccess())
//                 {
//                     var renderer = SystemAPI.ManagedAPI.GetComponent<ParticleSystemRenderer>(entity);
//                     renderer.material.SetColor("_BaseColor", Color.blue);
//                 }
//             }
//          
//         }
//
//         public void OnDestroy(ref SystemState state)
//         {
//
//         }
//     }
// }