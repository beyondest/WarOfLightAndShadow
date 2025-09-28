using System;
using SparFlame.Components.General;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Random = Unity.Mathematics.Random;

namespace SparFlame.Systems.General.BasicControl.Init
{
    [UpdateInGroup(typeof(InitializationSystemGroup))]
    public partial struct InitRandomSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<EndInitializationEntityCommandBufferSystem.Singleton>();
            state.RequireForUpdate<AssignRandomRequest>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            new InitRandomJob
            {
                ElapsedTime = (float)SystemAPI.Time.ElapsedTime,
                ECB = SystemAPI.GetSingleton<EndInitializationEntityCommandBufferSystem.Singleton>().CreateCommandBuffer(state.WorldUnmanaged).AsParallelWriter(),
                
            }.ScheduleParallel();
        }
    }

    [BurstCompile]
    [WithNone(typeof(AssignGlobalSingleIDRequest))]
    [WithAll(typeof(AssignRandomRequest))]
    public partial struct InitRandomJob : IJobEntity
    {
        [ReadOnly] public float ElapsedTime;
        public EntityCommandBuffer.ParallelWriter ECB;

        private void Execute([ChunkIndexInQuery] int index, ref Rnd rnd, in GlobalSingleId singleId, Entity selfEntity)
        {
            var seed = GetSeed(singleId.value, selfEntity.Index, selfEntity.Version, ElapsedTime);
            rnd.value = new Random(seed);
            ECB.RemoveComponent<AssignRandomRequest>(index, selfEntity);
        }

        private uint GetSeed(long singleIdValue, int index, int version, float elapsedTime)
        {
            // Hash the components of the Entity (index and version) into a uint.
            // This is a robust way to get a unique identifier for the Entity.
            uint entityHash = math.hash(new int2(index, version));

            // Hash the long into a uint.
            // We split the long into two 32-bit integers to feed into the hash function.
            uint longHash = math.hash(new uint2((uint)(singleIdValue >> 32), (uint)singleIdValue));

            // Create a hash from the elapsed time.
            // Using `asuint()` converts the float's bit representation into an integer.
            uint timeHash = math.asuint(elapsedTime);

            // Combine all the hashes for a unique and well-distributed seed.
            return math.hash(new uint3(entityHash, longHash, timeHash));
        }
    }
}