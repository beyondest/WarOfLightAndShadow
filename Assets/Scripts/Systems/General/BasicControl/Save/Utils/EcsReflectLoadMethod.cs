using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using SparFlame.Components.General;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;

namespace SparFlame.Systems.General.BasicControl
{
    public static class EcsReflectLoadMethod
    {
        private static readonly MethodInfo HasComponent = typeof(EntityManager)
            .GetMethods(BindingFlags.Public | BindingFlags.Instance)
            .First(m => m.Name == "HasComponent" && m.IsGenericMethod &&
                        m.GetParameters().Length == 1);

        private static readonly MethodInfo HasBuffer = typeof(EntityManager)
            .GetMethods(BindingFlags.Public | BindingFlags.Instance)
            .First(m => m.Name == "HasBuffer" && m.IsGenericMethod &&
                        m.GetParameters().Length == 1);

        private static readonly Dictionary<Type, Func<EntityManager, Entity, bool>> HasComponentCache = new();
        private static readonly Dictionary<Type, Func<EntityManager, Entity, bool>> HasBufferCache = new();

        public static async Task<RiftGameFileHeader> LoadFileHeaderAsync(FileStream fs)
        {
            var header = new RiftGameFileHeader();
            var buffer = new byte[4];
            _ = await fs.ReadAsync(buffer, 0, 4);
            header.SaveArcheType = (SaveArcheType)BitConverter.ToInt32(buffer, 0);
            _ = await fs.ReadAsync(buffer, 0, 4);
            header.EntityCount = BitConverter.ToInt32(buffer, 0);
            return header;
        }

        private static bool HasComponentCached(EntityManager em, Entity e, Type type)
        {
            if (!HasComponentCache.TryGetValue(type, out var func))
            {
                var methodInfo = HasComponent.MakeGenericMethod(type);
                var emParam = Expression.Parameter(typeof(EntityManager), "em");
                var entityParam = Expression.Parameter(typeof(Entity), "e");
                var body = Expression.Call(emParam, methodInfo, entityParam);
                func = Expression.Lambda<Func<EntityManager, Entity, bool>>(body, emParam, entityParam).Compile();
                HasComponentCache[type] = func;
            }

            return func(em, e);
        }

        private static bool HasBufferCached(EntityManager em, Entity e, Type type)
        {
            if (!HasBufferCache.TryGetValue(type, out var func))
            {
                var methodInfo = HasBuffer.MakeGenericMethod(type);
                var emParam = Expression.Parameter(typeof(EntityManager), "em");
                var entityParam = Expression.Parameter(typeof(Entity), "e");
                var body = Expression.Call(emParam, methodInfo, entityParam);
                func = Expression.Lambda<Func<EntityManager, Entity, bool>>(body, emParam, entityParam).Compile();
                HasBufferCache[type] = func;
            }

            return func(em, e);
        }

        /// <summary>
        /// Warning : native array should be disposed manually
        /// </summary>
        /// <param name="fs"></param>
        /// <param name="entityCount"></param>
        /// <returns></returns>
        public static async Task<NativeArray<PrefabId>> LoadPrefabIdsAsync(FileStream fs,
            int entityCount)
        {
            var prefabIds =
                new NativeArray<PrefabId>(entityCount, Allocator.Persistent,
                    NativeArrayOptions.UninitializedMemory);
            var totalSize = entityCount * UnsafeUtility.SizeOf<PrefabId>();
            var managed = new byte[totalSize];
            _ = await fs.ReadAsync(managed, 0, managed.Length);
            unsafe
            {
                fixed (byte* src = managed)
                {
                    UnsafeUtility.MemCpy(prefabIds.GetUnsafePtr(), src, totalSize);
                }
            }

            return prefabIds;
        }

        public static async Task PreLoadFixedComponentDataArraysAsync(FileStream fs,
            SaveArcheTypeInfo info, Dictionary<Type, Array> typeToArray,
            int entityCount)
        {
            foreach (var t in info.Types)
            {
                var elementSize = Marshal.SizeOf(t);
                var totalBytes = entityCount * elementSize;
                var bytesAll = new byte[totalBytes];

                var read = 0;
                while (read < totalBytes)
                {
                    var r = await fs.ReadAsync(bytesAll, read, totalBytes - read);
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

                typeToArray[t] = array;
            }
        }

        public static void LoadFixedComponentDatas(SaveArcheTypeInfo info,
            EntityCommandBuffer ecb, Entity e, int entityIndex,
            Dictionary<Type, Array> typeToArray)
        {
            foreach (var type in info.Types)
            {
                var array = typeToArray[type];
                var comp = array.GetValue(entityIndex);
                SaveUtilities.ECBSetComponentCached(ecb, e, type, comp);
            }
        }

        public static async Task LoadFixedBuffers(
            FileStream fs,
            SaveArcheTypeInfo info, EntityCommandBuffer ecb,
            Entity e)
        {
            foreach (var bufferType in info.BufferTypes)
            {
                var elementSize = UnsafeUtility.SizeOf(bufferType);

                var lengthBytes = new byte[4];
                _ = await fs.ReadAsync(lengthBytes, 0, 4);
                var bufferLength = BitConverter.ToInt32(lengthBytes, 0);
                SaveUtilities.ECBAddBufferCached(ecb, e, bufferType);
                if (bufferLength > 0)
                {
                    var bytesAll = new byte[bufferLength * elementSize];
                    var read = 0;
                    while (read < bytesAll.Length)
                    {
                        var r = await fs.ReadAsync(bytesAll, read, bytesAll.Length - read);
                        if (r == 0) throw new EndOfStreamException();
                        read += r;
                    }

                    var array = Array.CreateInstance(bufferType, bufferLength);
                    var handle = GCHandle.Alloc(array, GCHandleType.Pinned);
                    try
                    {
                        Marshal.Copy(bytesAll, 0, handle.AddrOfPinnedObject(), bytesAll.Length);
                    }
                    finally
                    {
                        handle.Free();
                    }

                    for (var j = 0; j < bufferLength; j++)
                    {
                        var element = array.GetValue(j);
                        SaveUtilities.ECBAppendToBufferCached(ecb, e, bufferType, element);
                    }
                }
            }
        }

        public static async Task LoadConditionalComponentDatas(FileStream fs,
            SaveArcheTypeInfo info, EntityCommandBuffer ecb, Entity e, EntityManager em, Entity prefab)
        {
            foreach (var compType in info.ConditionalComponentTypes)
            {
                var flag = fs.ReadByte();
                if (flag == 1)
                {
                    var size = Marshal.SizeOf(compType);
                    var bytes = new byte[size];
                    var read = 0;
                    while (read < size)
                    {
                        var r = await fs.ReadAsync(bytes, read, size - read);
                        if (r == 0) throw new EndOfStreamException();
                        read += r;
                    }

                    var handle = GCHandle.Alloc(bytes, GCHandleType.Pinned);
                    var comp = Marshal.PtrToStructure(handle.AddrOfPinnedObject(), compType)!;
                    handle.Free();
                    if (HasComponentCached(em, prefab, compType))
                    {
                        SaveUtilities.ECBSetComponentCached(ecb, e, compType, comp);
                    }
                    else
                    {
                        SaveUtilities.ECBAddComponentCached(ecb, e, compType, comp);
                    }
                }
            }
        }

        public static async Task LoadConditionalBuffers(
            FileStream fs,
            SaveArcheTypeInfo info, EntityCommandBuffer ecb, Entity e, EntityManager em, Entity prefab)
        {
            foreach (var bufferType in info.ConditionalBufferTypes)
            {
                int flag = fs.ReadByte();
                if (flag == 0) continue;

                var elementSize = UnsafeUtility.SizeOf(bufferType);

                var lengthBytes = new byte[4];
                _ = await fs.ReadAsync(lengthBytes, 0, 4);
                int bufferLength = BitConverter.ToInt32(lengthBytes, 0);

                if (!HasBufferCached(em, prefab, bufferType))
                    SaveUtilities.ECBAddBufferCached(ecb, e, bufferType);
                if (bufferLength > 0)
                {
                    var bytesAll = new byte[bufferLength * elementSize];
                    int read = 0;
                    while (read < bytesAll.Length)
                    {
                        var r = await fs.ReadAsync(bytesAll, read, bytesAll.Length - read);
                        if (r == 0) throw new EndOfStreamException();
                        read += r;
                    }

                    var array = Array.CreateInstance(bufferType, bufferLength);
                    var handle = GCHandle.Alloc(array, GCHandleType.Pinned);
                    try
                    {
                        Marshal.Copy(bytesAll, 0, handle.AddrOfPinnedObject(), bytesAll.Length);
                    }
                    finally
                    {
                        handle.Free();
                    }

                    for (var j = 0; j < bufferLength; j++)
                    {
                        var element = array.GetValue(j);
                        SaveUtilities.ECBAppendToBufferCached(ecb, e, bufferType, element);
                    }
                }
            }
        }

        public static async Task LoadFixedComponentSingletonsAsync(
            FileStream fs,
            SaveArcheTypeInfo info,
            EntityManager em,
            EntityCommandBuffer ecb)
        {
            foreach (var t in info.Types)
            {
                var elementSize = Marshal.SizeOf(t);
                var bytesAll = new byte[elementSize];

                var read = 0;
                while (read < elementSize)
                {
                    var r = await fs.ReadAsync(bytesAll, read, elementSize - read);
                    if (r == 0) throw new EndOfStreamException();
                    read += r;
                }

                // Deserialize to struct
                object comp;
                var handle = GCHandle.Alloc(bytesAll, GCHandleType.Pinned);
                try
                {
                    comp = Marshal.PtrToStructure(handle.AddrOfPinnedObject(), t)!;
                }
                finally
                {
                    handle.Free();
                }

                // find current singleton entity
                using var query = em.CreateEntityQuery(t);
                var singletonEntity = query.GetSingletonEntity();

                // set back to singleton entity
                SaveUtilities.ECBSetComponentCached(ecb, singletonEntity, t, comp);
            }
        }


        public static async Task LoadFixedBufferSingletonsAsync(
            FileStream fs,
            SaveArcheTypeInfo info,
            EntityManager em,
            EntityCommandBuffer ecb)
        {
            foreach (var bufferType in info.BufferTypes)
            {
                // 先读 buffer 长度
                var lengthBytes = new byte[4];
                var read = 0;
                while (read < 4)
                {
                    var r = await fs.ReadAsync(lengthBytes, read, 4 - read);
                    if (r == 0) throw new EndOfStreamException();
                    read += r;
                }

                var bufferLength = BitConverter.ToInt32(lengthBytes, 0);

                using var query = em.CreateEntityQuery(bufferType);
                var singletonEntity = query.GetSingletonEntity();

                // reset buffer
                SaveUtilities.ECBSetBufferCached(ecb, singletonEntity, bufferType);

                if (bufferLength <= 0) continue;

                var elementSize = UnsafeUtility.SizeOf(bufferType);
                var bytesAll = new byte[bufferLength * elementSize];

                read = 0;
                while (read < bytesAll.Length)
                {
                    var r = await fs.ReadAsync(bytesAll, read, bytesAll.Length - read);
                    if (r == 0) throw new EndOfStreamException();
                    read += r;
                }

                var array = Array.CreateInstance(bufferType, bufferLength);
                var handle = GCHandle.Alloc(array, GCHandleType.Pinned);
                try
                {
                    Marshal.Copy(bytesAll, 0, handle.AddrOfPinnedObject(), bytesAll.Length);
                }
                finally
                {
                    handle.Free();
                }

                // Append 
                for (var j = 0; j < bufferLength; j++)
                {
                    var element = array.GetValue(j);
                    SaveUtilities.ECBAppendToBufferCached(ecb, singletonEntity, bufferType, element);
                }
            }
        }
    }
}