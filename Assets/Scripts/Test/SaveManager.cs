using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using SparFlame.Systems.General.BasicControl;
using SparFlame.Test;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using UnityEngine;
using SavableArchetype = SparFlame.Test.SavableArchetype;

public class SaveManager : MonoBehaviour
{
    public static SaveManager Instance;
    public List<SavableArchetype> archetypeTemplates = new(); // 编辑器里配置
    public Dictionary<int, Entity> PrefabRegistry = new(); // prefabId → prefab entity

    private string SavePath => Path.Combine(Application.persistentDataPath, "save.bin");

    private void Awake()
    {
        Instance = this;

        Debug.Log($"{SavePath}");
    }

    private void Start()
    {
        foreach (var template in archetypeTemplates)
        {
            template.RebuildCache();
        }
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.S))
            _ = SaveAsync().ContinueWith(t => { Debug.LogError(t.Exception); }, TaskContinuationOptions.OnlyOnFaulted);
        if (Input.GetKeyDown(KeyCode.L))
            _ = LoadAsync().ContinueWith(t => { Debug.LogError(t.Exception); }, TaskContinuationOptions.OnlyOnFaulted);
    }


    private void SaveSync()
    {
        var tmpPath = SavePath + ".tmp";
        Debug.Log("Start saving");

        foreach (var template in archetypeTemplates)
        {
            var em = World.DefaultGameObjectInjectionWorld.EntityManager;
            var query = em.CreateEntityQuery(template.ComponentTypes.ToArray());

            using var entities = query.ToEntityArray(Allocator.Persistent);
            var entityCount = entities.Length;
            var typeToBytes = new Dictionary<Type, byte[]>();
            var queryType = typeof(EntityQuery);
            var methodInfo = queryType.GetMethods(BindingFlags.Public | BindingFlags.Instance)
                .FirstOrDefault(m =>
                    m.Name == "ToComponentDataArray" &&
                    m.IsGenericMethodDefinition &&
                    m.GetParameters().Length == 1
                );

            // 1. 遍历所有需要保存的组件类型，反射调用 ToComponentDataArray<T>
            for (var i = 0; i < template.Types.Count; i++)
            {
                var t = template.Types[i];
                var ct = template.ComponentTypes[i];
                var elementSize = Marshal.SizeOf(t);
                var method = methodInfo.MakeGenericMethod(t);
                // 调用 ToComponentDataArray<T>(query, Allocator.Temp)
                var handle = AllocatorManager.ConvertToAllocatorHandle(Allocator.Temp);
                var nativeArrayObj = method.Invoke(query, new object[] { handle });
                var bytesAll = SaveUtilities.NativeArrayBoxedToBytes(nativeArrayObj, elementSize);
                typeToBytes[t] = bytesAll;
                if (nativeArrayObj is IDisposable disp)
                    disp.Dispose();
            }

            // 2. 原子写入 tmp 文件
            using (var fs = new FileStream(tmpPath, FileMode.Create, FileAccess.Write, FileShare.None, 65536))
            {
                // ArchetypeId
                fs.Write(BitConverter.GetBytes(template.ArchetypeId), 0, 4);
                // EntityCount
                fs.Write(BitConverter.GetBytes(entityCount), 0, 4);

                // 写入每个组件整列
                foreach (var t in template.Types)
                {
                    var bytesAll = typeToBytes[t];
                    fs.Write(bytesAll, 0, bytesAll.Length);
                }

                fs.Flush();
            }

            // 3. 覆盖保存
            File.Copy(tmpPath, SavePath, true);
        }
    }


// Save: 对每个 entity，遍历 template.ComponentTypes，用 GetComponentBoxed 拿到 boxed struct，再用 StructToBytes 写文件
    private async Task SaveAsync()
    {
        // await using var fs = new FileStream(savePath, FileMode.Create, FileAccess.Write, FileShare.None);
        var tmpPath = SavePath + ".tmp";
        Debug.Log("Start saving");

        foreach (var template in archetypeTemplates)
        {
            // 构造 EntityQuery：SavingTag + PrefabId + template.ComponentTypes
            // var qb = new EntityQueryDesc { All = new ComponentType[] { typeof(SavingTag), typeof(PrefabId) } };
            // 这里简化：请用 QueryBuilder 或动态构建，根据 template.ComponentTypes 构建查询
            // ...
            var em = World.DefaultGameObjectInjectionWorld.EntityManager;
            var query = em.CreateEntityQuery(template.ComponentTypes.ToArray());
            var entityCount = query.CalculateEntityCount();
            var typeToBytes = new Dictionary<Type, byte[]>();
            var queryType = typeof(EntityQuery);
            var methodInfo = queryType.GetMethods(BindingFlags.Public | BindingFlags.Instance)
                .FirstOrDefault(m =>
                    m.Name == "ToComponentDataArray" &&
                    m.IsGenericMethodDefinition &&
                    m.GetParameters().Length == 1
                );

            // 1. 遍历所有需要保存的组件类型，反射调用 ToComponentDataArray<T>
            foreach (var t in template.Types)
            {
                var elementSize = Marshal.SizeOf(t);

                var method = methodInfo!.MakeGenericMethod(t);

                // 调用 ToComponentDataArray<T>(query, Allocator.Temp)
                var handle = AllocatorManager.ConvertToAllocatorHandle(Allocator.Temp);
                var nativeArrayObj = method.Invoke(query, new object[] { handle });


                var bytesAll = SaveUtilities.NativeArrayBoxedToBytes(nativeArrayObj, elementSize);
                typeToBytes[t] = bytesAll;

                if (nativeArrayObj is IDisposable disposable)
                    disposable.Dispose();
            }

            // 2. 原子写入 tmp 文件
            await using (var fs = new FileStream(tmpPath, FileMode.Create, FileAccess.Write, FileShare.None,
                             65536, useAsync: true))
            {
                // ArchetypeId
                await fs.WriteAsync(BitConverter.GetBytes(template.ArchetypeId), 0, 4);
                // EntityCount
                await fs.WriteAsync(BitConverter.GetBytes(entityCount), 0, 4);

                // 写入每个组件整列
                foreach (var t in template.Types)
                {
                    var bytesAll = typeToBytes[t];
                    await fs.WriteAsync(bytesAll, 0, bytesAll.Length);
                }

                await fs.FlushAsync();
            }

            // 3. 覆盖保存
            File.Copy(tmpPath, SavePath, true);
        }
    }

// Load：读取 archetypeId + prefabId，Instantiate prefab，然后按 template 里的 ComponentTypes 依次读 size+data -> BytesToStruct -> SetComponentBoxed
    private async Task LoadAsync()
    {
        await using var fs = new FileStream(
            SavePath,
            FileMode.Open,
            FileAccess.Read,
            FileShare.Read,
            bufferSize: 65536,
            useAsync: true
        );

        // --- 1. 读 ArchetypeId ---
        var buffer = new byte[4];
        _ = await fs.ReadAsync(buffer, 0, 4);
        int archetypeId = BitConverter.ToInt32(buffer, 0);
        var template = archetypeTemplates[archetypeId];
        // --- 2. 读 EntityCount ---
        _ = await fs.ReadAsync(buffer, 0, 4);
        int entityCount = BitConverter.ToInt32(buffer, 0);

        
        
        // --- 3. 读取所有组件整列 ---
        var typeToArrays = new Dictionary<Type, Array>();
        foreach (var t in template.Types)
        {
            int elementSize = Marshal.SizeOf(t);
            int totalBytes = entityCount * elementSize;
            var bytesAll = new byte[totalBytes];

            int read = 0;
            while (read < totalBytes)
            {
                int r = await fs.ReadAsync(bytesAll, read, totalBytes - read);
                if (r == 0) throw new EndOfStreamException();
                read += r;
            }
            
            // 转回 NativeArray<T>
            var array = Array.CreateInstance(t, entityCount);
            
            
            
            var handle = GCHandle.Alloc(array, GCHandleType.Pinned);
            try
            {
                Marshal.Copy(bytesAll, 0, handle.AddrOfPinnedObject(), totalBytes);
            }
            finally
            {
                handle.Free();
            }
            typeToArrays[t] = array;
            
        }

        // --- 4. 用 ECB 创建并还原 ---
        using var ecb = new EntityCommandBuffer(Allocator.TempJob);
        for (int i = 0; i < entityCount; i++)
        {
            var e = ecb.CreateEntity(); // 根据 prefab 实例化（假设每个 Archetype 对应一个 prefab）
            foreach (var t in template.Types)
            {
                var array = typeToArrays[t];
                var comp = array.GetValue(i);

                // 调用 EntityCommandBuffer.AddComponent<T>(entity, component)
                // var method = typeToMethods[t];
                // method.Invoke(ecb, new[] { e, comp });
                SaveUtilities.ECBAddComponentCached(ecb, e, t, comp);
            }
        }
        
        ecb.Playback(World.DefaultGameObjectInjectionWorld.EntityManager);
    }

    private void LoadSync()
    {
        using var fs = new FileStream(
            SavePath,
            FileMode.Open,
            FileAccess.Read,
            FileShare.Read,
            bufferSize: 65536
        );

        // --- 1. 读 ArchetypeId ---
        var buffer = new byte[4];
        fs.Read(buffer, 0, 4);
        int archetypeId = BitConverter.ToInt32(buffer, 0);
        var template = archetypeTemplates[archetypeId];

        // --- 2. 读 EntityCount ---
        fs.Read(buffer, 0, 4);
        int entityCount = BitConverter.ToInt32(buffer, 0);

        // --- 3. 读取所有组件整列 ---
        var typeToArrays = new Dictionary<Type, Array>();
        foreach (var t in template.Types)
        {
            int elementSize = Marshal.SizeOf(t);
            int totalBytes = entityCount * elementSize;
            var bytesAll = new byte[totalBytes];

            int read = 0;
            while (read < totalBytes)
            {
                int r = fs.Read(bytesAll, read, totalBytes - read);
                if (r == 0) throw new EndOfStreamException();
                read += r;
            }

            // 转回 NativeArray<T> 或普通数组
            var array = Array.CreateInstance(t, entityCount);
            var handle = GCHandle.Alloc(array, GCHandleType.Pinned);
            try
            {
                Marshal.Copy(bytesAll, 0, handle.AddrOfPinnedObject(), totalBytes);
            }
            finally
            {
                handle.Free();
            }

            typeToArrays[t] = array;
        }

        // --- 4. 用 ECB 创建并还原 ---
        var ecb = new EntityCommandBuffer(Allocator.TempJob);
        var methodInfo = typeof(EntityCommandBuffer)
            .GetMethods(BindingFlags.Public | BindingFlags.Instance)
            .First(m => m.Name == "AddComponent" &&
                        m.GetParameters().Length == 2);
        var typeToMethod = new Dictionary<Type, MethodInfo>();
        foreach (var t in template.Types)
        {
            var method = methodInfo.MakeGenericMethod(t);
            typeToMethod[t] = method;
        }

        for (var i = 0; i < entityCount; i++)
        {
            var e = ecb.CreateEntity(); // 根据 prefab 实例化（假设每个 Archetype 对应一个 prefab）
            foreach (var type in template.Types)
            {
                var array = typeToArrays[type];
                var method = typeToMethod[type];
                var comp = array.GetValue(i);
                // 调用 EntityCommandBuffer.AddComponent<T>(entity, component)
                method.Invoke(ecb, new[] { e, comp });
            }
        }


        ecb.Playback(World.DefaultGameObjectInjectionWorld.EntityManager);
        ecb.Dispose();
    }
}