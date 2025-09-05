using SparFlame.Components.SubGameplay;
using Unity.Entities;
using UnityEngine;

namespace SparFlame.Database
{
    public class UnitDatabaseAuthoring : MonoBehaviour
    {
        private class UnitDatabaseAuthoringBaker : Baker<UnitDatabaseAuthoring>
        {
            public override void Bake(UnitDatabaseAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.None);
                var buffer = AddBuffer<UnitEntityPrefabData>(entity);
              
                foreach (var unitData in DatabaseManager.UnitDatabaseSo.Items)
                {
                    buffer.Add(new UnitEntityPrefabData
                    {
                        Type = unitData.type,
                        Prefab = GetEntity(unitData.prefab, TransformUsageFlags.Dynamic),
                        GlobalIdx = unitData.id
                    });
                }
            }
        }
    }
}