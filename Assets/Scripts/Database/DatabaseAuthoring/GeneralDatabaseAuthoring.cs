/*using SparFlame.Components.General;
using SparFlame.Components.SubGameplay;
using Unity.Entities;
using UnityEngine;

namespace SparFlame.Database
{
    public class GeneralDatabaseAuthoring : MonoBehaviour
    {
        private class DatabaseManagerAuthoringBaker : Baker<GeneralDatabaseAuthoring>
        {
            public override void Bake(GeneralDatabaseAuthoring authoring)
            {
                var entity1 = GetEntity(TransformUsageFlags.None);
                var buffer = AddBuffer<BuildingEntityPrefabData>(entity1);
                foreach (var buildingData in DatabaseManager.BuildingDatabaseSo.Items) // Bake all building entity prefabs to singleton buffer for further use
                {
                    buffer.Add(new BuildingEntityPrefabData
                    {
                        Type = buildingData.type,
                        Prefab = GetEntity(buildingData.prefab, TransformUsageFlags.Dynamic),
                        GlobalIdx = buildingData.id
                    });
                }
          
                var entity2 = CreateAdditionalEntity(TransformUsageFlags.None);
                var buffer2 = AddBuffer<UnitEntityPrefabData>(entity2);
                foreach (var unitData in DatabaseManager.UnitDatabaseSo.Items)
                {
                    buffer2.Add(new UnitEntityPrefabData
                    {
                        Type = unitData.type,
                        Prefab = GetEntity(unitData.prefab, TransformUsageFlags.Dynamic),
                        GlobalIdx = unitData.id
                    });
                }
                
                var entity3 = CreateAdditionalEntity(TransformUsageFlags.None);
                var buffer3 = AddBuffer<ResourceEntityPrefabData>(entity3);
                foreach (var resourceData in DatabaseManager.ResourceDatabaseSo.Items)
                {
                    buffer3.Add(new ResourceEntityPrefabData
                    {
                        Type = resourceData.type,
                        Prefab = GetEntity(resourceData.prefab, TransformUsageFlags.Dynamic),
                        Probability = resourceData.prob,
                        AmountRange = resourceData.amountRange,
                        GlobalIdx = resourceData.id
                    });
                }
            }
        }
    }
}*/