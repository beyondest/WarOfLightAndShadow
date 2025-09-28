// using System;
// using System.Collections.Generic;
// using System.IO;
// using System.Linq;
// using System.Reflection;
// using System.Runtime.InteropServices;
// using System.Threading.Tasks;
// using SparFlame.Systems.General.BasicControl;
// using Unity.Collections;
// using Unity.Collections.LowLevel.Unsafe;
// using Unity.Entities;
// using UnityEngine;
//
// public class TestSaveManager : MonoBehaviour
// {
//     public SaveArcheTypeInfo info; // 编辑器里配置
//     public bool syncOrNot;
//     private string SavePath => Path.Combine(Application.persistentDataPath, "save.bin");
//
//     private void Awake()
//     {
//         Debug.Log($"Test save path : {SavePath}");
//     }
//
//     private void Start()
//     {
//         info.RebuildCache();
//     }
//
//     private void Update()
//     {
//         if (Input.GetKeyDown(KeyCode.S))
//             if (syncOrNot)
//             {
//                 SaveSync();
//             }
//             else
//             {
//                 _ = SaveAsync().ContinueWith(t => { Debug.LogError(t.Exception); },
//                     TaskContinuationOptions.OnlyOnFaulted);
//             }
//
//         if (Input.GetKeyDown(KeyCode.L))
//             if (syncOrNot)
//             {
//                 LoadSync();
//             }
//             else
//             {
//                 _ = LoadAsync().ContinueWith(t => { Debug.LogError(t.Exception); },
//                     TaskContinuationOptions.OnlyOnFaulted);
//             }
//
//         if (Input.GetKeyDown(KeyCode.F))
//         {
//             var em = World.DefaultGameObjectInjectionWorld.EntityManager;
//         }
//     }
//
//
//     // Save: 对每个 entity，遍历 template.ComponentTypes，用 GetComponentBoxed 拿到 boxed struct，再用 StructToBytes 写文件
//     private async Task SaveAsync()
//     {
//         // await using var fs = new FileStream(savePath, FileMode.Create, FileAccess.Write, FileShare.None);
//         var tmpPath = SavePath + ".aux";
//         var template = info;
//         // 构造 EntityQuery：SavingTag + PrefabId + template.ComponentTypes
//         // var qb = new EntityQueryDesc { All = new ComponentType[] { typeof(SavingTag), typeof(PrefabId) } };
//         // 这里简化：请用 QueryBuilder 或动态构建，根据 template.ComponentTypes 构建查询
//         // ...
//         var em = World.DefaultGameObjectInjectionWorld.EntityManager;
//         using var query = em.CreateEntityQuery(template.QueryWithAllComponentTypes.ToArray());
//         var entityCount = query.CalculateEntityCount();
//         var typeToBytes = new Dictionary<Type, byte[]>();
//         var queryType = typeof(EntityQuery);
//         var methodInfo = queryType.GetMethods(BindingFlags.Public | BindingFlags.Instance)
//             .FirstOrDefault(m =>
//                 m.Name == "ToComponentDataArray" &&
//                 m.IsGenericMethodDefinition &&
//                 m.GetParameters().Length == 1
//             );
//
//         // 1. 遍历所有需要保存的组件类型，反射调用 ToComponentDataArray<T>
//         foreach (var t in template.Types)
//         {
//             var elementSize = Marshal.SizeOf(t);
//
//             var method = methodInfo!.MakeGenericMethod(t);
//
//             // 调用 ToComponentDataArray<T>(query, Allocator.Temp)
//             var handle = AllocatorManager.ConvertToAllocatorHandle(Allocator.Temp);
//             var nativeArrayObj = method.Invoke(query, new object[] { handle });
//
//
//             var bytesAll = SaveUtilities.NativeArrayBoxedToBytes(nativeArrayObj, elementSize);
//             typeToBytes[t] = bytesAll;
//
//             if (nativeArrayObj is IDisposable disposable)
//                 disposable.Dispose();
//         }
//
//         // 2. 原子写入 tmp 文件
//         await using (var fs = new FileStream(tmpPath, FileMode.Create, FileAccess.Write, FileShare.None,
//                          65536, useAsync: true))
//         {
//             // ArchetypeId
//             await fs.WriteAsync(BitConverter.GetBytes((int)template.saveArcheType), 0, 4);
//             // EntityCount
//             await fs.WriteAsync(BitConverter.GetBytes(entityCount), 0, 4);
//
//             // 写入每个组件整列
//             foreach (var t in template.Types)
//             {
//                 var bytesAll = typeToBytes[t];
//                 await fs.WriteAsync(bytesAll, 0, bytesAll.Length);
//             }
//
//             await fs.FlushAsync();
//         }
//
//         // 3. 覆盖保存
//         File.Copy(tmpPath, SavePath, true);
//     }
//
//     // Load：读取 archetypeId + prefabId，Instantiate prefab，然后按 template 里的 ComponentTypes 依次读 size+data -> BytesToStruct -> SetComponentBoxed
//     private async Task LoadAsync()
//     {
//         await using var fs = new FileStream(
//             SavePath,
//             FileMode.Open,
//             FileAccess.Read,
//             FileShare.Read,
//             bufferSize: 65536,
//             useAsync: true
//         );
//
//         // --- 1. 读 ArchetypeId ---
//         var buffer = new byte[4];
//         _ = await fs.ReadAsync(buffer, 0, 4);
//         var archetypeId = BitConverter.ToInt32(buffer, 0);
//         if (archetypeId != (int)info.saveArcheType) throw new ArgumentException("Wrong archetype");
//         var template = info;
//         // --- 2. 读 EntityCount ---
//         _ = await fs.ReadAsync(buffer, 0, 4);
//         var entityCount = BitConverter.ToInt32(buffer, 0);
//
//
//         // --- 3. 读取所有组件整列 ---
//         var typeToArrays = new Dictionary<Type, Array>();
//         foreach (var t in template.Types)
//         {
//             var elementSize = Marshal.SizeOf(t);
//             var totalBytes = entityCount * elementSize;
//             var bytesAll = new byte[totalBytes];
//
//             var read = 0;
//             while (read < totalBytes)
//             {
//                 var r = await fs.ReadAsync(bytesAll, read, totalBytes - read);
//                 if (r == 0) throw new EndOfStreamException();
//                 read += r;
//             }
//
//             // 转回 NativeArray<T>
//             var array = Array.CreateInstance(t, entityCount);
//
//
//             var handle = GCHandle.Alloc(array, GCHandleType.Pinned);
//             try
//             {
//                 Marshal.Copy(bytesAll, 0, handle.AddrOfPinnedObject(), totalBytes);
//             }
//             finally
//             {
//                 handle.Free();
//             }
//
//             typeToArrays[t] = array;
//         }
//
//         // --- 4. 用 ECB 创建并还原 ---
//         using var ecb = new EntityCommandBuffer(Allocator.TempJob);
//         for (var i = 0; i < entityCount; i++)
//         {
//             var e = ecb.CreateEntity(); // 根据 prefab 实例化（假设每个 Archetype 对应一个 prefab）
//             foreach (var t in template.Types)
//             {
//                 var array = typeToArrays[t];
//                 var comp = array.GetValue(i);
//
//                 // 调用 EntityCommandBuffer.AddComponent<T>(entity, component)
//                 // var method = typeToMethods[t];
//                 // method.Invoke(ecb, new[] { e, comp });
//                 SaveUtilities.ECBAddComponentCached(ecb, e, t, comp);
//             }
//         }
//
//         ecb.Playback(World.DefaultGameObjectInjectionWorld.EntityManager);
//     }
//
//
//     #region Sync
//
//     private void LoadSync()
//     {
//         Debug.Log("Start loading");
//         using var fs = new FileStream(
//             SavePath,
//             FileMode.Open,
//             FileAccess.Read,
//             FileShare.Read,
//             bufferSize: 65536
//         );
//
//         // --- 1. 读 ArchetypeId ---
//         var buffer = new byte[4];
//         _ = fs.Read(buffer, 0, 4);
//         var archetypeId = BitConverter.ToInt32(buffer, 0);
//         if (archetypeId != (int)info.saveArcheType) throw new ArgumentException("Wrong archetype");
//         var template = info;
//
//         // --- 2. 读 EntityCount ---
//         _ = fs.Read(buffer, 0, 4);
//         var entityCount = BitConverter.ToInt32(buffer, 0);
//
//         // --- 3. 读取所有组件整列 ---
//         var typeToArray = new Dictionary<Type, Array>();
//         foreach (var t in template.Types)
//         {
//             var elementSize = Marshal.SizeOf(t);
//             var totalBytes = entityCount * elementSize;
//             var bytesAll = new byte[totalBytes];
//
//             var read = 0;
//             while (read < totalBytes)
//             {
//                 var r = fs.Read(bytesAll, read, totalBytes - read);
//                 if (r == 0) throw new EndOfStreamException();
//                 read += r;
//             }
//
//             // 转回 NativeArray<T> 或普通数组
//             var array = Array.CreateInstance(t, entityCount);
//             var handle = GCHandle.Alloc(array, GCHandleType.Pinned);
//             try
//             {
//                 Marshal.Copy(bytesAll, 0, handle.AddrOfPinnedObject(), totalBytes);
//             }
//             finally
//             {
//                 handle.Free();
//             }
//
//             typeToArray[t] = array;
//         }
//
//
//         // --- 4. 用 ECB 创建并还原 ---
//         var ecb = new EntityCommandBuffer(Allocator.TempJob);
//         var methodInfo = typeof(EntityCommandBuffer)
//             .GetMethods(BindingFlags.Public | BindingFlags.Instance)
//             .First(m => m.Name == "AddComponent" &&
//                         m.GetParameters().Length == 2);
//         var typeToMethod = new Dictionary<Type, MethodInfo>();
//         foreach (var t in template.Types)
//         {
//             var method = methodInfo.MakeGenericMethod(t);
//             typeToMethod[t] = method;
//         }
//
//
//         for (var i = 0; i < entityCount; i++)
//         {
//             var e = ecb.CreateEntity();
//
//             // --- Fixed Components ---
//             foreach (var type in template.Types)
//             {
//                 var array = typeToArray[type];
//                 var method = typeToMethod[type];
//                 var comp = array.GetValue(i);
//                 method.Invoke(ecb, new[] { e, comp });
//             }
//
//             // --- Fixed Buffers ---
//             foreach (var bufferType in info.BufferTypes)
//             {
//                 var elementSize = UnsafeUtility.SizeOf(bufferType);
//
//                 var lengthBytes = new byte[4];
//                 _ = fs.Read(lengthBytes, 0, 4);
//                 int bufferLength = BitConverter.ToInt32(lengthBytes, 0);
//
//                 SaveUtilities.ECBAddBufferCached(ecb, e, bufferType);
//                 if (bufferLength > 0)
//                 {
//                     var bytesAll = new byte[bufferLength * elementSize];
//                     int read = 0;
//                     while (read < bytesAll.Length)
//                     {
//                         var r = fs.Read(bytesAll, read, bytesAll.Length - read);
//                         if (r == 0) throw new EndOfStreamException();
//                         read += r;
//                     }
//
//                     var array = Array.CreateInstance(bufferType, bufferLength);
//                     var handle = GCHandle.Alloc(array, GCHandleType.Pinned);
//                     try
//                     {
//                         Marshal.Copy(bytesAll, 0, handle.AddrOfPinnedObject(), bytesAll.Length);
//                     }
//                     finally
//                     {
//                         handle.Free();
//                     }
//
//                     for (var j = 0; j < bufferLength; j++)
//                     {
//                         var element = array.GetValue(j);
//                         SaveUtilities.ECBAppendToBufferCached(ecb, e, bufferType, element);
//                     }
//                 }
//             }
//
//             // --- Conditional Components ---
//             foreach (var compType in info.ConditionalComponentTypes)
//             {
//                 int flag = fs.ReadByte();
//                 if (flag == 1)
//                 {
//                     int size = Marshal.SizeOf(compType);
//                     var bytes = new byte[size];
//                     int read = 0;
//                     while (read < size)
//                     {
//                         int r = fs.Read(bytes, read, size - read);
//                         if (r == 0) throw new EndOfStreamException();
//                         read += r;
//                     }
//
//                     var handle = GCHandle.Alloc(bytes, GCHandleType.Pinned);
//                     object comp = Marshal.PtrToStructure(handle.AddrOfPinnedObject(), compType)!;
//                     handle.Free();
//                     SaveUtilities.ECBAddComponentCached(ecb, e, compType, comp);
//                 }
//             }
//
//             // --- Conditional Buffers ---
//             foreach (var bufferType in info.ConditionalBufferTypes)
//             {
//                 int flag = fs.ReadByte();
//                 if (flag == 0) continue;
//
//                 var elementSize = UnsafeUtility.SizeOf(bufferType);
//
//                 var lengthBytes = new byte[4];
//                 _ = fs.Read(lengthBytes, 0, 4);
//                 int bufferLength = BitConverter.ToInt32(lengthBytes, 0);
//
//                 SaveUtilities.ECBAddBufferCached(ecb, e, bufferType);
//                 if (bufferLength > 0)
//                 {
//                     var bytesAll = new byte[bufferLength * elementSize];
//                     int read = 0;
//                     while (read < bytesAll.Length)
//                     {
//                         var r = fs.Read(bytesAll, read, bytesAll.Length - read);
//                         if (r == 0) throw new EndOfStreamException();
//                         read += r;
//                     }
//
//                     var array = Array.CreateInstance(bufferType, bufferLength);
//                     var handle = GCHandle.Alloc(array, GCHandleType.Pinned);
//                     try
//                     {
//                         Marshal.Copy(bytesAll, 0, handle.AddrOfPinnedObject(), bytesAll.Length);
//                     }
//                     finally
//                     {
//                         handle.Free();
//                     }
//
//                     for (var j = 0; j < bufferLength; j++)
//                     {
//                         var element = array.GetValue(j);
//                         SaveUtilities.ECBAppendToBufferCached(ecb, e, bufferType, element);
//                     }
//                 }
//             }
//         }
//
//         ecb.Playback(World.DefaultGameObjectInjectionWorld.EntityManager);
//         ecb.Dispose();
//     }
//
//     private void SaveSync()
//     {
//         var tmpPath = SavePath + ".tmp";
//         Debug.Log("Start saving");
//
//         var template = info;
//         var em = World.DefaultGameObjectInjectionWorld.EntityManager;
//         using var query = em.CreateEntityQuery(template.QueryWithAllComponentTypes.ToArray());
//
//         using var entities = query.ToEntityArray(Allocator.Persistent);
//         var entityCount = entities.Length;
//         var typeToBytes = new Dictionary<Type, byte[]>();
//         var queryType = typeof(EntityQuery);
//         var methodInfo = queryType.GetMethods(BindingFlags.Public | BindingFlags.Instance)
//             .FirstOrDefault(m =>
//                 m.Name == "ToComponentDataArray" &&
//                 m.IsGenericMethodDefinition &&
//                 m.GetParameters().Length == 1
//             );
//
//         // 1. 遍历所有需要保存的组件类型，反射调用 ToComponentDataArray<T>
//         foreach (var t in template.Types)
//         {
//             var elementSize = Marshal.SizeOf(t);
//             var method = methodInfo!.MakeGenericMethod(t);
//             // 调用 ToComponentDataArray<T>(query, Allocator.Temp)
//             var handle = AllocatorManager.ConvertToAllocatorHandle(Allocator.Temp);
//             var nativeArrayObj = method.Invoke(query, new object[] { handle });
//             var bytesAll = SaveUtilities.NativeArrayBoxedToBytes(nativeArrayObj, elementSize);
//             typeToBytes[t] = bytesAll;
//             if (nativeArrayObj is IDisposable disposable)
//                 disposable.Dispose();
//         }
//
//         // 2. 原子写入 tmp 文件
//         using (var fs = new FileStream(tmpPath, FileMode.Create, FileAccess.Write, FileShare.None, 65536))
//         {
//             // ArchetypeId
//             fs.Write(BitConverter.GetBytes((int)template.saveArcheType), 0, 4);
//             // EntityCount
//             fs.Write(BitConverter.GetBytes(entityCount), 0, 4);
//
//             // --- Fixed Components ---
//             foreach (var t in template.Types)
//             {
//                 var bytesAll = typeToBytes[t];
//                 fs.Write(bytesAll, 0, bytesAll.Length);
//             }
//
//             // Traverse all entities and save all buffer types
//             var getBufferMethodInfo =
//                 typeof(EntityManager)
//                     .GetMethods(BindingFlags.Public | BindingFlags.Instance)
//                     .First(m => m.IsGenericMethod && m.Name == "GetBuffer" && m.GetParameters().Length == 2 &&
//                                 m.GetParameters()[0].ParameterType == typeof(Entity));
//             var getComponentDataMethodInfo = typeof(EntityManager)
//                 .GetMethods(BindingFlags.Public | BindingFlags.Instance)
//                 .First(m => m.IsGenericMethod && m.Name == "GetComponentData" && m.GetParameters().Length == 1 &&
//                             m.GetParameters()[0].ParameterType == typeof(Entity));
//             
//             foreach (var e in entities)
//             {
//                 // --- Fixed Buffers ---
//                 foreach (var bufferType in info.BufferTypes)
//                 {
//                     var getBufferMethod = getBufferMethodInfo!.MakeGenericMethod(bufferType);
//                     var elementSize = UnsafeUtility.SizeOf(bufferType);
//
//                     var bufferObj = getBufferMethod.Invoke(em, new object[] { e, true });
//                     var bufferTypeInterface = bufferObj.GetType();
//                     var lengthProperty = bufferTypeInterface.GetProperty("Length")!;
//                     int length = (int)lengthProperty.GetValue(bufferObj)!;
//
//                     fs.Write(BitConverter.GetBytes(length), 0, 4);
//
//                     if (length > 0)
//                     {
//                         var toNativeArrayMethod = bufferTypeInterface.GetMethod("AsNativeArray")!;
//                         var nativeArrayObj = toNativeArrayMethod.Invoke(bufferObj, null);
//
//                         var bytes = SaveUtilities.NativeArrayBoxedToBytes(nativeArrayObj, elementSize);
//                         fs.Write(bytes, 0, bytes.Length);
//
//                         if (nativeArrayObj is IDisposable disposable)
//                             disposable.Dispose();
//                     }
//                 }
//
//                 // --- Conditional Components ---
//                 foreach (var compType in info.ConditionalComponentTypes)
//                 {
//                     bool hasComp = em.HasComponent(e, compType);
//                     fs.WriteByte((byte)(hasComp ? 1 : 0));
//                     if (hasComp)
//                     {
//                         var method = getComponentDataMethodInfo!.MakeGenericMethod(compType);
//                         var compObj = method.Invoke(em, new object[] { e });
//
//                         int size = Marshal.SizeOf(compType);
//                         var buffer = new byte[size];
//                         var handle = GCHandle.Alloc(compObj, GCHandleType.Pinned);
//                         try
//                         {
//                             Marshal.Copy(handle.AddrOfPinnedObject(), buffer, 0, size);
//                         }
//                         finally
//                         {
//                             handle.Free();
//                         }
//
//                         fs.Write(buffer, 0, size);
//                     }
//                 }
//
//                 // --- Conditional Buffers ---
//                 foreach (var bufferType in info.ConditionalBufferTypes)
//                 {
//                     bool hasBuffer = em.HasComponent(e, bufferType); // buffer 是 component
//                     fs.WriteByte((byte)(hasBuffer ? 1 : 0));
//                     if (!hasBuffer) continue;
//
//                     var getBufferMethod = getBufferMethodInfo!.MakeGenericMethod(bufferType);
//                     var elementSize = UnsafeUtility.SizeOf(bufferType);
//
//                     var bufferObj = getBufferMethod.Invoke(em, new object[] { e, true });
//                     var bufferTypeInterface = bufferObj.GetType();
//                     var lengthProperty = bufferTypeInterface.GetProperty("Length")!;
//                     int length = (int)lengthProperty.GetValue(bufferObj)!;
//
//                     fs.Write(BitConverter.GetBytes(length), 0, 4);
//
//                     if (length > 0)
//                     {
//                         var toNativeArrayMethod = bufferTypeInterface.GetMethod("AsNativeArray")!;
//                         var nativeArrayObj = toNativeArrayMethod.Invoke(bufferObj, null);
//
//                         var bytes = SaveUtilities.NativeArrayBoxedToBytes(nativeArrayObj, elementSize);
//                         fs.Write(bytes, 0, bytes.Length);
//
//                         if (nativeArrayObj is IDisposable disposable)
//                             disposable.Dispose();
//                     }
//                 }
//             }
//
//             fs.Flush();
//         }
//
//
//         // 3. 覆盖保存
//         File.Copy(tmpPath, SavePath, true);
//     }
//
//     #endregion
// }