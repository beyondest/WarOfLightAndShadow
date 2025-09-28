using System;
using System.IO;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Mathematics;
using BinaryReader = Unity.Entities.Serialization.BinaryReader;
using BinaryWriter = Unity.Entities.Serialization.BinaryWriter;

namespace SparFlame.Systems.General.BasicControl
{
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
}