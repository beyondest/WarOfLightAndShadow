using System;
using SparFlame.GamePlaySystem.Building;
using SparFlame.GamePlaySystem.Resource;
using SparFlame.GamePlaySystem.Units;
using SparFlame.Test;
using Unity.Entities;
using UnityEngine;

namespace SparFlame.Database
{
    public class DatabaseManagerAuthoring : MonoBehaviour
    {

        public TestDatabaseSo so;

        private class DatabaseManagerAuthoringBaker : Baker<DatabaseManagerAuthoring>
        {
            public override void Bake(DatabaseManagerAuthoring authoring)
            {
                var entity1 = GetEntity(TransformUsageFlags.None);
                var buffer = AddBuffer<BuildingEntityPrefabData>(entity1);
                foreach (var buildingData in DatabaseManager.BuildingDatabaseSo.Items) // Bake all building entity prefabs to singleton buffer for further use
                {
                    buffer.Add(new BuildingEntityPrefabData
                    {
                        Type = buildingData.type,
                        Prefab = GetEntity(buildingData.prefab, TransformUsageFlags.Dynamic)
                    });
                }
                var b =AddBuffer<TestBuildingPrefab>(entity1);
                b.Add(new TestBuildingPrefab
                {
                    Prefab = GetEntity(authoring.so.prefab, TransformUsageFlags.Dynamic),
                });
                var entity2 = CreateAdditionalEntity(TransformUsageFlags.None);
                var buffer2 = AddBuffer<UnitEntityPrefabData>(entity2);
                foreach (var unitData in DatabaseManager.UnitDatabaseSo.Items)
                {
                    buffer2.Add(new UnitEntityPrefabData
                    {
                        Type = unitData.type,
                        Prefab = GetEntity(unitData.prefab, TransformUsageFlags.Dynamic)
                    });
                }
                
                var entity3 = CreateAdditionalEntity(TransformUsageFlags.None);
                var buffer3 = AddBuffer<ResourceEntityPrefabData>(entity3);
                foreach (var resourceData in DatabaseManager.ResourceDatabaseSo.Items)
                {
                    buffer3.Add(new ResourceEntityPrefabData
                    {
                        Type = resourceData.type,
                        Prefab = GetEntity(resourceData.prefab, TransformUsageFlags.Dynamic)
                    });
                }
            }
        }
    }


    public struct TestBuildingPrefab : IBufferElementData
    {
        public Entity Prefab;
    }
    public interface IEntityPrefabData<T> : IBufferElementData where T : Enum 
    {
        public Entity Prefab { get; set; }
        public T Type { get; set; }
    }

  
    public struct BuildingEntityPrefabData : IEntityPrefabData<BuildingType>
    {
        public Entity Prefab { get; set; }
        public BuildingType Type { get; set; }
    }

    public struct UnitEntityPrefabData : IEntityPrefabData<UnitType>
    {
        public Entity Prefab { get; set; }
        public UnitType Type { get; set; }
    }
    
    public struct ResourceEntityPrefabData : IBufferElementData,IEntityPrefabData<ResourceType>
    {
        public Entity Prefab { get; set; }
        public ResourceType Type { get; set; }
    }
}