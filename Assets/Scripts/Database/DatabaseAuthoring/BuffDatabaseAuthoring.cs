using SparFlame.GamePlaySystem.Interact;
using Unity.Entities;
using UnityEngine;

namespace SparFlame.Database
{
    public class BuffDatabaseAuthoring : MonoBehaviour
    {
        private class BuffDatabaseAuthoringBaker : Baker<BuffDatabaseAuthoring>
        {
            public override void Bake(BuffDatabaseAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.None);
                var buffer = AddBuffer<BuffPrefabDataPair>(entity);
                var items = DatabaseManager.BuffDatabaseSo.items;
                foreach (var item in items)
                {
                    buffer.Add(new BuffPrefabDataPair
                    {
                        Prefab = GetEntity(item.prefab, TransformUsageFlags.Dynamic),
                        Name = item.buffName,
                        BuffType = item.buffType,
                        Filter = item.filter
                    });
                }
            }
        }
    }
}