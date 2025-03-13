//
// // This code is not needed anymore, cause navmesh data can be baked in main scene
// using Unity.Entities;
// using UnityEngine;
// using UnityEngine.AI;
//
//
// namespace SparFlame.GamePlaySystem.Movement
// {
//     public class NavMeshSurfaceAuthoring : MonoBehaviour
//     {
//         public NavMeshData data;
//         public class Baker : Baker<NavMeshSurfaceAuthoring>
//         {
//             public override void Bake(NavMeshSurfaceAuthoring authoring) 
//             {
//                 var entity = GetEntity(TransformUsageFlags.Dynamic);
//                 AddComponentObject(entity, new NavMeshDataComponent
//                 {
//                     Data = authoring.data,
//                 });
//             }
//         }
//     }
//
//     public class NavMeshDataComponent : IComponentData
//     {
//         public NavMeshData Data;
//     }
// }