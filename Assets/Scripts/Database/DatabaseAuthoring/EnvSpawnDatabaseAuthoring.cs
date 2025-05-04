using SparFlame.GamePlaySystem.RandomSpawn.GamePlaySystem.Functionality.RandomSpawn;
using Unity.Entities;
using UnityEngine;

namespace SparFlame.Database
{
    public class EnvSpawnDatabaseAuthoring : MonoBehaviour
    {
        private class EnvSpawnDatabaseAuthoringBaker : Baker<EnvSpawnDatabaseAuthoring>
        {
            public override void Bake(EnvSpawnDatabaseAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.None);
                var buffer = AddBuffer<EnvSpawnPrefabData>(entity);
                var items = DatabaseManager.EnvSpawnDatabaseSo.items;
                foreach (var item in items)
                {
                    buffer.Add(new EnvSpawnPrefabData
                    {
                        Amount = item.amount,
                        Prefab = GetEntity(item.prefab,TransformUsageFlags.Renderable),
                        Prob = item.prob,
                        Type = item.type
                    });
                }
                
            }
        }
    }
}