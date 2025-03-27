// using Unity.Entities;
// using Unity.Physics.Authoring;
// using UnityEngine;
//
// namespace DefaultNamespace
// {
//     public class MoveAuthor : MonoBehaviour
//     {
//         // public PhysicsCategoryNames name;
//         public PhysicsCategoryTags tags;
//         private class MoveAuthorBaker : Baker<MoveAuthor>
//         {
//             public override void Bake(MoveAuthor authoring)
//             {
//                 var a = authoring.name.CategoryNames[0];
//                 Debug.Log($"Baking {authoring.name.CategoryNames[0]}");
//                 Debug.Log($"Value : {authoring.tags.Value}");
//                 var entity = GetEntity(TransformUsageFlags.Dynamic);
//             }
//         }
//     }
//
//     
// }