// using System;
// using System.Collections.Generic;
// using Unity.Entities;
// using UnityEngine;
//
// namespace SparFlame.GamePlaySystem.Interact.GamePlaySystem.Functionality.Interact.Buff.Authoring
// {
//     public class BlessingBuffAuthoring : MonoBehaviour
//     {
//         [Serializable]
//         public struct Pair
//         {
//             public float lastDuration;
//             public float reduceCurrentHpRatio;
//         }
//         
//         public List<Pair> reduceCurrentHpRatios; 
//         private class BlessingBuffAuthoringBaker : Baker<BlessingBuffAuthoring>
//         {
//             public override void Bake(BlessingBuffAuthoring authoring)
//             {
//                 var entity = GetEntity(TransformUsageFlags.None);
//                 var buffer = AddBuffer<BlessingBuffConfigs>(entity);
//                 foreach (var duration in authoring.reduceCurrentHpRatios)
//                 {
//                     buffer.Add(new BlessingBuffConfigs
//                     {
//                         LastDuration = duration.lastDuration,
//                         ReduceCurrentHpRatio = duration.reduceCurrentHpRatio
//                     });
//                 }
//             }
//         }
//     }
//
//     public struct BlessingBuffConfigs : IBufferElementData
//     {
//         public float LastDuration;
//         public float ReduceCurrentHpRatio;
//     }
//
//     public struct BlessingBuffData : IComponentData
//     {
//         public float LastDuration;
//         public float ReduceCurrentHpRatio;
//         public float StopTime;
//     }
// }