using System;
using SparFlame.Components.SubGameplay;
using Unity.Entities;
using UnityEngine;

namespace SparFlame.Database
{
    public class BuildingDatabaseAuthoring : MonoBehaviour
    {
        private class BuildingDatabaseAuthoringBaker : Baker<BuildingDatabaseAuthoring>
        {
            public override void Bake(BuildingDatabaseAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.None);
                var buffer = AddBuffer<BuildingEntityPrefabData>(entity);
               

                foreach (var buildingData in
                         DatabaseManager.BuildingDatabaseSo
                             .Items) // Bake all building entity prefabs to singleton buffer for further use
                {
                    buffer.Add(new BuildingEntityPrefabData
                    {
                        Type = buildingData.type,
                        Prefab = GetEntity(buildingData.prefab, TransformUsageFlags.Dynamic),
                        GlobalIdx = buildingData.id
                    });
                }
            }
        }
    }
}