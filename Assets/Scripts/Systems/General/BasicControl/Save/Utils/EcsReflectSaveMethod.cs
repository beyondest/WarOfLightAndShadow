using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using SparFlame.Components.General;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;

namespace SparFlame.Systems.General.BasicControl
{
    public static class EcsReflectSaveMethod
    {
        private static readonly MethodInfo ToComponentDataArray = typeof(EntityQuery)
            .GetMethods(BindingFlags.Public | BindingFlags.Instance)
            .FirstOrDefault(m =>
                m.Name == "ToComponentDataArray" &&
                m.IsGenericMethodDefinition &&
                m.GetParameters().Length == 1
            );

        private static readonly MethodInfo GetBuffer =
            typeof(EntityManager)
                .GetMethods(BindingFlags.Public | BindingFlags.Instance)
                .First(m => m.IsGenericMethod && m.Name == "GetBuffer" && m.GetParameters().Length == 2 &&
                            m.GetParameters()[0].ParameterType == typeof(Entity));


        private static readonly MethodInfo GetComponentData = typeof(EntityManager)
            .GetMethods(BindingFlags.Public | BindingFlags.Instance)
            .First(m => m.IsGenericMethod && m.Name == "GetComponentData" && m.GetParameters().Length == 1 &&
                        m.GetParameters()[0].ParameterType == typeof(Entity));


        private static void SaveFixedComponentDataArrayIntoDictBytes(Dictionary<Type, byte[]> typeToBytes,
            SaveArcheTypeInfo info, EntityQuery query)
        {
            // Use ToComponentDataArray to get all component data arrays
            foreach (var t in info.Types)
            {
                var elementSize = Marshal.SizeOf(t);

                var method = ToComponentDataArray!.MakeGenericMethod(t);

                // 调用 ToComponentDataArray<T>(query, Allocator.Temp)
                var handle = AllocatorManager.ConvertToAllocatorHandle(Allocator.Temp);
                var nativeArrayObj = method.Invoke(query, new object[] { handle });

                var bytesAll = SaveUtilities.NativeArrayBoxedToBytes(nativeArrayObj, elementSize);
                typeToBytes[t] = bytesAll;

                if (nativeArrayObj is IDisposable disposable)
                    disposable.Dispose();
            }
        }


        private static void SaveFixedComponentDataSingletonIntoDictBytes(Dictionary<Type, byte[]> typeToBytes,
            SaveArcheTypeInfo info, EntityManager em)
        {
            foreach (var t in info.Types)
            {
                // query singleton entity
                using var query = em.CreateEntityQuery(t);
                var entity = query.GetSingletonEntity();

                var genericMethod = GetComponentData!.MakeGenericMethod(t);
                var compData = genericMethod.Invoke(em, new object[] { entity });

                var size = Marshal.SizeOf(t);
                var bytes = new byte[size];
                var ptr = Marshal.AllocHGlobal(size);
                try
                {
                    Marshal.StructureToPtr(compData!, ptr, false);
                    Marshal.Copy(ptr, bytes, 0, size);
                }
                finally
                {
                    Marshal.FreeHGlobal(ptr);
                }

                typeToBytes[t] = bytes;
            }
        }


        public static async Task WriteFileHeaderAsync(FileStream fs, SaveArcheTypeInfo info,
            EntityQuery query = default)
        {
            var entityCount = info.saveEntityType == SaveEntityType.Prefab
                ? query.CalculateEntityCount()
                : info.Types.Count + info.BufferTypes.Count;
            // ArchetypeId
            await fs.WriteAsync(BitConverter.GetBytes((int)info.saveArcheType), 0, 4);
            // EntityCount
            await fs.WriteAsync(BitConverter.GetBytes(entityCount), 0, 4);

            if (info.saveEntityType == SaveEntityType.Prefab)
            {
                // Prefab Ids
                using var prefabIds = query.ToComponentDataArray<PrefabId>(Allocator.Persistent);
                var eSize = UnsafeUtility.SizeOf<PrefabId>();
                var bytes = prefabIds.Reinterpret<byte>(eSize).ToArray();
                var len = eSize * entityCount;
                await fs.WriteAsync(bytes, 0, len);
            }
        }

        public static async Task WriteFixedComponentDataArrayBytesAsync(FileStream fs,
            SaveArcheTypeInfo info, EntityManager em, EntityQuery query)
        {
            var typeToBytes = new Dictionary<Type, byte[]>();
            if (info.saveEntityType == SaveEntityType.Prefab)
            {
                SaveFixedComponentDataArrayIntoDictBytes(typeToBytes, info, query);
            }
            else
            {
                SaveFixedComponentDataSingletonIntoDictBytes(typeToBytes, info, em);
            }

            foreach (var t in info.Types)
            {
                var bytesAll = typeToBytes[t];
                await fs.WriteAsync(bytesAll, 0, bytesAll.Length);
            }
        }

        public static async Task WriteFixedBufferSingletonsAsync(
            FileStream fs,
            SaveArcheTypeInfo info,
            EntityManager em)
        {
            foreach (var bufferType in info.BufferTypes)
            {
                // Query singleton entity
                using var query = em.CreateEntityQuery(bufferType);
                var entity = query.GetSingletonEntity();

                var getBufferMethod = GetBuffer.MakeGenericMethod(bufferType);

                var bufferObj = getBufferMethod.Invoke(em, new object[] { entity, true })!;
                var bufferTypeInterface = bufferObj.GetType();

                var lengthProperty = bufferTypeInterface.GetProperty("Length")!;
                int length = (int)lengthProperty.GetValue(bufferObj)!;

                // 写入长度
                await fs.WriteAsync(BitConverter.GetBytes(length), 0, 4);

                if (length > 0)
                {
                    // 调用 AsNativeArray()
                    var toNativeArrayMethod = bufferTypeInterface.GetMethod("AsNativeArray")!;
                     bufferObj = getBufferMethod.Invoke(em, new object[] { entity, true })!;

                    var nativeArrayObj = toNativeArrayMethod.Invoke(bufferObj, null)!;

                    var elementSize = UnsafeUtility.SizeOf(bufferType);
                    var bytes = SaveUtilities.NativeArrayBoxedToBytes(nativeArrayObj, elementSize);

                    await fs.WriteAsync(bytes, 0, bytes.Length);

                    if (nativeArrayObj is IDisposable disposable)
                        disposable.Dispose();
                }
            }
        }


        public static async Task WriteFixedBufferOfSingleEntityAsync(FileStream fs, SaveArcheTypeInfo info,
            EntityManager em,
            Entity entity)
        {
            foreach (var bufferType in info.BufferTypes)
            {
                var getBufferMethod = GetBuffer!.MakeGenericMethod(bufferType);
                var elementSize = UnsafeUtility.SizeOf(bufferType);

                var bufferObj = getBufferMethod.Invoke(em, new object[] { entity, true });
                var bufferTypeInterface = bufferObj.GetType();
                var lengthProperty = bufferTypeInterface.GetProperty("Length")!;
                var length = (int)lengthProperty.GetValue(bufferObj)!;

                await fs.WriteAsync(BitConverter.GetBytes(length), 0, 4);
                if (length > 0)
                {
                    bufferObj = getBufferMethod.Invoke(em, new object[] { entity, true });
                    var toNativeArrayMethod = bufferTypeInterface.GetMethod("AsNativeArray")!;
                    var nativeArrayObj = toNativeArrayMethod.Invoke(bufferObj, null);

                    var bytes = SaveUtilities.NativeArrayBoxedToBytes(nativeArrayObj, elementSize);
                    await fs.WriteAsync(bytes, 0, bytes.Length);

                    if (nativeArrayObj is IDisposable disposable)
                        disposable.Dispose();
                }
            }
        }

        public static async Task WriteConditionalComponentsOfSingleEntityAsync(FileStream fs, SaveArcheTypeInfo info,
            EntityManager em,
            Entity entity)
        {
            foreach (var compType in info.ConditionalComponentTypes)
            {
                var hasComp = em.HasComponent(entity, compType);
                fs.WriteByte((byte)(hasComp ? 1 : 0));
                if (hasComp)
                {
                    var method = GetComponentData!.MakeGenericMethod(compType);
                    var compObj = method.Invoke(em, new object[] { entity });

                    var size = Marshal.SizeOf(compType);
                    var buffer = new byte[size];
                    var handle = GCHandle.Alloc(compObj, GCHandleType.Pinned);
                    try
                    {
                        Marshal.Copy(handle.AddrOfPinnedObject(), buffer, 0, size);
                    }
                    finally
                    {
                        handle.Free();
                    }
                    await fs.WriteAsync(buffer, 0, size);
                }
            }
        }
        /*
        public static async Task WriteConditionalComponentsOfSingleEntityAsync2(
            FileStream fs,
            SaveArcheTypeInfo info,
            EntityManager em,
            Entity entity)
        {
            foreach (var compType in info.ConditionalComponentTypes)
            {
                var hasComp = em.HasComponent(entity, compType);
                fs.WriteByte((byte)(hasComp ? 1 : 0));

                if (hasComp)
                {
                    var componentData = em.GetComponentData<IComponentData>(entity, compType);

                    unsafe
                    {
                        var size = UnsafeUtility.SizeOf(compType);
                        var ptr = UnsafeUtility.As<IComponentData, byte>(ref componentData);
                        var buffer = new byte[size];
                        // Copy from the component data's memory directly into the byte array
                        UnsafeUtility.CopyPtrToByteArray(ptr, buffer, 0, size);
                        await fs.WriteAsync(buffer, 0, size);
                    }
                }
            }
        }
        */

        public static async Task WriteConditionalBuffersOfSingleEntityAsync(FileStream fs, SaveArcheTypeInfo info,
            EntityManager em,
            Entity entity)
        {
            foreach (var bufferType in info.ConditionalBufferTypes)
            {
                var hasBuffer = em.HasComponent(entity, bufferType); // buffer 是 component
                fs.WriteByte((byte)(hasBuffer ? 1 : 0));
                if (!hasBuffer) continue;

                var getBufferMethod = GetBuffer!.MakeGenericMethod(bufferType);
                var elementSize = UnsafeUtility.SizeOf(bufferType);

                var bufferObj = getBufferMethod.Invoke(em, new object[] { entity, true });
                var bufferTypeInterface = bufferObj.GetType();
                var lengthProperty = bufferTypeInterface.GetProperty("Length")!;
                var length = (int)lengthProperty.GetValue(bufferObj)!;

                await fs.WriteAsync(BitConverter.GetBytes(length), 0, 4);

                if (length > 0)
                {
                    bufferObj = getBufferMethod.Invoke(em, new object[] { entity, true });
                    var toNativeArrayMethod = bufferTypeInterface.GetMethod("AsNativeArray")!;
                    var nativeArrayObj = toNativeArrayMethod.Invoke(bufferObj, null);

                    var bytes = SaveUtilities.NativeArrayBoxedToBytes(nativeArrayObj, elementSize);
                    await fs.WriteAsync(bytes, 0, bytes.Length);

                    if (nativeArrayObj is IDisposable disposable)
                        disposable.Dispose();
                }
            }
        }
    }
}