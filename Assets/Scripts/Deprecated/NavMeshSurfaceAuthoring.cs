//
// // This code is not needed anymore, cause navmesh data can be baked in main scene
//
// using Unity.AI.Navigation;
// using Unity.Entities;
// using UnityEngine;
// using UnityEngine.AI;
//
//
// namespace SparFlame.GamePlaySystem.Movement
// {
//     public class NavMeshSurfaceAuthoring : MonoBehaviour
//     {
//         
//         public class Baker : Baker<NavMeshSurfaceAuthoring>
//         {
//             public override void Bake(NavMeshSurfaceAuthoring authoring) 
//             {
//                 var entity = GetEntity(TransformUsageFlags.WorldSpace);
//                 if (!authoring.TryGetComponent(out NavMeshSurface surface))
//                 {
//                     Debug.LogError("NavMeshSurfaceAuthoring need NavMeshSurface component");
//                     return;
//                 }
//                 surface.BuildNavMesh();
//                 Debug.Log("Baked NavMesh");
//                 AddComponentObject(entity, new NavMeshDataComponent
//                 {
//                     Data = surface.navMeshData,
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