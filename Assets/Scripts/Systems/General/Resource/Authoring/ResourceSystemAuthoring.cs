using System;
using SparFlame.Components.General;
using SparFlame.Core.Utils;
using Unity.Entities;
using UnityEngine;

namespace SparFlame.Systems.MainGameplay.City
{
    public class ResourceSystemAuthoring : MonoBehaviour
    {
        public int initPopulationStorage = 3;
        public ResourceType populationResourceType = ResourceType.SoulPact;
        public int initEssenceCount = 100;
        public int initAetheriumCount = 10;

        private class CityResourceSystemAuthoringBaker : Baker<ResourceSystemAuthoring>
        {
            public override void Bake(ResourceSystemAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.None);
                var buffer = AddBuffer<ResourceData>(entity);

                foreach (ResourceType resourceType in Enum.GetValues(typeof(ResourceType)))
                {
                    switch (resourceType)
                    {
                        case ResourceType.SoulPact:
                        case ResourceType.Mana:
                        case ResourceType.Crystal:
                            buffer.Add(new ResourceData
                            {
                                resourceType = resourceType,
                                storage = 0,
                                availableAmount = 0,
                                amountPerHour = 0,
                            });
                            break;
                        case ResourceType.Essence:
                            buffer.Add(new ResourceData
                            {
                                resourceType = resourceType,
                                storage = int.MaxValue,
                                availableAmount = authoring.initEssenceCount,
                                amountPerHour = 0,
                            });
                            break;
                        case ResourceType.Aetherium:
                            buffer.Add(new ResourceData
                            {
                                resourceType = resourceType,
                                storage = int.MaxValue,
                                availableAmount = authoring.initAetheriumCount,
                                amountPerHour = 0,
                            });
                            break;
                        default:
                            BurstSafe.UnexpectedEnum(resourceType);
                            break;
                    }
                }

                var pop1 = CreateAdditionalEntity(TransformUsageFlags.None);
                AddComponent(pop1, new PopulationResourceData
                {
                    storage = authoring.initPopulationStorage,
                    occupiedCount = 0,
                    virtualOccupiedCount = 0,
                    populationResourceType = authoring.populationResourceType,
                });

                var pop2 = CreateAdditionalEntity(TransformUsageFlags.None);
                AddBuffer<PopulationStorageAddTask>(pop2);
                var pop3 = CreateAdditionalEntity(TransformUsageFlags.None);
                AddBuffer<PopulationConjureTask>(pop3);
            }
        }
    }
}