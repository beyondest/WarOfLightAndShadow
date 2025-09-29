// using Sirenix.OdinInspector;
// using SparFlame.Components.SubGameplay;
// using Unity.Entities;
// using UnityEngine;
//
// namespace SparFlame.Systems.SubGameplay.EnemyAI
// {
//     public class SurroundingMonitorAuthoring : MonoBehaviour
//     {
//         [AssetsOnly]
//         public GameObject lightMonitorPrefab;
//         [AssetsOnly]
//         public GameObject darkMonitorPrefab;
//
//         private class Baker : Baker<
//             SurroundingMonitorAuthoring>
//         {
//             public override void Bake(SurroundingMonitorAuthoring authoring)
//             {
//                 var entity = GetEntity(TransformUsageFlags.None);
//                 AddComponent(entity, new MonitorPrefabData
//                 {
//                     LightMonitorPrefab = GetEntity(authoring.lightMonitorPrefab, TransformUsageFlags.Dynamic),
//                     DarkMonitorPrefab = GetEntity(authoring.darkMonitorPrefab, TransformUsageFlags.Dynamic),
//                 });
//             }
//         }
//     }
//
//
//     
// }