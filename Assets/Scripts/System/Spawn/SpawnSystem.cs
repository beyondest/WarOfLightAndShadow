using Unity.Entities;
using UnityEngine;
using Unity.Transforms;
using Unity.Mathematics;


namespace SparFlame.System.Spawn
{
    [UpdateBefore(typeof(TransformSystemGroup))]
    public partial struct SpawnSystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<SpawnSystemConfig>();
            state.RequireForUpdate<Spawnable>();
            state.RequireForUpdate<SpawnedData>();
        }

        public void OnUpdate(ref SystemState state)
        {
            //var config = SystemAPI.GetSingleton<SpawnSystemConfig>();

            foreach (var spawnable in SystemAPI.Query<RefRO<SpawnedData>>().WithAll<Spawnable>())
            {

                if (!Input.GetKeyDown(spawnable.ValueRO.SpawnKey)) continue;
                var spawned = state.EntityManager.Instantiate(spawnable.ValueRO.SpawnPrefab);
                state.EntityManager.SetComponentData(spawned, new LocalTransform
                {
                    Position = spawnable.ValueRO.SpawnPos,
                    Rotation = quaternion.identity,
                    Scale = 1
                });
            }
        }
    }
}