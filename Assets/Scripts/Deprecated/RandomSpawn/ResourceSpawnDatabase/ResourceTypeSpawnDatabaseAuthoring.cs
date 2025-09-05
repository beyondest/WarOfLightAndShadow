// using System;
// using System.Collections.Generic;
// using System.Linq;
// using SparFlame.GamePlaySystem.Resource;
// using Unity.Collections;
// using Unity.Entities;
// using UnityEngine;
//
// namespace SparFlame.Database
// {
//     public class ResourceTypeSpawnDatabaseAuthoring : MonoBehaviour
//     {
//         private class ResourceTypeSpawnDatabaseAuthoringBaker : Baker<ResourceTypeSpawnDatabaseAuthoring>
//         {
//             public override void Bake(ResourceTypeSpawnDatabaseAuthoring authoring)
//             {
//                 var entity = GetEntity(TransformUsageFlags.None);
//                 // var buffer = AddBuffer<ResourceTileTypeSpecialData>(entity);
//                 // var items = DatabaseManager.ResourceTypeSpawnDatabaseSo.items;
//                 // var dict = new Dictionary<ResourceType, ResourceTileTypeSpecialData>();
//                 // foreach (var data in items)
//                 // {
//                 //     var fix = new FixedList128Bytes<TileTypeToSpawnWeight>();
//                 //     foreach (var weight in data.spawnableTiles)
//                 //     {
//                 //         fix.Add(weight);
//                 //     }
//                 //     var d = new ResourceTileTypeSpecialData
//                 //     {
//                 //         Type = data.type,
//                 //         SpawnableTiles = fix
//                 //     };
//                 //     if (!dict.TryAdd(data.type, d))
//                 //     {
//                 //         throw new ArgumentException($"Resource type duplicated in resource type spawn database. {data.type}");
//                 //     }
//                 // }
//                 // foreach (var type in Enum.GetValues(typeof(ResourceType)).OfType<ResourceType>())
//                 // {
//                 //     var newD = new ResourceTileTypeSpecialData
//                 //     {
//                 //         Type = type,
//                 //         SpawnableTiles = new FixedList128Bytes<TileTypeToSpawnWeight>()
//                 //     };
//                 //     var d = dict.ContainsKey(type) ? dict[type] : newD;
//                 //     buffer.Add(d);
//                 // }
//             }
//         }
//     }
// }