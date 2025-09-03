using System;
using SparFlame.Components.General;
using Unity.Entities;
using UnityEngine;

namespace SparFlame.Systems.MainGameplay.City
{
    public class ResourceSystemAuthoring : MonoBehaviour
    {
        private class CityResourceSystemAuthoringBaker : Baker<ResourceSystemAuthoring>
        {
            public override void Bake(ResourceSystemAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.None);
                var buffer = AddBuffer<ResourceData>(entity);

                foreach (ResourceType resourceType in Enum.GetValues(typeof(ResourceType)))
                {
                    buffer.Add(new ResourceData
                    {
                        resourceType = resourceType,
                        storage = 0,
                        availableAmount = 0,
                        hoursPerUnit = -1
                    });
                }
                
            }
        }
    }
}