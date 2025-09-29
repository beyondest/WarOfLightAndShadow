// using SparFlame.Components.SubGameplay;
// using Unity.Entities;
// using UnityEngine;
//
// namespace SparFlame.Systems.SubGameplay.EnemyAI
// {
//     public class PlayerUnitOutsideMonitorAuthoring : MonoBehaviour
//     {
//         public float outsideDisThreshold;
//         private class PlayerUnitOutsideMonitorAuthoringBaker : Baker<PlayerUnitOutsideMonitorAuthoring>
//         {
//             public override void Bake(PlayerUnitOutsideMonitorAuthoring authoring)
//             {
//                 var entity = GetEntity(TransformUsageFlags.None);
//                 AddComponent(entity, new PlayerUnitOutsideMonitorConfig
//                 {
//                     OutSideDisThresholdSq = authoring.outsideDisThreshold * authoring.outsideDisThreshold,
//                 });
//             }
//         }
//     }
//
// }