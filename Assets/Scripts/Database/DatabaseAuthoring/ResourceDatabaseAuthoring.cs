using System;
using SparFlame.Components.General;
using Unity.Entities;
using UnityEngine;

namespace SparFlame.Database
{
    public class ResourceDatabaseAuthoring : MonoBehaviour
    {
        
        private class ResourceDatabaseAuthoringBaker : Baker<ResourceDatabaseAuthoring>
        {
            public override void Bake(ResourceDatabaseAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.None);
                var buffer3 = AddBuffer<ResourceEntityPrefabData>(entity);
                if (!DatabaseManager.ResourceDatabaseSo || DatabaseManager.ResourceDatabaseSo.Items.Count == 0)
                {
                    throw new ArgumentException("No resource database found");
                }
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
}