// using Sirenix.OdinInspector;
// using Unity.Entities;
// using UnityEngine;
//
// namespace SparFlame.GamePlaySystem.Particle.LightLine
// {
//     public class LightLineSystemAuthoring : MonoBehaviour
//     {
//         public float maxLightLineDistance;
//         [AssetsOnly]
//         public GameObject lightLineSightPrefab;
//
//         public float lightLineSightInterval;
//         private class LightLineSystemAuthoringBaker : Baker<LightLineSystemAuthoring>
//         {
//             public override void Bake(LightLineSystemAuthoring authoring)
//             {
//                 var entity = GetEntity(TransformUsageFlags.None);
//                 AddComponent(entity,new LightLineConfig
//                 {
//                     LightLineMaxDisSq = authoring.maxLightLineDistance * authoring.maxLightLineDistance,
//                     SightPrefab = GetEntity(authoring.lightLineSightPrefab, TransformUsageFlags.Dynamic),
//                     SightInterval = authoring.lightLineSightInterval
//                 });
//             }
//         }
//     }
//     public struct LightLineConfig : IComponentData
//     {
//         public float LightLineMaxDisSq;
//         public Entity SightPrefab;
//         public float SightInterval;
//     }
//
//     /// <summary>
//     ///  This is the little sight around the light line
//     /// </summary>
//     public struct LightLineSightTag : IComponentData
//     {
//         
//     }
//
//     /// <summary>
//     /// With this tag, beacon cannot contribute to light line 
//     /// </summary>
//     public struct BlindnessTag : IComponentData
//     {
//         
//     }
// }