// using System;
// using System.Collections.Generic;
// using Sirenix.OdinInspector;
// using Unity.Mathematics;
// using UnityEngine;
//
// namespace SparFlame.GamePlaySystem.CustomParticleSystem.LightLine
// {
//     public class LightLineController : MonoBehaviour
//     {
//         [AssetsOnly]
//         public GameObject lightLinePrefab;
//         public float lightLineHeight ;
//
//         public static LightLineController Instance;
//         public readonly Dictionary<(float3, float3), GameObject> CurrentLines = new();
//
//         private void Awake()
//         {
//             if (Instance == null)
//                 Instance = this;
//             else
//                 Destroy(gameObject);
//         }
//     }
// }