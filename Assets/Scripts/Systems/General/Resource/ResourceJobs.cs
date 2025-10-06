using SparFlame.Components.General;
using SparFlame.Components.MainGameplay;
using SparFlame.Components.SubGameplay;
using SparFlame.Core.Utils;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace SparFlame.Systems.General.Resource
{
    [BurstCompile]
    public partial struct CityResourceCheckJob : IJobEntity
    {
        [ReadOnly] public float CurrentTotalHours;
        [ReadOnly] public NativeHashMap<int, int> ResourceTypeToGlobalAvailableAmount;
        [ReadOnly] public ResourceDebug ResourceDebug;
        private void Execute(ref DynamicBuffer<CityTask> tasks,
            ref DynamicBuffer<CityResourceEntry> cityResourceEntries
        )
        {
            // Check tasks
            for (var i = tasks.Length - 1; i >= 0; i--)
            {
                var task = tasks[i];
                if (task.finishTotalHours <= CurrentTotalHours)
                {
                    var resourceKey = (int)task.resourceType;
                    var cityResourceEntry = cityResourceEntries[resourceKey];
                    switch (task.taskType)
                    {
                        case CityTaskType.StorageAdd:
                            cityResourceEntry.resourceData.storage += task.storageAddAmount;
                            tasks.RemoveAt(i);
                            break;
                        case CityTaskType.PlantGenerator:
                            cityResourceEntry.resourceData.amountPerHour += 1f / task.hoursPerUnit;
                            tasks.RemoveAt(i);
                            break;
                        default:
                            BurstSafe.UnexpectedEnum(task.taskType);
                            break;
                    }

                    cityResourceEntries[resourceKey] = cityResourceEntry;
                }
            }

            // Generate resource
            for (var i = 0; i < cityResourceEntries.Length; i++)
            {
                var cityResourceEntry = cityResourceEntries[i];
                // Assign global resource data
                if (ResourceTypeToGlobalAvailableAmount.TryGetValue(i, out var availableAmount))
                {
                    cityResourceEntry.resourceData.availableAmount = availableAmount;
                    cityResourceEntries[i] = cityResourceEntry;
                    continue;
                }

                if (cityResourceEntry.accumulatedHours < 0.001f)
                {
                    cityResourceEntry.accumulatedHours = CurrentTotalHours;
                    cityResourceEntries[i] = cityResourceEntry;
                    continue;
                }

                if (math.abs(cityResourceEntry.resourceData.amountPerHour) < 0.001f)
                {
                    cityResourceEntry.accumulatedHours = CurrentTotalHours;
                    cityResourceEntries[i] = cityResourceEntry;
                    continue; // This resource is not generating.
                }
                var deltaTime = CurrentTotalHours - cityResourceEntry.accumulatedHours;
                var hoursPerUnit = 1f / cityResourceEntry.resourceData.amountPerHour;
                if(ResourceDebug is { enabled: true, spawnHoursScale: > 0f })
                    hoursPerUnit *= ResourceDebug.spawnHoursScale;
                if (deltaTime >= hoursPerUnit)
                {
                    var amount = (int)(deltaTime / hoursPerUnit);
                    var maxAddAmount = cityResourceEntry.resourceData.storage -
                                       cityResourceEntry.resourceData.availableAmount;
                    maxAddAmount = math.max(0, maxAddAmount);
                    cityResourceEntry.resourceData.availableAmount += math.min(maxAddAmount, amount);

                    cityResourceEntry.accumulatedHours +=
                        deltaTime - deltaTime % hoursPerUnit;
                }

                cityResourceEntries[i] = cityResourceEntry;
            }
        }
    }

    [BurstCompile]
    [WithNone(typeof(ConstructingTimer))]
    [WithAll(typeof(PlayerTag))]
    public partial struct ResourceMineGenerateJob : IJobEntity
    {
        public EntityCommandBuffer.ParallelWriter ECB;

        [ReadOnly] public ComponentLookup<GeneratingTag> GeneratingTagLookup;
        [ReadOnly] public Entity City;

        private void Execute([ChunkIndexInQuery] int index, ref GenerateAttr resourceMineGenerateAttr,
            in DynamicBuffer<GarrisonEntity> entities, in BuildingAttr buildingAttr,
            in SubGameplayGeneralAttr subGameplayGeneralAttr,
            in LocalTransform transform,
            Entity entity)
        {
            if (buildingAttr.SubTypeIndex != (int)GeneratorType.ResourceMine) return;
            // Not enough workers, decrease city generate speed if it has been already added.
            if (entities.Length < resourceMineGenerateAttr.MinCultivatorsRequireToGenerate)
            {
                resourceMineGenerateAttr.GenerateSpeedHoursPerUnit = 0f;
                if (GeneratingTagLookup.HasComponent(entity))
                {
                    ECB.RemoveComponent<GeneratingTag>(index, entity);
                    var decreaseGenerateSpeedRequest = ECB.CreateEntity(index);
                    ECB.AddComponent<SubGameplayEntityTag>(index, decreaseGenerateSpeedRequest);
                    ECB.AddComponent(index, decreaseGenerateSpeedRequest, new ResourceChangeRequest
                    {
                        ResourceType = resourceMineGenerateAttr.GenerateResourceType,
                        RequestType = ResourceRequestType.DecreaseGenerateSpeed,
                        City = City,
                        HoursPerUnit = resourceMineGenerateAttr.GenerateSpeedHoursPerUnit
                    });
                }

                return;
            }

            // Increase city generate speed if it has not been added yet.
            if (!GeneratingTagLookup.HasComponent(entity))
            {
                ECB.AddComponent<GeneratingTag>(index, entity);
                var decreaseGenerateSpeedRequest = ECB.CreateEntity(index);
                ECB.AddComponent<SubGameplayEntityTag>(index, decreaseGenerateSpeedRequest);
                ECB.AddComponent(index, decreaseGenerateSpeedRequest, new ResourceChangeRequest
                {
                    ResourceType = resourceMineGenerateAttr.GenerateResourceType,
                    RequestType = ResourceRequestType.IncreaseGenerateSpeed,
                    City = City,
                    HoursPerUnit = resourceMineGenerateAttr.GenerateSpeedHoursPerUnit
                });
            }
        }
    }
}