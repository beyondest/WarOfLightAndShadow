// using System;
// using System.Collections.Generic;
// using SparFlame.Components.General;
// using Unity.Entities;
// using UnityEngine;
//
// namespace SparFlame.Systems.General.Resource
// {
//     public class RegeneratingSystemAuthoring : MonoBehaviour
//     {
//         public float regeneratingTimeScale = 1.0f;
//         public List<RenewableResourceType> renewableResourceTypes;
//         private class RegeneratingSystemAuthoringBaker : Baker<RegeneratingSystemAuthoring>
//         {
//             public override void Bake(RegeneratingSystemAuthoring authoring)
//             {
//                 var entity = GetEntity(TransformUsageFlags.None);
//                 AddComponent(entity, new RegeneratingSystemConfig
//                 {
//                     RegeneratingTimeScale = authoring.regeneratingTimeScale,
//                 });
//                 var buffer = AddBuffer<RenewableResourceType>(entity);
//                 foreach (var type in authoring.renewableResourceTypes)
//                 {
//                     buffer.Add(type);
//                 }
//             }
//         }
//     }
//
//     public struct RegeneratingSystemConfig : IComponentData
//     {
//         public float RegeneratingTimeScale;
//     }
//
//     [Serializable]
//     public struct RenewableResourceType : IBufferElementData
//     {
//         public ResourceType resourceType;
//     }
//     
// }