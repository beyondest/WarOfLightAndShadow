// using SparFlame.Components.SubGameplay;
// using Unity.Entities;
// using Unity.Mathematics;
// using UnityEngine;
//
// namespace SparFlame.Systems.SubGameplay.EnemyAI
// {
//     public class EnemyBasePosDataAuthoring : MonoBehaviour
//     {
//         
//         // public List<TeamTypeToAssembleLocRef> teamTypeToAssembles;
//         public GameObject fallBackPosRef;
//         public float defenseRadius;
//         
//         private class Baker : Baker<EnemyBasePosDataAuthoring>
//         {
//             public override void Bake(EnemyBasePosDataAuthoring authoring)
//             {
//                 float3 baseWorldPos = authoring.transform.position;
//                 var entity = GetEntity(TransformUsageFlags.None);
//                 AddComponent(entity, new EnemyBasePosData
//                 {
//                     FallBackPosBias =(float3) authoring.fallBackPosRef.transform.position - baseWorldPos,
//                     DefenseRadius = authoring.defenseRadius
//                 });
//                 
//                 // Bake assembleLocs bias
//                 // var buffer = AddBuffer<EnemyBaseAssembleLocs>(entity);
//                 // var count = 0;
//                 // foreach (var entry in authoring.teamTypeToAssembles)
//                 // {
//                 //     if (entry.teamType != (AITeamType)count)
//                 //         throw new ArgumentException("Team type must be in enum type order");
//                 //     count++;
//                 //     if (entry.locationRef == null)
//                 //         continue;
//                 //     float3 targetPos = entry.locationRef.transform.position;
//                 //     var offset = targetPos - baseWorldPos;
//                 //     buffer.Add(new EnemyBaseAssembleLocs
//                 //     {
//                 //         TeamAssembleLocationBias = offset
//                 //     });
//                 // }
//
//             }
//         }
//     }
//
//
//     
// }