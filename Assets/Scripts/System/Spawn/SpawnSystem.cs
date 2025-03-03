using Unity.Entities;
using UnityEngine;
using Unity.Transforms;
using Unity.Mathematics;


namespace SparFlame.System.Spawn
{
    public partial struct SpawnSystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<SpawnSystemData>();
            state.RequireForUpdate<SpawnSystemConfig>();
            state.RequireForUpdate<PrefabDisableChildIndices>();
        }



        public void OnUpdate(ref SystemState state)
        {
            var config = SystemAPI.GetSingleton<SpawnSystemConfig>();
            var data = SystemAPI.GetSingleton<SpawnSystemData>();
            var prefabDisableChildIndices = SystemAPI.GetSingletonBuffer<PrefabDisableChildIndices>();
            
            if (!Input.GetKey(config.SpawnKey)) return;

            var bufferLookup = SystemAPI.GetBufferLookup<LinkedEntityGroup>();
            
            var spawned = state.EntityManager.Instantiate(config.SpawnPrefab);
            state.EntityManager.SetComponentData(spawned, new LocalTransform
            {
                Position = data.SpawnPos,
                Rotation = quaternion.identity,
                Scale = 1
            });
            
            if (bufferLookup.TryGetBuffer(spawned, out var linkedEntities))
            {
                foreach (var disableIndex in prefabDisableChildIndices)
                {
                    if (disableIndex.Value >= linkedEntities.Length)
                    {
                        continue;
                    }
                    state.EntityManager.SetEnabled(linkedEntities[disableIndex.Value].Value,false);
                }
            }
        }
    }
}