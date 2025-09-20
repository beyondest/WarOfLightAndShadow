// using System;
// using System.IO;
// using System.Collections.Generic;
// using System.Threading.Tasks;
// using Unity.Collections;
// using Unity.Collections.LowLevel.Unsafe;
// using Unity.Entities;
// using Unity.Jobs;
// using Unity.Burst;
// using UnityEngine;
//
// namespace SparFlame.Test
// {
//     // 标记可保存组件
//     public interface ISavableComponent : IComponentData {}
//
//     public struct HpComponent : IComponentData, ISavableComponent { public int Value; }
//     public struct ExpComponent : IComponentData, ISavableComponent { public int Exp; }
//
//     // 标记需要保存的 entity
//     public struct SavingTag : IComponentData {}
//
//     // prefab 对应的全局 ID
//     public struct PrefabId : IComponentData { public int Value; }
//
//     // 每个 entity 的序列化数据
//     public struct EntitySaveData
//     {
//         public int PrefabId;
//         public NativeList<byte> ComponentData; // 所有可保存组件的二进制
//     }
//
//     public partial class SaveSystem : SystemBase
//     {
//         // Prefab库，用于Load时实例化
//         public Dictionary<int, Entity> PrefabRegistry = new();
//
//         // 保存目录
//         private string savePath = Path.Combine(Application.persistentDataPath, "save.dat");
//
//         protected override void OnUpdate()
//         {
//             if (Input.GetKeyDown(KeyCode.S))
//             {
//                 _ = SaveAsync();
//             }
//
//             if (Input.GetKeyDown(KeyCode.L))
//             {
//                 _ = LoadAsync();
//             }
//         }
//
//         /// <summary>
//         /// 异步保存
//         /// </summary>
//         private async Task SaveAsync()
//         {
//             // 找到所有带 SavingTag 的 entity
//             var query = SystemAPI.QueryBuilder().WithAll<SavingTag, PrefabId>().Build();
//
//             var entities = query.ToEntityArray(Allocator.TempJob);
//             var prefabIds = query.ToComponentDataArray<PrefabId>(Allocator.TempJob);
//
//             var allSaveData = new NativeList<EntitySaveData>(Allocator.TempJob);
//
//             // 并行任务：每个 entity 都转成 NativeList<byte>
//             await Task.Run(() =>
//             {
//                 for (int i = 0; i < entities.Length; i++)
//                 {
//                     var e = entities[i];
//                     var prefabId = prefabIds[i].Value;
//
//                     var bytes = new NativeList<byte>(Allocator.Temp);
//
//                     // 遍历所有可保存组件
//                     var archetype = EntityManager.GetChunk(e).Archetype;
//                     var types = archetype.GetComponentTypes(Allocator.Temp);
//                     foreach (var type in types)
//                     {
//                         if (typeof(ISavableComponent).IsAssignableFrom(type.GetManagedType()))
//                         {
//                             // 获取组件数据
//                             var size = UnsafeUtility.SizeOf(type.GetManagedType());
//                             var ptr = EntityManager.GetComponentDataRawRO(e, type);
//                             unsafe
//                             {
//                                 bytes.ResizeUninitialized(bytes.Length + size);
//                                 UnsafeUtility.MemCpy(
//                                     (byte*)bytes.GetUnsafePtr() + bytes.Length - size,
//                                     ptr.ToPointer(),
//                                     size);
//                             }
//                         }
//                     }
//
//                     allSaveData.Add(new EntitySaveData
//                     {
//                         PrefabId = prefabId,
//                         ComponentData = bytes
//                     });
//                 }
//             });
//
//             // 将 NativeList<byte> 转 byte[] 写入文件
//             using (var fs = new FileStream(savePath, FileMode.Create, FileAccess.Write, FileShare.None))
//             {
//                 foreach (var data in allSaveData)
//                 {
//                     // 写 PrefabId
//                     var prefabBytes = BitConverter.GetBytes(data.PrefabId);
//                     await fs.WriteAsync(prefabBytes, 0, prefabBytes.Length);
//
//                     // 写组件长度
//                     var lenBytes = BitConverter.GetBytes(data.ComponentData.Length);
//                     await fs.WriteAsync(lenBytes, 0, lenBytes.Length);
//
//                     // 写组件内容
//                     var compArray = data.ComponentData.ToArray();
//                     await fs.WriteAsync(compArray, 0, compArray.Length);
//
//                     data.ComponentData.Dispose();
//                 }
//             }
//
//             entities.Dispose();
//             prefabIds.Dispose();
//             allSaveData.Dispose();
//
//             Debug.Log("Save complete!");
//         }
//
//         /// <summary>
//         /// 异步加载
//         /// </summary>
//         private async Task LoadAsync()
//         {
//             if (!File.Exists(savePath))
//             {
//                 Debug.LogWarning("Save file not found");
//                 return;
//             }
//
//             var bytes = await File.ReadAllBytesAsync(savePath);
//             int offset = 0;
//
//             while (offset < bytes.Length)
//             {
//                 // 读取 PrefabId
//                 int prefabId = BitConverter.ToInt32(bytes, offset);
//                 offset += 4;
//
//                 // 读取组件长度
//                 int compLen = BitConverter.ToInt32(bytes, offset);
//                 offset += 4;
//
//                 // 读取组件数据
//                 var compBytes = new byte[compLen];
//                 Array.Copy(bytes, offset, compBytes, 0, compLen);
//                 offset += compLen;
//
//                 // 根据 prefabId 实例化 entity
//                 if (!PrefabRegistry.TryGetValue(prefabId, out var prefab))
//                 {
//                     Debug.LogError($"Prefab {prefabId} not registered!");
//                     continue;
//                 }
//
//                 var entity = EntityManager.Instantiate(prefab);
//
//                 // 还原组件数据
//                 unsafe
//                 {
//                     fixed (byte* ptr = compBytes)
//                     {
//                         var archetype = EntityManager.GetChunk(entity).Archetype;
//                         var types = archetype.GetComponentTypes(Allocator.Temp);
//                         int byteOffset = 0;
//                         foreach (var type in types)
//                         {
//                             if (typeof(ISavableComponent).IsAssignableFrom(type.GetManagedType()))
//                             {
//                                 int size = UnsafeUtility.SizeOf(type.GetManagedType());
//                                 var dst = EntityManager.GetComponentDataRawRW(entity, type);
//                                 UnsafeUtility.MemCpy(dst.ToPointer(), ptr + byteOffset, size);
//                                 byteOffset += size;
//                             }
//                         }
//                     }
//                 }
//             }
//
//             Debug.Log("Load complete!");
//         }
//     }
// }
