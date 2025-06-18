using System;
using SparFlame.Components.SubGameplay;
using Unity.Android.Gradle.Manifest;
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
                var buffer2 = AddBuffer<UnitEntityPrefabData>(entity);
                if (!DatabaseManager.UnitDatabaseSo || DatabaseManager.UnitDatabaseSo.Items.Count == 0)
                {
                    throw new ArgumentException("UnitDatabase not found");
                }
                foreach (var unitData in DatabaseManager.UnitDatabaseSo.Items)
                {
                    buffer2.Add(new UnitEntityPrefabData
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