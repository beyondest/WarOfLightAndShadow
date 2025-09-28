using SparFlame.Components.MainGameplay;
using Unity.Entities;
using UnityEngine;

namespace SparFlame.Database
{
    public class CityDatabaseAuthoring : MonoBehaviour
    {
        private class CityDatabaseAuthoringBaker : Baker<CityDatabaseAuthoring>
        {
            public override void Bake(CityDatabaseAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.None);
                var buffer = AddBuffer<CityEntityPrefabData>(entity);
                foreach (var cityDataItem in DatabaseManager.CityDatabaseSo.items)
                {
                    buffer.Add(new CityEntityPrefabData
                    {
                        Prefab = GetEntity(cityDataItem.prefab, TransformUsageFlags.Dynamic),
                        PrefabId = cityDataItem.id
                    });
                }
            }
        }
    }
}