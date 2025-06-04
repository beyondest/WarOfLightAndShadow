// using System;
// using System.Collections.Generic;
// using Unity.Entities;
// using UnityEngine;
//
// namespace SparFlame.GamePlaySystem.Interact.GamePlaySystem.Functionality.Interact.Buff.Authoring
// {
//     public class LightCavalryBuffAuthoring : MonoBehaviour
//     {
//         [Serializable]
//         public struct Pair
//         {
//             public float lastDuration;
//             public float reduceCurrentHpRatio;
//         }
//         
//         public List<Pair> reduceCurrentHpRatios; 
//         private class LightCavalryBuffAuthoringBaker : Baker<LightCavalryBuffAuthoring>
//         {
//             public override void Bake(LightCavalryBuffAuthoring authoring)
//             {
//                 var entity = GetEntity(TransformUsageFlags.None);
//                 var buffer = AddBuffer<LightCavalryBuffConfigs>(entity);
//                 foreach (var duration in authoring.reduceCurrentHpRatios)
//                 {
//                     buffer.Add(new LightCavalryBuffConfigs
//                     {
//                         LastDuration = duration.lastDuration,
//                         ReduceCurrentHpRatio = duration.reduceCurrentHpRatio
//                     });
//                 }
//             }
//         }
//     }
//
//     public struct LightCavalryBuffConfigs : IBufferElementData
//     {
//         public float LastDuration;
//         public float ReduceCurrentHpRatio;
//     }
//
//     public struct LightCavalryBuffData : IComponentData
//     {
//         public float LastDuration;
//         public float ReduceCurrentHpRatio;
//         public float StopTime;
//     }
// }