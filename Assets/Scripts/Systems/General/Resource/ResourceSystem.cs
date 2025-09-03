using SparFlame.Components.General;
using SparFlame.Components.MainGameplay;
using SparFlame.Core.Utils;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

// ReSharper disable Unity.Entities.MustBeSurroundedWithRefRwRo

namespace SparFlame.Systems.MainGameplay.City
{
    /// <summary>
    /// This system manages the city storage changes and city resource generation.
    /// </summary>
    [BurstCompile]
    public partial struct ResourceSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<PopulationResourceType>();
            state.RequireForUpdate<WorldTimeData>();
            state.RequireForUpdate<GameStatusData>();
            state.RequireForUpdate<ResourceData>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var gameStatus = SystemAPI.GetSingleton<GameStatusData>();
            var worldTimeData = SystemAPI.GetSingleton<WorldTimeData>();
            if (gameStatus.Value == GameStatus.Init)
            {
                CalPlayerGeneralResourceData(ref state);
                return;
            }

            if (gameStatus.Value != GameStatus.MainGaming && gameStatus.Value != GameStatus.SubGaming) return;

            DealResourceChangeRequest(ref state);
            CalPlayerGeneralResourceData(ref state);

            new CityResourceCheckJob
            {
                CurrentTotalHours = worldTimeData.totalHours,
            }.ScheduleParallel();
        }


        private void CalPlayerGeneralResourceData(ref SystemState state)
        {
            var resourceDatas = SystemAPI.GetSingletonBuffer<ResourceData>();
            // Reset 
            for (var i = 0; i < resourceDatas.Length; i++)
            {
                var resource = resourceDatas[i];
                resource.availableAmount = 0;
                resource.storage = 0;
                resource.hoursPerUnit = -1f;
                resource.virtualOccupiedCount = 0;
                resource.occupiedCount = 0;
                resourceDatas[i] = resource;
            }

            // Calculate
            foreach (var cityResourceEntries in
                     SystemAPI.Query<DynamicBuffer<CityResourceEntry>>().WithAll<PlayerTag>())
            {
                foreach (var cityResourceEntry in cityResourceEntries)
                {
                    var resourceKey = (int)cityResourceEntry.resourceData.resourceType;
                    var resourceData = resourceDatas[resourceKey];
                    resourceData.storage += cityResourceEntry.resourceData.storage;
                    resourceData.availableAmount += cityResourceEntry.resourceData.availableAmount;
                    resourceData.virtualOccupiedCount += cityResourceEntry.resourceData.virtualOccupiedCount;
                    resourceData.occupiedCount += cityResourceEntry.resourceData.occupiedCount;
                    var curSpeed = resourceData.hoursPerUnit < 0 ? 0 : 1f / resourceData.hoursPerUnit;
                    var addSpeed = cityResourceEntry.resourceData.hoursPerUnit < 0
                        ? 0
                        : 1f / cityResourceEntry.resourceData.hoursPerUnit;
                    curSpeed += addSpeed;
                    if (math.abs(curSpeed) < 0.001f) resourceData.hoursPerUnit = -1f;
                    else resourceData.hoursPerUnit = 1f / curSpeed;
                    resourceDatas[resourceKey] = resourceData;
                }
            }
        }


        private void DealResourceChangeRequest(ref SystemState state)
        {
            var ecb = new EntityCommandBuffer(Allocator.Temp);
            var curTotalHours = SystemAPI.GetSingleton<WorldTimeData>().totalHours;
            var populationResourceType = SystemAPI.GetSingleton<PopulationResourceType>().Value;
            foreach (var (requestRO, entity) in SystemAPI.Query<RefRO<ResourceChangeRequest>>().WithEntityAccess())
            {
                var request = requestRO.ValueRO;
                var resourceKey = (int)request.ResourceType;
                var absAmount = request.AbsAmount;

                var cityResourceEntries = SystemAPI.GetBuffer<CityResourceEntry>(request.City);
                var cityResourceEntry = cityResourceEntries[resourceKey];

                var cityTasks = SystemAPI.GetBuffer<CityTask>(request.City);

                switch (request.RequestType)
                {
                    // Consume minus available amount
                    case ResourceRequestType.Consume:
                        var consumeAmount = math.min(cityResourceEntry.resourceData.availableAmount, absAmount);
                        cityResourceEntry.resourceData.availableAmount -= consumeAmount;
                        cityResourceEntry.resourceData.availableAmount = math.max(0, cityResourceEntry.resourceData.availableAmount);
                        if (populationResourceType == request.ResourceType)
                        {
                            cityResourceEntry.resourceData.virtualOccupiedCount += consumeAmount;
                        }
                        cityResourceEntries[resourceKey] = cityResourceEntry;

                        break;
                    // Generate and population release will add available amount
                    case ResourceRequestType.Generate:
                        var maxAddAmount = cityResourceEntry.resourceData.storage -
                                           cityResourceEntry.resourceData.availableAmount;
                        maxAddAmount = math.max(0, maxAddAmount);
                        cityResourceEntry.resourceData.availableAmount += math.min(maxAddAmount, absAmount);
                        cityResourceEntries[resourceKey] = cityResourceEntry;
                        break;
                    case ResourceRequestType.PopulationRelease:
                        cityResourceEntry.resourceData.occupiedCount -= absAmount;
                        cityResourceEntry.resourceData.occupiedCount = math.max(0, cityResourceEntry.resourceData.occupiedCount);
                        cityResourceEntry.resourceData.availableAmount = math.max(0, 
                            cityResourceEntry.resourceData.storage
                            - cityResourceEntry.resourceData.occupiedCount
                            - cityResourceEntry.resourceData.virtualOccupiedCount);
                        cityResourceEntries[resourceKey] = cityResourceEntry;
                        break;
                    // Storage add happens when player construct/upgrade a new dwelling or storage building.
                    case ResourceRequestType.StorageAddByTask:
                        ecb.AppendToBuffer(request.City, new CityTask
                        {
                            resourceType = request.ResourceType,
                            finishTotalHours = request.FinishTotalHours,
                            storageAddAmount = request.AbsAmount,
                            fromBuildingUniqueId = request.FromBuildingUniqueId,
                        });
                        break;
                    case ResourceRequestType.GenerateSpeedAddByTask:
                        ecb.AppendToBuffer(request.City, new CityTask
                        {
                            resourceType = request.ResourceType,
                            finishTotalHours = request.FinishTotalHours,
                            hoursPerUnit = request.HoursPerUnit,
                            fromBuildingUniqueId = request.FromBuildingUniqueId,
                            taskType = CityTaskType.PlantGenerator,
                        });
                        break;
                    case ResourceRequestType.ResourceBuildingDestroyedWhenConstructing:
                        for (var i = cityTasks.Length - 1; i >= 0; i--)
                        {
                            var task = cityTasks[i];
                            if (task.fromBuildingUniqueId != request.FromBuildingUniqueId) continue;
                            cityTasks.RemoveAt(i);
                            break;
                        }

                        break;
                    case ResourceRequestType.ResourceBuildingDestroyedAfterConstruction:
                        cityResourceEntry.resourceData.storage -= absAmount;
                        cityResourceEntry.resourceData.storage =
                            math.max(0, cityResourceEntry.resourceData.storage);
                        if (populationResourceType == request.ResourceType)
                        {
                            cityResourceEntry.resourceData.availableAmount = math.max(0,
                                cityResourceEntry.resourceData.storage 
                                - cityResourceEntry.resourceData.occupiedCount
                                - cityResourceEntry.resourceData.virtualOccupiedCount);
                        }

                        cityResourceEntries[resourceKey] = cityResourceEntry;
                        break;

                    case ResourceRequestType.DecreaseGenerateSpeedForResourceMine:
                        var currentSpeed = cityResourceEntry.resourceData.hoursPerUnit > 0
                            ? 1f / cityResourceEntry.resourceData.hoursPerUnit
                            : 0f;
                        var newSpeed = currentSpeed - 1f / request.HoursPerUnit;
                        cityResourceEntry.resourceData.hoursPerUnit = math.abs(newSpeed) > 0.001f ? 1f / newSpeed : -1f;
                        cityResourceEntries[resourceKey] = cityResourceEntry;
                        break;

                    case ResourceRequestType.IncreaseGenerateSpeedForResourceMine:
                        var curSpeed = cityResourceEntry.resourceData.hoursPerUnit > 0
                            ? 1 / cityResourceEntry.resourceData.hoursPerUnit
                            : 0;
                        var nSpeed = curSpeed + 1f / request.HoursPerUnit;
                        cityResourceEntry.resourceData.hoursPerUnit = 1f / nSpeed;
                        cityResourceEntries[resourceKey] = cityResourceEntry;
                        break;
                    case ResourceRequestType.ConjureUnitByTask:

                        var find = false;

                        // Find whether task already exist
                        for (var i = 0; i < cityTasks.Length; i++)
                        {
                            var task = cityTasks[i];
                            if (task.fromBuildingUniqueId == request.FromBuildingUniqueId
                                && math.abs(task.hoursPerUnit - request.HoursPerUnit) < 0.01f)
                            {
                                task.remainingConjuredUnitCount += request.AbsAmount;
                                cityTasks[i] = task;
                                find = true;
                                break;
                            }
                        }

                        if (!find)
                        {
                            ecb.AppendToBuffer(request.City, new CityTask
                            {
                                resourceType = request.ResourceType,
                                hoursPerUnit = request.HoursPerUnit,
                                fromBuildingUniqueId = request.FromBuildingUniqueId,
                                taskType = CityTaskType.ConjureUnits,
                                remainingConjuredUnitCount = request.AbsAmount,
                                finishTotalHours = curTotalHours + request.HoursPerUnit,
                            });
                        }

                        break;
                    case ResourceRequestType.ConjureBuildingDestroyed:
                        for (var i = cityTasks.Length - 1; i >= 0; i--)
                        {
                            var task = cityTasks[i];
                            if (task.fromBuildingUniqueId == request.FromBuildingUniqueId)
                            {
                                cityTasks.RemoveAt(i);
                                cityResourceEntry.resourceData.virtualOccupiedCount -= task.remainingConjuredUnitCount;
                                cityResourceEntry.resourceData.availableAmount += task.remainingConjuredUnitCount;
                                cityResourceEntries[resourceKey] = cityResourceEntry;
                            }
                        }

                        break;
                    default:
                        BurstSafe.UnexpectedEnum(request.RequestType);
                        break;
                }

                ecb.DestroyEntity(entity);
            }

            ecb.Playback(state.EntityManager);
            ecb.Dispose();
        }
    }


    [BurstCompile]
    [WithAll(typeof(PlayerTag))]
    public partial struct CityResourceCheckJob : IJobEntity
    {
        [ReadOnly] public float CurrentTotalHours;

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

                            // Population available amount will increase when storage increases.
                            if (task.resourceType == ResourceType.SoulPact)
                            {
                                cityResourceEntry.resourceData.availableAmount = math.max(0,
                                    cityResourceEntry.resourceData.storage -
                                    cityResourceEntry.resourceData.occupiedCount -
                                    cityResourceEntry.resourceData.virtualOccupiedCount);
                            }
                            tasks.RemoveAt(i);
                            break;
                        case CityTaskType.PlantGenerator:
                            var curSpeed = cityResourceEntry.resourceData.hoursPerUnit > 0
                                ? 1 / cityResourceEntry.resourceData.hoursPerUnit
                                : 0;
                            var newSpeed = curSpeed + 1f / task.hoursPerUnit;
                            cityResourceEntry.resourceData.hoursPerUnit = 1f / newSpeed;
                            tasks.RemoveAt(i);
                            break;
                        case CityTaskType.ConjureUnits:

                            var deltaHours = CurrentTotalHours - (task.finishTotalHours - task.hoursPerUnit);

                            var count = (int)(deltaHours / task.hoursPerUnit);
                            var validCount = math.min(count, task.remainingConjuredUnitCount);
                            task.finishTotalHours += validCount * task.hoursPerUnit;
                            task.remainingConjuredUnitCount -= validCount;
                            cityResourceEntry.resourceData.virtualOccupiedCount -= validCount;
                            cityResourceEntry.resourceData.occupiedCount += validCount;
                            if (task.remainingConjuredUnitCount <= 0)
                            {
                                tasks.RemoveAt(i);
                            }
                            else
                            {
                                tasks[i] = task;
                            }
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
                if (cityResourceEntry.resourceData.hoursPerUnit < 0) continue; // This resource is not generating.

                var deltaTime = CurrentTotalHours - cityResourceEntry.accumulatedHours;
                if (deltaTime >= cityResourceEntry.resourceData.hoursPerUnit)
                {
                    var amount = (int)(deltaTime / cityResourceEntry.resourceData.hoursPerUnit);
                    var maxAddAmount = cityResourceEntry.resourceData.storage -
                                       cityResourceEntry.resourceData.availableAmount;
                    maxAddAmount = math.max(0, maxAddAmount);
                    cityResourceEntry.resourceData.availableAmount += math.min(maxAddAmount, amount);

                    cityResourceEntry.accumulatedHours +=
                        deltaTime - deltaTime % cityResourceEntry.resourceData.hoursPerUnit;
                }

                cityResourceEntries[i] = cityResourceEntry;
            }
        }
    }
}