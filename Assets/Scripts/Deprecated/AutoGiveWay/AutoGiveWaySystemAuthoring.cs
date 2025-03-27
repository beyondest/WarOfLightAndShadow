// using UnityEngine;
// using Unity.Entities;
// using Unity.Mathematics;
// using Unity.Physics.Authoring;
// using UnityEngine.Serialization;
//
// namespace SparFlame.GamePlaySystem.Movement
// {
//     public class AutoGiveWaySystemAuthoring : MonoBehaviour
//     {
//         [Tooltip("This duration controls how long it takes to auto give way in one direction")]
//         public float duration = 1f;
//         
//         [Tooltip("This ratio * being squeezed max collider shapeXz is the detect collider radius")]
//         public float squeezeColliderDetectionRatio = 1.5f;
//
//         private class Baker : Baker<AutoGiveWaySystemAuthoring>
//         {
//             public override void Bake(AutoGiveWaySystemAuthoring authoring)
//             {
//                 var entity = GetEntity(TransformUsageFlags.None);
//                 AddComponent(entity, new AutoGiveWaySystemConfig
//                 {
//                     Duration = authoring.duration,
//                     SqueezeColliderDetectionRatio = authoring.squeezeColliderDetectionRatio
//                 });
//             }
//         }
//     }
//
//     public struct AutoGiveWaySystemConfig : IComponentData
//     {
//         public float Duration;
//         public float SqueezeColliderDetectionRatio;
//     }
//
//
//     public struct AutoGiveWayData : IComponentData
//     {
//         public float3 MoveVector;
//         public float ElapsedTime;
//         public bool IfGoBack;
//     }
//
//     public struct SqueezeData : IComponentData
//     {
//         public float3 MoveVector;
//     }
//     
// }