// using System;
// using System.Threading;
// using Unity.Jobs;
// using UnityEngine;
//
// namespace SparFlame.Test.Interview
// {
//     public class oo : MonoBehaviour
//     {
//         
//         private static int _sharedCounter;
//
//         private void Update()
//         {
//             Interlocked.CompareExchange()
//         }
//
//         public struct JobThatIncrementsCounter : IJob
//         {
//             public void Execute()
//             {
//                 Interlocked.Increment(ref _sharedCounter);
//                 lock ()
//                 {
//                     
//                 }
//             }
//         }
//     }
// }