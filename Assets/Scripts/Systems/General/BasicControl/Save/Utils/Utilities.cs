using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.InteropServices;
using SparFlame.Components.General;
using SparFlame.Components.SubGameplay;
using SparFlame.Core;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace SparFlame.Systems.General.BasicControl
{


   
   

   
    public struct SaveUtilities
    {
        public const int NewGameSaveSlot = 999;
        public const int AutomaticSaveSlot = 0;
        
        #region Path

        private const string CitySubDataFolder = "CitySubData";
        private const string ArmyGroupSubDataFolder = "ArmySubData";
        private const string GameGeneralDataFolder = "GameGeneralData";
        private const string CityMainDataName = "CityMainData";
        private const string ArmyGroupMainDataName = "ArmyMainData";
        private const string GameMainDataName = "GameMainData";
        private const string QuickDataFile = "QuickData.sav";

        // public static long GetTmpIdForSaving(Entity entity)
        // {
        //     // Index 占低位（0~31），Version 占高位（32~63）
        //     return ((long)entity.Version << 32) | (uint)entity.Index;
        // }
        private const string SaveFolder = "SaveData";

        private static readonly string SaveRootFolder = Path.Combine(Application.persistentDataPath, SaveFolder);


        public static string GetSaveSlotFolder(int playerSaveSlot)
        {
            return Path.Combine(SaveRootFolder, "SaveSlot" + playerSaveSlot);
        }

        private static string GetQuickDataPath(int playerSaveSlot)
        {
            return Path.Combine(GetSaveSlotFolder(playerSaveSlot), QuickDataFile);
        }

        public static string GetCitySubDataFolder(int playerSaveSlot)
        {
            var saveRootFolder = GetSaveSlotFolder(playerSaveSlot);
            var cityRootFolder = Path.Combine(saveRootFolder, CitySubDataFolder);
            return cityRootFolder;
        }

        public static string GetCityUnitSubDataPath(long singleId, int playerSaveSlot, bool isTmp)
        {
            var saveRootFolder = GetSaveSlotFolder(playerSaveSlot);
            var cityRootFolder = Path.Combine(saveRootFolder, CitySubDataFolder);
            if (!Directory.Exists(cityRootFolder))
                Directory.CreateDirectory(cityRootFolder);
            var finalPath = isTmp
                ? Path.Combine(cityRootFolder, $"{singleId}Unit.sav.tmp")
                : Path.Combine(cityRootFolder, $"{singleId}Unit.sav");
            return finalPath;
        }

        public static string GetCityBuildingSubDataPath(long singleId, int playerSaveSlot, bool isTmp)
        {
            var saveRootFolder = GetSaveSlotFolder(playerSaveSlot);
            var cityRootFolder = Path.Combine(saveRootFolder, CitySubDataFolder);
            if (!Directory.Exists(cityRootFolder))
                Directory.CreateDirectory(cityRootFolder);
            var finalPath = isTmp
                ? Path.Combine(cityRootFolder, $"{singleId}Building.sav.tmp")
                : Path.Combine(cityRootFolder, $"{singleId}Building.sav");
            return finalPath;
        }

        public static string GetCitySubDataPath(int cityId, int playerSaveSlot, bool isTmp)
        {
            var saveRootFolder = GetSaveSlotFolder(playerSaveSlot);
            var cityRootFolder = Path.Combine(saveRootFolder, CitySubDataFolder);
            if (!Directory.Exists(cityRootFolder))
                Directory.CreateDirectory(cityRootFolder);
            var finalPath = isTmp
                ? Path.Combine(cityRootFolder, $"{cityId}.sav.tmp")
                : Path.Combine(cityRootFolder, $"{cityId}.sav");
            return finalPath;
        }

        public static string GetCitySubDataPath(long cityId, int playerSaveSlot, bool isTmp)
        {
            var saveRootFolder = GetSaveSlotFolder(playerSaveSlot);
            var cityRootFolder = Path.Combine(saveRootFolder, CitySubDataFolder);
            if (!Directory.Exists(cityRootFolder))
                Directory.CreateDirectory(cityRootFolder);
            var finalPath = isTmp
                ? Path.Combine(cityRootFolder, $"{cityId}.sav.tmp")
                : Path.Combine(cityRootFolder, $"{cityId}.sav");
            return finalPath;
        }

        public static string GetArmyGroupSubDataFolder(int playerSaveSlot)
        {
            var saveRootFolder = GetSaveSlotFolder(playerSaveSlot);
            var armyGroupRootFolder = Path.Combine(saveRootFolder, ArmyGroupSubDataFolder);
            return armyGroupRootFolder;
        }

        public static string GetArmyGroupSubDataPath(long saveId, int playerSaveSlot, bool isTmp)
        {
            var saveRootFolder = GetSaveSlotFolder(playerSaveSlot);
            var armyGroupRootFolder = Path.Combine(saveRootFolder, ArmyGroupSubDataFolder);
            if (!Directory.Exists(armyGroupRootFolder))
                Directory.CreateDirectory(armyGroupRootFolder);
            var finalPath = isTmp
                ? Path.Combine(armyGroupRootFolder, $"{saveId}.sav.tmp")
                : Path.Combine(armyGroupRootFolder, $"{saveId}.sav");
            return finalPath;
        }

        public static string GetCityMainDataPath(int playerSaveSlot)
        {
            var saveRootFolder = GetSaveSlotFolder(playerSaveSlot);
            var generalDataFolder = Path.Combine(saveRootFolder, GameGeneralDataFolder);
            if (!Directory.Exists(generalDataFolder))
                Directory.CreateDirectory(generalDataFolder);
            var cityMainDataPath = Path.Combine(generalDataFolder, $"{CityMainDataName}.sav");
            return cityMainDataPath;
        }

        public static string GetArmyGroupMainDataPath(int playerSaveSlot)
        {
            var saveRootFolder = GetSaveSlotFolder(playerSaveSlot);
            var generalDataFolder = Path.Combine(saveRootFolder, GameGeneralDataFolder);
            if (!Directory.Exists(generalDataFolder))
                Directory.CreateDirectory(generalDataFolder);
            var armyGroupMainDataPath = Path.Combine(generalDataFolder, $"{ArmyGroupMainDataName}.sav");
            return armyGroupMainDataPath;
        }

        public static string GetGameMainDataPath(int playerSaveSlot)
        {
            var saveRootFolder = GetSaveSlotFolder(playerSaveSlot);
            var generalDataFolder = Path.Combine(saveRootFolder, GameGeneralDataFolder);
            if (!Directory.Exists(generalDataFolder))
                Directory.CreateDirectory(generalDataFolder);
            var gameMainDataPath = Path.Combine(generalDataFolder, $"{GameMainDataName}.sav");
            return gameMainDataPath;
        }

        #endregion

        #region Reflect Methods

        /// 
        /// 将 NativeArray T（boxed）通过 Reinterpret byte (elementSize).ToArray() 转为 managed byte[].
        /// nativeArrayObj: 反射得到的 NativeArray T 的 boxed 实例
        /// elementSize: 单个元素的字节大小（可用 Marshal.SizeOf(type) 获得）
        public static byte[] NativeArrayBoxedToBytes(object nativeArrayObj, int elementSize)
        {
            if (nativeArrayObj == null) throw new ArgumentNullException(nameof(nativeArrayObj));

            // 调用 nativeArray.Reinterpret<byte>(elementSize)
            var nativeArrayType = nativeArrayObj.GetType(); // NativeArray<T>
            var reinterpretMethod = nativeArrayType.GetMethod("Reinterpret", new[] { typeof(int) });
            var reinterpretGeneric = reinterpretMethod!.MakeGenericMethod(typeof(byte));
            var nativeByteArrayObj =
                reinterpretGeneric.Invoke(nativeArrayObj, new object[] { elementSize }); // NativeArray<byte>

            // 调用 NativeArray<byte>.ToArray()
            var toArrayMethod = nativeByteArrayObj.GetType().GetMethod("ToArray", Type.EmptyTypes);
            var managedBytes = (byte[])toArrayMethod!.Invoke(nativeByteArrayObj, null);

            // 不在这里 Dispose reinterpret 结果（它和原 nativeArray 指向同一内存）
            return managedBytes;
        }


        private static readonly Dictionary<Type, Action<EntityCommandBuffer, Entity, object>> AddComponentCache = new();
        private static readonly Dictionary<Type, Action<EntityCommandBuffer, Entity, object>> SetComponentCache = new();

        private static readonly Dictionary<Type, Action<EntityCommandBuffer, Entity, object>> AppendToBufferCache =
            new();

        private static readonly Dictionary<Type, Action<EntityCommandBuffer, Entity>> AddBufferCache = new();
        private static readonly Dictionary<Type, Action<EntityCommandBuffer, Entity>> SetBufferCache = new();


        private static readonly MethodInfo AddComponent = typeof(EntityCommandBuffer)
            .GetMethods(BindingFlags.Public | BindingFlags.Instance)
            .First(m => m.Name == "AddComponent" &&
                        m.GetParameters().Length == 2);

        private static readonly MethodInfo SetComponent = typeof(EntityCommandBuffer)
            .GetMethods(BindingFlags.Public | BindingFlags.Instance)
            .First(m => m.Name == "SetComponent" &&
                        m.GetParameters().Length == 2);

        private static readonly MethodInfo AppendToBuffer = typeof(EntityCommandBuffer)
            .GetMethods(BindingFlags.Public | BindingFlags.Instance)
            .First(m => m.Name == "AppendToBuffer" &&
                        m.GetParameters().Length == 2);

        private static readonly MethodInfo AddBuffer = typeof(EntityCommandBuffer)
            .GetMethods(BindingFlags.Public | BindingFlags.Instance)
            .First(m => m.Name == "AddBuffer" &&
                        m.GetParameters().Length == 1);

        private static readonly MethodInfo SetBuffer = typeof(EntityCommandBuffer)
            .GetMethods(BindingFlags.Public | BindingFlags.Instance)
            .First(m => m.Name == "SetBuffer" &&
                        m.GetParameters().Length == 1);

        public static void ECBAddComponentCached(EntityCommandBuffer ecb, Entity e, Type type, object component)
        {
            if (!AddComponentCache.TryGetValue(type, out var action))
            {
                var methodInfo = AddComponent!.MakeGenericMethod(type);

                var ecbParam = Expression.Parameter(typeof(EntityCommandBuffer), "ecb");
                var entityParam = Expression.Parameter(typeof(Entity), "e");
                var compParam = Expression.Parameter(typeof(object), "comp");

                var body = Expression.Call(
                    ecbParam,
                    methodInfo,
                    entityParam,
                    Expression.Convert(compParam, type)
                );

                action = Expression.Lambda<Action<EntityCommandBuffer, Entity, object>>(body,
                    ecbParam, entityParam, compParam).Compile();

                AddComponentCache[type] = action;
            }

            action(ecb, e, component);
        }

        public static void ECBSetComponentCached(EntityCommandBuffer ecb, Entity e, Type type, object component)
        {
            if (!SetComponentCache.TryGetValue(type, out var action))
            {
                var methodInfo = SetComponent!.MakeGenericMethod(type);

                var ecbParam = Expression.Parameter(typeof(EntityCommandBuffer), "ecb");
                var entityParam = Expression.Parameter(typeof(Entity), "e");
                var compParam = Expression.Parameter(typeof(object), "comp");

                var body = Expression.Call(
                    ecbParam,
                    methodInfo,
                    entityParam,
                    Expression.Convert(compParam, type)
                );

                action = Expression.Lambda<Action<EntityCommandBuffer, Entity, object>>(body,
                    ecbParam, entityParam, compParam).Compile();

                SetComponentCache[type] = action;
            }

            action(ecb, e, component);
        }

        public static void ECBAppendToBufferCached(EntityCommandBuffer ecb, Entity e, Type type, object comp)
        {
            if (!AppendToBufferCache.TryGetValue(type, out var action))
            {
                var methodInfo = AppendToBuffer!.MakeGenericMethod(type);

                var ecbParam = Expression.Parameter(typeof(EntityCommandBuffer), "ecb");
                var entityParam = Expression.Parameter(typeof(Entity), "e");
                var compParam = Expression.Parameter(typeof(object), "comp");

                var body = Expression.Call(
                    ecbParam,
                    methodInfo,
                    entityParam,
                    Expression.Convert(compParam, type)
                );

                action = Expression.Lambda<Action<EntityCommandBuffer, Entity, object>>(body,
                    ecbParam, entityParam, compParam).Compile();

                AppendToBufferCache[type] = action;
            }

            action(ecb, e, comp);
        }

        public static void ECBAddBufferCached(EntityCommandBuffer ecb, Entity e, Type type)
        {
            if (!AddBufferCache.TryGetValue(type, out var action))
            {
                var methodInfo = AddBuffer!.MakeGenericMethod(type);

                var ecbParam = Expression.Parameter(typeof(EntityCommandBuffer), "ecb");
                var entityParam = Expression.Parameter(typeof(Entity), "e");

                var body = Expression.Call(
                    ecbParam,
                    methodInfo,
                    entityParam
                );

                action = Expression.Lambda<Action<EntityCommandBuffer, Entity>>(body,
                    ecbParam, entityParam).Compile();

                AddBufferCache[type] = action;
            }

            action(ecb, e);
        }

        public static void ECBSetBufferCached(EntityCommandBuffer ecb, Entity e, Type type)
        {
            if (!SetBufferCache.TryGetValue(type, out var action))
            {
                var methodInfo = SetBuffer!.MakeGenericMethod(type);

                var ecbParam = Expression.Parameter(typeof(EntityCommandBuffer), "ecb");
                var entityParam = Expression.Parameter(typeof(Entity), "e");

                var body = Expression.Call(
                    ecbParam,
                    methodInfo,
                    entityParam
                );

                action = Expression.Lambda<Action<EntityCommandBuffer, Entity>>(body,
                    ecbParam, entityParam).Compile();

                SetBufferCache[type] = action;
            }

            action(ecb, e);
        }

        #endregion


      
        
        public static void WriteQuickData(int playerSaveSlot, RiftGameFileQuickData data)
        {
            var quickDataPath = GetQuickDataPath(playerSaveSlot);
            var size = Marshal.SizeOf<RiftGameFileQuickData>();
            var buffer = new byte[size];
            // 将 struct 拷贝到 byte[]
            unsafe
            {
                fixed (byte* ptr = buffer)
                {
                    Marshal.StructureToPtr(data, (IntPtr)ptr, false);
                }
            }
            // 写文件
            using var fs = new FileStream(quickDataPath, FileMode.Create, FileAccess.Write, FileShare.None);
            fs.Write(buffer, 0, size);
        }

        public static DateTime GetQuickDataLastWriteTime(int playerSaveSlot)
        {
            var quickDataPath = GetQuickDataPath(playerSaveSlot);
            return File.GetLastWriteTime(quickDataPath);
        }
        // 读取
        public static bool TryGetQuickData(int playerSaveSlot, out RiftGameFileQuickData data)
        {
            var quickDataPath = GetQuickDataPath(playerSaveSlot);
            data = new RiftGameFileQuickData();
            if (!File.Exists(quickDataPath))
            {
                return false;
            }
            var size = Marshal.SizeOf<RiftGameFileQuickData>();
            var buffer = new byte[size];
            using (var fs = new FileStream(quickDataPath, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                var read = fs.Read(buffer, 0, size);
                if (read < size)
                    throw new EndOfStreamException("Unexpected end of stream");
            }
            unsafe
            {
                fixed (byte* ptr = buffer)
                {
                    data = Marshal.PtrToStructure<RiftGameFileQuickData>((IntPtr)ptr);
                }
            }
            return true;
        }
        public static void InitializeSaveSlotFolder(string path, int slot)
        {
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }
            else
            {
                FileUtils.ClearDirectory(path);
            }
            var citySubDataFolder = SaveUtilities.GetCitySubDataFolder(slot);
            var armyGroupSubDataFolder = SaveUtilities.GetArmyGroupSubDataFolder(slot);
            if (!Directory.Exists(citySubDataFolder))
                Directory.CreateDirectory(citySubDataFolder);
            if (!Directory.Exists(armyGroupSubDataFolder))
                Directory.CreateDirectory(armyGroupSubDataFolder);
        }
    }
}