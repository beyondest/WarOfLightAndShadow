// using System;
// using System.IO;
// using System.Threading.Tasks;
// using SparFlame.Components.General;
// using SparFlame.Systems.General.BasicControl;
// using Unity.Collections;
// using Unity.Collections.LowLevel.Unsafe;
// using Unity.Entities;
// using UnityEngine;
//
// namespace SparFlame.Test
// {
//     
//  
//   
//     public partial class TestSavingPlus : SystemBase
//     {
//         private const int PlayerSaveSlot = 3;
//         private event Action OnLoadComplete;
//         protected override void OnCreate()
//         {
//             RequireForUpdate<TestSavingConfig>();
//             OnLoadComplete += PrintAll;
//         }
//
//         private void PrintAll()
//         {
//             var query = SystemAPI.QueryBuilder().WithAll<TestSavingComponentA>().Build();
//             var entities = query.ToEntityArray(Allocator.TempJob);
//             Debug.Log($"load complete, find {entities.Length} TestSavingComponents");
//             entities.Dispose();
//         }
//
//         private void GenerateNeedSaveEntity()
//         {
//             var config = SystemAPI.GetSingleton<TestSavingConfig>();
//             var ecb = new EntityCommandBuffer(Allocator.Temp);
//             for (int i = 0; i < config.count; i++)
//             {
//                 var entity = ecb.CreateEntity();
//                 ecb.AddComponent(entity,new TestSavingComponentA
//                 {
//                     Value = i
//                 });
//                 ecb.AddComponent(entity, new TestSavingComponentB
//                 {
//                     Value = i * 0.1f
//                 });
//                 ecb.AddComponent<NeedSaveTag>(entity);
//                 ecb.AddBuffer<TestSavingBuffer>(entity);
//                 ecb.AppendToBuffer(entity, new TestSavingBuffer{Value = (i + 2)*3});
//                 ecb.AppendToBuffer(entity, new TestSavingBuffer{Value = (i + 2)*3});
//
//                 ecb.AddBuffer<TestSavingBuffer2>(entity);
//                 ecb.AppendToBuffer(entity, new TestSavingBuffer2{value = (i + 3)*3});
//                 ecb.AppendToBuffer(entity, new TestSavingBuffer2{value = (i + 3)*3});
//                 if (i % 2 == 0)
//                 {
//                     ecb.AddComponent(entity, new ConditionalTestSavingComponentB
//                     {
//                         Value = i * 0.2f
//                     });
//                     ecb.AddBuffer<ConditionalTestSavingBuffer2>(entity);
//                     ecb.AppendToBuffer(entity, new ConditionalTestSavingBuffer2
//                     {
//                         Value = (i + 4) * 3
//                     });
//                 }
//                 else
//                 {
//                     ecb.AddComponent(entity, new ConditionalTestSavingComponentA
//                     {
//                         Value = i+100
//                     });
//                     ecb.AddBuffer<ConditionalTestSavingBuffer>(entity);
//                     ecb.AppendToBuffer(entity,new ConditionalTestSavingBuffer
//                     {
//                         Value = (i + 100) * 3
//                     });
//                 }
//             }
//             ecb.Playback(EntityManager);
//             ecb.Dispose();
//             Debug.Log($"Generate {config.count}");
//         }
//         protected override void OnUpdate()
//         {
//             // if (Input.GetKeyDown(KeyCode.S))
//             // {
//             //     _ = Save();
//             // }
//             //
//             // if (Input.GetKeyDown(KeyCode.L))
//             // {
//             //     _ = Load();
//             // }
//             if (Input.GetKeyDown(KeyCode.Space))
//             {
//                 GenerateNeedSaveEntity();
//             }
//         }
//
//         private async Task Save()
//         {
//             Debug.Log("Start saving");
//             var config = SystemAPI.GetSingleton<TestSavingConfig>();
//             var savePath = SaveUtilities.GetCityMainDataPath(PlayerSaveSlot);
//             var savings = new NativeList<TestSavingComponentA>(Allocator.Temp);
//             for (var i = 0; i < config.count; i++)
//             {
//                 savings.Add(new TestSavingComponentA { Value = i });
//             }
//
//             var saveSize = UnsafeUtility.SizeOf<TestSavingComponentA>();
//             var bytes = savings.Length * saveSize;
//             var managed = savings.AsArray().Reinterpret<byte>(saveSize).ToArray();
//             savings.Dispose();
//
//             var tmp = savePath + ".tmp";
//             await using (var fs = new FileStream(tmp, FileMode.Create, FileAccess.Write, FileShare.None,
//                              bufferSize: 65536, useAsync: true))
//             {
//                 await fs.WriteAsync(managed, 0, bytes);
//                 await fs.FlushAsync();
//             }
//
//             // 原子替换
//             File.Copy(tmp, savePath, true);
//         }
//
//
//         private async Task Load()
//         {
//             Debug.Log("Start loading");
//             var savePath = SaveUtilities.GetCityMainDataPath(PlayerSaveSlot);
//             await using var fs = new FileStream(savePath, FileMode.Open, FileAccess.Read, FileShare.Read, bufferSize: 65536,
//                 useAsync: true);
//             var managed = new byte[fs.Length];
//             _ = await fs.ReadAsync(managed, 0, managed.Length);
//             var sizeOf = UnsafeUtility.SizeOf<TestSavingComponentA>();
//             var count = managed.Length / sizeOf;
//             var native = new NativeArray<TestSavingComponentA>(count, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
//             unsafe
//             {
//                 fixed (byte* src = managed)
//                 {
//                     UnsafeUtility.MemCpy(native.GetUnsafePtr(), src, count * sizeOf);
//                 }
//             }
//             var ecb = new EntityCommandBuffer(Allocator.Temp);
//             foreach (var s in native)
//             {
//                 var entity = ecb.CreateEntity();
//                 ecb.AddComponent(entity, s);
//             }
//             ecb.Playback(EntityManager);
//             ecb.Dispose();
//             native.Dispose();
//             OnLoadComplete?.Invoke();
//         }
//     }
// }