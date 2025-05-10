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
                var db = DatabaseManager.ResourceWaveSpawnDatabaseSo;
                var buffer = AddBuffer<ResourceSpawnData>(entity);
                
                foreach (var pair in db.items)
                {
                    foreach (var pair2 in pair.pairs)
                    {
                        buffer.Add(new ResourceSpawnData
                        {
                            TimePoints = pair.timePoint,
                            ResourceType = pair2.resourceType,
                            Amount = pair2.amount
                        });
                    }
                }

                var timePointsBuffer = AddBuffer<ResourcePointData>(entity);
                
                foreach (var pair in db.items)
                {
                    timePointsBuffer.Add(new ResourcePointData
                    {
                        Points = pair.timePoint
                    });
                }

                // Bake renewable resource info list
                var entity2 = CreateAdditionalEntity(TransformUsageFlags.None);
                var buffer2 = AddBuffer<RenewableResourceType>(entity2);
                var set = new HashSet<ResourceType>();
                
                foreach (var item in DatabaseManager.ResourceDatabaseSo.Items)
                {
                    if (!item.renewable) continue;
                    if (!set.Add(item.type))
                    {
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