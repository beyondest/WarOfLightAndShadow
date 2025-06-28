using System;
using System.IO;
using SparFlame.Core.Utils;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Mathematics;
using BinaryReader = Unity.Entities.Serialization.BinaryReader;
using BinaryWriter = Unity.Entities.Serialization.BinaryWriter;

namespace SparFlame.Systems.General.BasicControl
{
    [Serializable]
    public struct SeGlobalId : IComponentData
    {
        public int value;
    }

    [Serializable]
    public struct SeTransform : IComponentData
    {
        public float3 position;
        public quaternion rotation;
        public float scale;
    }


    
    
    
  

    [GenerateTestsForBurstCompatibility]
    public unsafe struct BurstableMemoryBinaryWriter : BinaryWriter
    {
        private readonly byte* buffer;
        private readonly int capacity;
        private long position;

        public long Position
        {
            get => position;
            set
            {
#if ENABLE_UNITY_COLLECTIONS_CHECKS || UNITY_DOTS_DEBUG
                if (value < 0 || value > capacity)
                    throw new ArgumentOutOfRangeException($"Position out of range.");
#endif
                position = value;
            }
        }

        public long Length => position;

        public BurstableMemoryBinaryWriter(void* buffer, int capacity)
        {
            this.buffer = (byte*)buffer;
            this.capacity = capacity;
            this.position = 0;
        }

        public void WriteBytes(void* data, int bytes)
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS || UNITY_DOTS_DEBUG
            if (position + bytes > capacity)
                throw new InvalidOperationException("Buffer overflow in BurstableMemoryBinaryWriter");
#endif
            UnsafeUtility.MemCpy(buffer + position, data, bytes);
            position += bytes;
        }

        public void Dispose()
        {
            // Nothing to dispose. Caller must manage memory.
        }

        public void GetData(out byte* ptr, out int length)
        {
            ptr = buffer;
            length = (int)position;
        }
    }

    
    public unsafe class StreamBinaryWriter : BinaryWriter
    {
        private readonly Stream stream;
        private readonly byte[] buffer;

        public long Position
        {
            get => stream.Position;
            set => stream.Position = value;
        }

        public StreamBinaryWriter(string fileName, int bufferSize = 65536)
        {
            stream = File.Open(fileName, FileMode.Create, FileAccess.Write);
            buffer = new byte[bufferSize];
        }

        public void Dispose()
        {
            stream.Dispose();
        }

        public void WriteBytes(void* data, int bytes)
        {
            int remaining = bytes;
            int bufferSize = buffer.Length;

            fixed (byte* fixedBuffer = buffer)
            {
                while (remaining != 0)
                {
                    int bytesToWrite = math.min(remaining, bufferSize);
                    UnsafeUtility.MemCpy(fixedBuffer, data, bytesToWrite);
                    stream.Write(buffer, 0, bytesToWrite);
                    data = (byte*)data + bytesToWrite;
                    remaining -= bytesToWrite;
                }
            }
        }

        public long Length => stream.Length;
    }


    public unsafe class StreamBinaryReader : BinaryReader
    {
#if UNITY_EDITOR
        private readonly Stream stream;
        private readonly byte[] buffer;
        public long Position
        {
            get => stream.Position;
            set => stream.Position = value;
        }
#else
        public long Position { get; set; }
#endif

        public StreamBinaryReader(string filePath, long bufferSize = 65536)
        {
            if (string.IsNullOrEmpty(filePath))
                throw new ArgumentException("The filepath can neither be null nor empty", nameof(filePath));

#if UNITY_EDITOR
            stream = File.Open(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
            buffer = new byte[bufferSize];
#else
            Position = 0;
#endif
        }

        public void Dispose()
        {
#if UNITY_EDITOR
            stream.Dispose();
#endif
        }

        public void ReadBytes(void* data, int bytes)
        {
#if UNITY_EDITOR
            int remaining = bytes;
            int bufferSize = buffer.Length;

            fixed (byte* fixedBuffer = buffer)
            {
                while (remaining != 0)
                {
                    int read = stream.Read(buffer, 0, Math.Min(remaining, bufferSize));
                    remaining -= read;
                    UnsafeUtility.MemCpy(data, fixedBuffer, read);
                    data = (byte*)data + read;
                }
            }
#else
            var readCmd = new ReadCommand
            {
                Size = bytes, Offset = Position, Buffer = data
            };
            Assert.IsFalse(string.IsNullOrEmpty(FilePath));
#if ENABLE_PROFILER
            // When AsyncReadManagerMetrics are available, mark up the file read for more informative IO metrics.
            // Metrics can be retrieved by AsyncReadManagerMetrics.GetMetrics
            var readHandle =
 AsyncReadManager.Read(FilePath, &readCmd, 1, subsystem: AssetLoadingSubsystem.EntitiesStreamBinaryReader);
#else
            var readHandle = AsyncReadManager.Read(FilePath, &readCmd, 1);
#endif
            readHandle.JobHandle.Complete();

            if (readHandle.Status != ReadStatus.Complete)
            {
                throw new IOException($"Failed to read from {FilePath}!");
            }
            Position += bytes;
#endif
        }
    }
    
    public struct SaveUtilities
    {
        private const string CitySubDataFolder = "CitySubData";
        private const string ArmyGroupSubDataFolder = "ArmySubData";
        private const string GameGeneralDataFolder = "GameGeneralData";
        private const string CityMainDataName = "CityMainData";
        private const string ArmyGroupMainDataName = "ArmyMainData";
        private const string GameMainDataName = "GameMainData";
        public static long GetTmpIdForSaving(Entity entity)
        {
            // Index 占低位（0~31），Version 占高位（32~63）
            return ((long)entity.Version << 32) | (uint)entity.Index;
        }

        public static string GetCitySubDataPath(int cityId, int playerSaveSlot)
        {
            var saveRootFolder = FolderPathUtils.GetPlayerSaveSlotFolder(playerSaveSlot);
            var cityRootFolder = Path.Combine(saveRootFolder, CitySubDataFolder);
            if (!Directory.Exists(cityRootFolder))
                Directory.CreateDirectory(cityRootFolder);
            var finalPath = Path.Combine(cityRootFolder, $"{cityId}.sav"); 
            return finalPath;
        }

        public static string GetArmyGroupSubDataPath(long saveId, int playerSaveSlot)
        {
            var saveRootFolder = FolderPathUtils.GetPlayerSaveSlotFolder(playerSaveSlot);
            var armyGroupRootFolder = Path.Combine(saveRootFolder, ArmyGroupSubDataFolder);
            if (!Directory.Exists(armyGroupRootFolder))
                Directory.CreateDirectory(armyGroupRootFolder);
            var finalPath = Path.Combine(armyGroupRootFolder, $"{saveId}.sav");
            return finalPath;
        }

        public static string GetCityMainDataPath(int playerSaveSlot)
        {
            var saveRootFolder = FolderPathUtils.GetPlayerSaveSlotFolder(playerSaveSlot);
            var generalDataFolder = Path.Combine(saveRootFolder, GameGeneralDataFolder);
            if (!Directory.Exists(generalDataFolder))
                Directory.CreateDirectory(generalDataFolder);
            var cityMainDataPath = Path.Combine(generalDataFolder, $"{CityMainDataName}.sav");
            return cityMainDataPath;
        }

        public static string GetArmyGroupMainDataPath(int playerSaveSlot)
        {
            var saveRootFolder = FolderPathUtils.GetPlayerSaveSlotFolder(playerSaveSlot);
            var generalDataFolder = Path.Combine(saveRootFolder, GameGeneralDataFolder);
            if (!Directory.Exists(generalDataFolder))
                Directory.CreateDirectory(generalDataFolder);
            var armyGroupMainDataPath = Path.Combine(generalDataFolder, $"{ArmyGroupMainDataName}.sav");
            return armyGroupMainDataPath;
        }

        public static string GetGameMainDataPath(int playerSaveSlot)
        {
            var saveRootFolder = FolderPathUtils.GetPlayerSaveSlotFolder(playerSaveSlot);
            var generalDataFolder = Path.Combine(saveRootFolder, GameGeneralDataFolder);
            if (!Directory.Exists(generalDataFolder))
                Directory.CreateDirectory(generalDataFolder);
            var gameMainDataPath = Path.Combine(generalDataFolder, $"{GameMainDataName}.sav");
            return gameMainDataPath;
        }
        
    }
    
    

    public struct IdData : IComponentData
    {
        public int Value;
    }
    
}