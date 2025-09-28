using SparFlame.Components.General;
using SparFlame.Components.MainGameplay;
using SparFlame.Components.SubGameplay;
using SparFlame.Core.Utils;
using SparFlame.Systems.General.Resource;
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
        private ComponentLookup<GeneratingTag> _generatingTagLookup;

        
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<SubGameStatusData>();
            state.RequireForUpdate<EndSimulationEntityCommandBufferSystem.Singleton>();
            state.RequireForUpdate<PopulationResourceData>();
            state.RequireForUpdate<WorldTimeData>();
            state.RequireForUpdate<GameStatusData>();
            state.RequireForUpdate<ResourceData>();
            _generatingTagLookup = state.GetComponentLookup<GeneratingTag>(true);
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
            CheckPopulationResourceTask(ref state);
            CalPlayerGeneralResourceData(ref state);

            var generalResourceDatas = SystemAPI.GetSingletonBuffer<ResourceData>();
            var globalResourceAvailableDatas = new NativeHashMap<int, int>(3, Allocator.TempJob);
            globalResourceAvailableDatas.Add((int)ResourceType.SoulPact,
                generalResourceDatas[(int)ResourceType.SoulPact].availableAmount);
            globalResourceAvailableDatas.Add((int)ResourceType.Essence,
                generalResourceDatas[(int)ResourceType.Essence].availableAmount);
            globalResourceAvailableDatas.Add((int)ResourceType.Aetherium,
                generalResourceDatas[(int)ResourceType.Aetherium].availableAmount);

           
            _generatingTagLookup.Update(ref state);
            var ecbSingleton = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>();
            var ecb = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged).AsParallelWriter();
            new ResourceMineGenerateJob
            {
                ECB = ecb,
                GeneratingTagLookup = _generatingTagLookup,
                City = SystemAPI.GetSingleton<SubGameStatusData>().City
            }.ScheduleParallel();
            
            var job = new CityResourceCheckJob
            {
                CurrentTotalHours = worldTimeData.totalHours,
                ResourceTypeToGlobalAvailableAmount = globalResourceAvailableDatas,
            }.ScheduleParallel(state.Dependency);
            job.Complete();
            globalResourceAvailableDatas.Dispose();
            
        }


        private void CalPlayerGeneralResourceData(ref SystemState state)
        {
            var resourceDatas = SystemAPI.GetSingletonBuffer<ResourceData>();
            // Reset 

            var manaResourceData = new ResourceData
            {
                resourceType = ResourceType.Mana,
                availableAmount = 0,
                amountPerHour = 0,
                storage = 0
            };
            var crystalResourceData = new ResourceData
            {
                resourceType = ResourceType.Crystal,
                availableAmount = 0,
                amountPerHour = 0,
                storage = 0
            };

            // Calculate
            foreach (var cityResourceEntries in
                     SystemAPI.Query<DynamicBuffer<CityResourceEntry>>().WithAll<PlayerTag>())
            {
                var manaResourceEntry = cityResourceEntries[(int)ResourceType.Mana];
                manaResourceData.storage += manaResourceEntry.resourceData.storage;
                manaResourceData.availableAmount += manaResourceEntry.resourceData.availableAmount;
                manaResourceData.amountPerHour += manaResourceEntry.resourceData.amountPerHour;

                var crystalResourceEntry = cityResourceEntries[(int)ResourceType.Crystal];
                crystalResourceData.storage += crystalResourceEntry.resourceData.storage;
                crystalResourceData.availableAmount += crystalResourceEntry.resourceData.availableAmount;
                crystalResourceData.amountPerHour += crystalResourceEntry.resourceData.amountPerHour;
            }

            resourceDatas[(int)ResourceType.Mana] = manaResourceData;
            resourceDatas[(int)ResourceType.Crystal] = crystalResourceData;

            var populationResourceData = SystemAPI.GetSingleton<PopulationResourceData>();
            resourceDatas[(int)populationResourceData.populationResourceType] = new ResourceData
            {
                resourceType = populationResourceData.populationResourceType,
                availableAmount = math.max(0,
                    populationResourceData.storage - populationResourceData.occupiedCount -
                    populationResourceData.virtualOccupiedCount),
                amountPerHour = 0,
                storage = populationResourceData.storage,
            };
        }


        private void DealResourceChangeRequest(ref SystemState state)
        {
            var ecb = new EntityCommandBuffer(Allocator.Temp);
            var curTotalHours = SystemAPI.GetSingleton<WorldTimeData>().totalHours;
            ref var populationResourceData = ref SystemAPI.GetSingletonRW<PopulationResourceData>().ValueRW;
            var generalResourceDatas = SystemAPI.GetSingletonBuffer<ResourceData>();
            var populationStorageTasks = SystemAPI.GetSingletonBuffer<PopulationStorageAddTask>();


            foreach (var (requestRO, entity) in SystemAPI.Query<RefRO<ResourceChangeRequest>>().WithEntityAccess())
            {
                ecb.DestroyEntity(entity);

                var request = requestRO.ValueRO;
                var resourceKey = (int)request.ResourceType;

                var cityResourceEntries = SystemAPI.GetBuffer<CityResourceEntry>(request.City);
                var cityResourceEntry = cityResourceEntries[resourceKey];

                var populationConjureTasks = SystemAPI.GetSingletonBuffer<PopulationConjureTask>();
                var cityTasks = SystemAPI.GetBuffer<CityTask>(request.City);

                switch (request.RequestType)
                {
                    // Consume minus available amount
                    case ResourceRequestType.Consume:

                        switch (request.ResourceType)
                        {
                            case ResourceType.SoulPact:
                                populationResourceData.virtualOccupiedCount += request.AbsAmount;

                                break;

                            case ResourceType.Mana:
                            case ResourceType.Crystal:
                                var consumeAmount = math.min(cityResourceEntry.resourceData.availableAmount,
                                    request.AbsAmount);
                                cityResourceEntry.resourceData.availableAmount -= consumeAmount;
                                cityResourceEntry.resourceData.availableAmount =
                                    math.max(0, cityResourceEntry.resourceData.availableAmount);
                                cityResourceEntries[resourceKey] = cityResourceEntry;
                                break;
                            case ResourceType.Aetherium:
                            case ResourceType.Essence:
                                var generalResourceData = generalResourceDatas[resourceKey];
                                generalResourceData.availableAmount -= request.AbsAmount;
                                generalResourceData.availableAmount = math.max(0, generalResourceData.availableAmount);
                                generalResourceDatas[resourceKey] = generalResourceData;
                                break;
                            default:
                                BurstSafe.UnexpectedEnum(request.ResourceType);
                                break;
                        }


                        break;
                    // Generate and population release will add available amount
                    case ResourceRequestType.Generate:
                        switch (request.ResourceType)
                        {
                            case ResourceType.SoulPact: 
                                // This happens when system give player some special units
                                populationResourceData.occupiedCount += request.AbsAmount;
                                break;
                            case ResourceType.Mana:
                            case ResourceType.Crystal:
                                var maxAddAmount = cityResourceEntry.resourceData.storage -
                                                   cityResourceEntry.resourceData.availableAmount;
                                maxAddAmount = math.max(0, maxAddAmount);
                                cityResourceEntry.resourceData.availableAmount += math.min(maxAddAmount, request.AbsAmount);
                                cityResourceEntries[resourceKey] = cityResourceEntry;
                                break;
                            case ResourceType.Essence:
                            case ResourceType.Aetherium:
                                var generalResourceData = generalResourceDatas[resourceKey];
                                generalResourceData.availableAmount += request.AbsAmount;
                                generalResourceDatas[resourceKey] = generalResourceData;
                                break;
                            default:
                                BurstSafe.UnexpectedEnum(request.ResourceType);
                                break;
                        }
                        break;
                    case ResourceRequestType.PopulationRelease:
                        populationResourceData.occupiedCount -= request.AbsAmount;
                        populationResourceData.occupiedCount = math.max(0, populationResourceData.occupiedCount);
                        break;
                    // Storage add happens when player construct/upgrade a new dwelling or storage building.
                    case ResourceRequestType.StorageAddByTask:
                        if (request.ResourceType == populationResourceData.populationResourceType)
                        {
                            populationStorageTasks.Add(new PopulationStorageAddTask
                            {
                                finishTotalHours = request.FinishTotalHours,
                                fromBuildingSingleId = request.FromBuildingSingleId,
                                addAmount = request.AbsAmount,
                            });
                        }
                        else
                        {
                            ecb.AppendToBuffer(request.City, new CityTask
                            {
                                resourceType = request.ResourceType,
                                finishTotalHours = request.FinishTotalHours,
                                storageAddAmount = request.AbsAmount,
                                fromBuildingSingleId = request.FromBuildingSingleId,
                            });
                        }

                        break;
                    case ResourceRequestType.GenerateSpeedAddByTask:

                        ecb.AppendToBuffer(request.City, new CityTask
                        {
                            resourceType = request.ResourceType,
                            finishTotalHours = request.FinishTotalHours,
                            hoursPerUnit = request.HoursPerUnit,
                            fromBuildingSingleId = request.FromBuildingSingleId,
                            taskType = CityTaskType.PlantGenerator,
                        });
                        break;

                    case ResourceRequestType.ConstructingBuildingDestroyedAndRemoveTask:

                        if (request.ResourceType == populationResourceData.populationResourceType)
                        {
                            for (var i = populationStorageTasks.Length - 1; i >= 0; i--)
                            {
                                var task = populationStorageTasks[i];
                                if (task.fromBuildingSingleId != request.FromBuildingSingleId) continue;
                                populationStorageTasks.RemoveAt(i);
                                break;
                            }
                        }
                        else
                        {
                            for (var i = cityTasks.Length - 1; i >= 0; i--)
                            {
                                var task = cityTasks[i];
                                if (task.fromBuildingSingleId != request.FromBuildingSingleId) continue;
                                cityTasks.RemoveAt(i);
                                break;
                            }
                        }

                        break;
                    case ResourceRequestType.DecreaseStorage:
                        if (populationResourceData.populationResourceType == request.ResourceType)
                        {
                            populationResourceData.storage -= request.AbsAmount;
                        }
                        else
                        {
                            cityResourceEntry.resourceData.storage -= request.AbsAmount;
                            cityResourceEntry.resourceData.storage =
                                math.max(0, cityResourceEntry.resourceData.storage);
                            cityResourceEntries[resourceKey] = cityResourceEntry;
                        }

                        break;

                    case ResourceRequestType.DecreaseGenerateSpeed:
                        if(request.HoursPerUnit == 0)
                            break;
                        cityResourceEntry.resourceData.amountPerHour -= 1 / request.HoursPerUnit;
                        cityResourceEntries[resourceKey] = cityResourceEntry;
                        break;

                    case ResourceRequestType.IncreaseGenerateSpeed:
                        if (request.HoursPerUnit == 0)
                            break;
                        cityResourceEntry.resourceData.amountPerHour += 1 / request.HoursPerUnit;
                        cityResourceEntries[resourceKey] = cityResourceEntry;
                        break;

                    case ResourceRequestType.ConjureUnitByTask:
                        var find = false;

                        // Find whether task already exist
                        for (var i = 0; i < populationConjureTasks.Length; i++)
                        {
                            var task = populationConjureTasks[i];
                            if (task.fromBuildingSingleId == request.FromBuildingSingleId
                                && math.abs(task.hoursPerUnit - request.HoursPerUnit) < 0.01f)
                            {
                                task.remainingConjuredUnitCount += request.AbsAmount;
                                populationConjureTasks[i] = task;
                                find = true;
                                break;
                            }
                        }

                        if (!find)
                        {
                            populationConjureTasks.Add(new PopulationConjureTask
                            {
                                hoursPerUnit = request.HoursPerUnit,
                                fromBuildingSingleId = request.FromBuildingSingleId,
                                remainingConjuredUnitCount = request.AbsAmount,
                                accumulatedHours = curTotalHours,
                            });
                        }

                        break;
                    case ResourceRequestType.ConjureBuildingDestroyed:
                        for (var i = populationConjureTasks.Length - 1; i >= 0; i--)
                        {
                            var task = populationConjureTasks[i];
                            if (task.fromBuildingSingleId == request.FromBuildingSingleId)
                            {
                                populationConjureTasks.RemoveAt(i);
                                populationResourceData.virtualOccupiedCount -= task.remainingConjuredUnitCount;
                            }
                        }

                        break;
                    default:
                        BurstSafe.UnexpectedEnum(request.RequestType);
                        break;
                }
            }

            ecb.Playback(state.EntityManager);
            ecb.Dispose();
        }

        private void CheckPopulationResourceTask(ref SystemState state)
        {
            ref var populationResourceData = ref SystemAPI.GetSingletonRW<PopulationResourceData>().ValueRW;
            var storageAddTasks = SystemAPI.GetSingletonBuffer<PopulationStorageAddTask>();
            var conjureTasks = SystemAPI.GetSingletonBuffer<PopulationConjureTask>();
            var curTotalHours = SystemAPI.GetSingleton<WorldTimeData>().totalHours;
            for (var i = storageAddTasks.Length - 1; i >= 0; i--)
            {
                var task = storageAddTasks[i];
                if (task.finishTotalHours <= curTotalHours)
                {
                    populationResourceData.storage += task.addAmount;
                    storageAddTasks.RemoveAt(i);
                }
            }

            for (var i = conjureTasks.Length - 1; i >= 0; i--)
            {
                var task = conjureTasks[i];
                var deltaHours = curTotalHours - task.accumulatedHours;
                if (deltaHours >= task.hoursPerUnit)
                {
                    var validAmount = math.min(task.remainingConjuredUnitCount, task.hoursPerUnit == 0 ? 0 : (int)(deltaHours / task.hoursPerUnit));
                    populationResourceData.occupiedCount += validAmount;
                    populationResourceData.virtualOccupiedCount -= validAmount;
                    task.accumulatedHours += validAmount * task.hoursPerUnit;
                    task.remainingConjuredUnitCount -= validAmount;

                    if (task.remainingConjuredUnitCount <= 0)
                        conjureTasks.RemoveAt(i);
                    else
                    {
                        conjureTasks[i] = task;
                    }
                }
            }
        }
    }


  
}