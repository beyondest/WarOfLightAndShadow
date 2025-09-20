using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;
using System.Linq;

public static class ComponentReflectionUtil
{
    // 缓存 MethodInfo
    static readonly MethodInfo _getCompMethod = typeof(EntityManager)
        .GetMethods(BindingFlags.Public | BindingFlags.Instance)
        .First(m => m.IsGenericMethod && m.Name == "GetComponentData" && m.GetParameters().Length == 1 && m.GetParameters()[0].ParameterType == typeof(Entity));
    static readonly MethodInfo _setCompMethod = typeof(EntityManager)
        .GetMethods(BindingFlags.Public | BindingFlags.Instance)
        .First(m => m.IsGenericMethod && m.Name == "SetComponentData" && m.GetParameters().Length == 2 && m.GetParameters()[0].ParameterType == typeof(Entity));

    static readonly Dictionary<Type, MethodInfo> _getCompCache = new();
    static readonly Dictionary<Type, MethodInfo> _setCompCache = new();

    public static object GetComponentBoxed(EntityManager em, Entity e, Type componentType)
    {
        if (!_getCompCache.TryGetValue(componentType, out var mi))
        {
            mi = _getCompMethod.MakeGenericMethod(componentType);
            _getCompCache[componentType] = mi;
        }
        return mi.Invoke(em, new object[] { e });
    }

    public static void SetComponentBoxed(EntityManager em, Entity e, object componentBoxed, Type componentType)
    {
        if (!_setCompCache.TryGetValue(componentType, out var mi))
        {
            mi = _setCompMethod.MakeGenericMethod(componentType);
            _setCompCache[componentType] = mi;
        }
        mi.Invoke(em, new object[] { e, componentBoxed });
    }
    

    // ---------- struct <-> bytes via Marshal ----------
    // 注意：componentType 必须为非托管的 blittable struct（没有引用类型字段）
    public static byte[] StructToBytes(object boxedStruct, Type componentType)
    {
        if (boxedStruct == null) throw new ArgumentNullException(nameof(boxedStruct));
        int size = Marshal.SizeOf(componentType);
        IntPtr buffer = Marshal.AllocHGlobal(size);
        try
        {
            // 把托管 struct 拷到非托管内存
            Marshal.StructureToPtr(boxedStruct, buffer, false);
            var result = new byte[size];
            Marshal.Copy(buffer, result, 0, size);
            return result;
        }
        finally
        {
            Marshal.FreeHGlobal(buffer);
        }
    }

    public static object BytesToStruct(byte[] bytes, Type componentType)
    {
        int size = Marshal.SizeOf(componentType);
        if (bytes.Length != size)
            throw new ArgumentException($"bytes length {bytes.Length} != struct size {size}");

        IntPtr buffer = Marshal.AllocHGlobal(size);
        try
        {
            Marshal.Copy(bytes, 0, buffer, size);
            var obj = Marshal.PtrToStructure(buffer, componentType);
            return obj;
        }
        finally
        {
            Marshal.FreeHGlobal(buffer);
        }
    }
}
