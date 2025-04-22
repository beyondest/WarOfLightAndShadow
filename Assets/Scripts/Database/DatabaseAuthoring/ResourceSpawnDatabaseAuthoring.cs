using System.Collections.Generic;
using SparFlame.GamePlaySystem.Resource;
using Unity.Entities;
using UnityEngine;

namespace SparFlame.Database
{
    public class ResourceSpawnDatabaseAuthoring : MonoBehaviour
    {
        private class ResourceSpawnAuthoringBaker : Baker<ResourceSpawnDatabaseAuthoring>
        {
            public override void Bake(ResourceSpawnDatabaseAuthoring databaseAuthoring)
            {
                var entity = GetEntity(TransformUsageFlags.None);
                var db = DatabaseManager.ResourceSpawnDatabaseSo;
                var buffer = AddBuffer<ResourceSpawnData>(entity);
                
                foreach (var pair in db.timePoints)
                {
                    foreach (var pair2 in pair.Value)
                    {
                        buffer.Add(new ResourceSpawnData
                        {
                            TimePoints = pair.Key,
                            ResourceType = pair2.Key,
                            Amount = pair2.Value
                        });
                    }
                }

                var entity2 = CreateAdditionalEntity(TransformUsageFlags.None);
                var buffer2 = AddBuffer<RenewableResourceType>(entity2);
                var set = new HashSet<ResourceType>();
                
                foreach (var item in DatabaseManager.ResourceDatabaseSo.Items)
                {
                    if (!item.renewable) continue;
                    if (!set.Add(item.type))
                    {
                        Debug.LogError("Renewable resource type duplicated in resource database");
                        continue;
                    }
                    buffer2.Add(new RenewableResourceType
                    {
                        ResourceType = item.type
                    });

                }
            }
        }
    }

  
}