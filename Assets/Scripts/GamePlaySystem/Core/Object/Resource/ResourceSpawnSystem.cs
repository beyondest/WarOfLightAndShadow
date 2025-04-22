using System;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;

namespace SparFlame.GamePlaySystem.Resource
{
    public partial struct ResourceSpawnSystem : ISystem
    {
        // TimePoints to resource type to amount
        private NativeHashMap<int, NativeHashMap<int, int>> _resourceSpawnDatabase;
        private NativeHashSet<int> _renewableResources;
        
        
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<ResourceSpawnSystemConfig>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            // var config = SystemAPI.GetSingleton<ResourceSpawnSystemConfig>();
            // if (!_resourceSpawnDatabase.IsCreated)
            // {
            //     if (SystemAPI.HasSingleton<ResourceSpawnData>())
            //     {
            //         _resourceSpawnDatabase = new NativeHashMap<int, NativeHashMap<int, int>>(5, Allocator.Persistent);
            //         var buffer = SystemAPI.GetSingletonBuffer<ResourceSpawnData>();
            //         foreach (var data in buffer)
            //         {
            //         }
            //     }
            //     else
            //     {
            //         return;
            //     }
            // }
        }

        [BurstCompile]
        public void OnDestroy(ref SystemState state)
        {
            if (_resourceSpawnDatabase.IsCreated)
                _resourceSpawnDatabase.Dispose();
        }
    }
}