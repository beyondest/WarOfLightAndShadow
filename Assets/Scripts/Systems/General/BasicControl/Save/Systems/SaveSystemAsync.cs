using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using SparFlame.Components.General;
using SparFlame.Components.MainGameplay;
using SparFlame.Core;
using SparFlame.Core.Utils;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

namespace SparFlame.Systems.General.BasicControl
{
    public partial class SaveSystemAsync : SystemBase
    {
        private SavePreProcessorFactory _savePreProcessorFactory;
        private Dictionary<SaveArcheType, SaveArcheTypeInfo> _archeTypeInfos;
        private bool _initialized;

        protected override void OnCreate()
        {
            RequireForUpdate<SaveLoadConfig>();
            _savePreProcessorFactory = new SavePreProcessorFactory();
        }


        protected override void OnStartRunning()
        {
            if (!_initialized)
            {
                _initialized = true;
                _archeTypeInfos = new Dictionary<SaveArcheType, SaveArcheTypeInfo>();
                var config = SystemAPI.ManagedAPI.GetSingleton<SaveLoadConfig>();
                foreach (var archetype in config.archetypes)
                {
                    archetype.RebuildCache();
                    _archeTypeInfos.Add(archetype.saveArcheType, archetype);
                }

                SaveLoadController.Instance.OnEcsStartSaveArmyGroupSubData +=
                    shouldSaveToTmp =>
                    {
                        _ = SaveAbstractAsync(SaveLoadTaskType.ArmyGroupSubData, shouldSaveToTmp).ContinueWith
                            (task => Debug.LogException(task.Exception));
                    };
                SaveLoadController.Instance.OnEcsStartSaveEnemySpecificArmyGroupSubData +=
                    () =>
                    {
                        _ = SaveAbstractAsync(SaveLoadTaskType.ArmyGroupSubData, true, true).ContinueWith
                            (task => Debug.LogException(task.Exception));
                    };
                SaveLoadController.Instance.OnEcsStartSavingCitySubData +=
                    shouldSaveToTmp =>
                    {
                        _ = SaveAbstractAsync(SaveLoadTaskType.CitySubData, shouldSaveToTmp)
                            .ContinueWith(task => Debug.LogException(task.Exception));
                    };
                SaveLoadController.Instance.OnEcsStartSaveGameMainData +=
                    () =>
                    {
                        _ = SaveAbstractAsync(SaveLoadTaskType.GameMainData, false)
                            .ContinueWith(task => Debug.LogException(task.Exception));
                    };
                SaveLoadController.Instance.OnEcsCopyDeleteTmpSubDatas += CopyDeleteTmpSubDatas;


                SaveLoadController.Instance.OnEcsDeleteInvalidSubDatas += DeleteInvalidSubDataFiles;
                SaveLoadController.Instance.OnEcsCopyOverrideSavingSlot += CopyOverrideSavingSlot;
            }
        }

        protected override void OnUpdate()
        {
        }

        private async Task SaveAbstractAsync(SaveLoadTaskType taskType, bool shouldSaveToTmp,
            bool isSpecificArmyGroup = false)
        {
            switch (taskType)
            {
                case SaveLoadTaskType.GameMainData:
                    SaveQuickData();
                    var saveArmyGroupTask = SaveGameMainDataAsync(SaveArcheType.ArmyGroup);
                    var saveCityTask = SaveGameMainDataAsync(SaveArcheType.City);
                    var saveGameMainTask = SaveGameMainDataAsync(SaveArcheType.GameMain);
                    await Task.WhenAll(saveArmyGroupTask, saveCityTask, saveGameMainTask);
                    // DisableNeedSaveTag(false, false);
                    break;
                case SaveLoadTaskType.ArmyGroupSubData:
                    var saveArmyGroupSubDataTask = SaveArmyGroupUnitAsync(shouldSaveToTmp, isSpecificArmyGroup);
                    await Task.WhenAll(saveArmyGroupSubDataTask);
                    // No need to disable need save tag because this tag will be disabled in method
                    break;
                case SaveLoadTaskType.CitySubData:
                    var saveCityUnitTask = SaveCityUnitDataAsync(shouldSaveToTmp);
                    var saveCityBuildingTask = SaveCityBuildingDataAsync(shouldSaveToTmp);
                    await Task.WhenAll(saveCityBuildingTask, saveCityUnitTask);
                    // DisableNeedSaveTag(false, false);
                    break;
                default:
                    BurstSafe.UnexpectedEnum(taskType);
                    break;
            }

            SaveLoadController.Instance.OneTaskSaveComplete();
        }

        private void SaveQuickData()
        {
            var playerSaveSlot = SystemAPI.GetSingleton<CurrentSaveSlot>().Value;
            var playerFactionData = SystemAPI.GetSingleton<PlayerFactionData>();
            var worldTime = SystemAPI.GetSingleton<WorldTimeData>();
            var city = SystemAPI.GetSingleton<SubGameStatusData>().City;
            var quickData = new RiftGameFileQuickData
            {
                Faction = playerFactionData.faction,
                SubFaction = playerFactionData.subFaction,
                TotalHours = (int)worldTime.totalHours,
                CityPrefabId = SystemAPI.HasComponent<PrefabId>(city)
                    ? SystemAPI.GetComponent<PrefabId>(city).value
                    : 0
            };
            SaveUtilities.WriteQuickData(playerSaveSlot, quickData);
        }

        private async Task SaveCityBuildingDataAsync(bool shouldSaveToTmp)
        {
            var subGameStatus = SystemAPI.GetSingleton<SubGameStatusData>();
            var citySingleId = SystemAPI.GetComponent<GlobalSingleId>(subGameStatus.City).value;
            var playerSaveSlot = SystemAPI.GetSingleton<CurrentSaveSlot>().Value;
            var saveBuildingPath = SaveUtilities.GetCityBuildingSubDataPath(citySingleId, playerSaveSlot,
                shouldSaveToTmp);

            const SaveArcheType buildingSaveArcheType = SaveArcheType.CityBuilding;


            // Add need saving tag to savable buildings
            // var buildingPreprocessor = _savePreProcessorFactory.GetPreProcessor(buildingSaveArcheType);
            // _needSaveTagLookup.Update(this);
            // var args = new CityBuildingSavePreProcessorArgs(_needSaveTagLookup, Dependency);
            // await buildingPreprocessor.Run(args);
            // CityBuildingSavePreProcess();
            await SingleQuerySinglePathSave(_archeTypeInfos[buildingSaveArcheType], saveBuildingPath);
        }

        private async Task SaveCityUnitDataAsync(bool shouldSaveToTmp)
        {
            var subGameStatus = SystemAPI.GetSingleton<SubGameStatusData>();
            var citySingleId = SystemAPI.GetComponent<GlobalSingleId>(subGameStatus.City).value;
            var playerSaveSlot = SystemAPI.GetSingleton<CurrentSaveSlot>().Value;
            var saveUnitPath =
                SaveUtilities.GetCityUnitSubDataPath(citySingleId, playerSaveSlot, shouldSaveToTmp);


            const SaveArcheType unitSaveArcheType = SaveArcheType.CityUnit;

            // Add need saving tag to savable city units

            // var unitPreprocessor = _savePreProcessorFactory.GetPreProcessor(unitSaveArcheType);
            // _needSaveTagLookup.Update(this);
            // var args1 = new CityUnitSavePreArgs(_needSaveTagLookup, Dependency);
            // await unitPreprocessor.Run(args1);
            // CityUnitSavePreProcess();

            await SingleQuerySinglePathSave(_archeTypeInfos[unitSaveArcheType], saveUnitPath);
        }

        private async Task SaveArmyGroupUnitAsync(bool shouldSaveToTmp, bool isSpecificArmyGroup)
        {
            var playerSaveSlot = SystemAPI.GetSingleton<CurrentSaveSlot>().Value;
            const SaveArcheType saveArcheType = SaveArcheType.ArmyGroupUnit;
            var query = isSpecificArmyGroup
                ? SystemAPI.QueryBuilder().WithAll<GlobalSingleId>().WithAll<EnemyArmyGroupSaveTag>()
                    .WithAll<ArmyGroupAttr>().Build()
                : SystemAPI.QueryBuilder().WithAll<GlobalSingleId>().WithAll<InSubGameTag>().WithAll<ArmyGroupAttr>()
                    .Build();


            using var armyGroups = query.ToEntityArray(Allocator.Persistent);
            using var armyGroupSingleIds = query.ToComponentDataArray<GlobalSingleId>(Allocator.Persistent);
            for (var i = 0; i < armyGroups.Length; i++)
            {
                var armyGroup = armyGroups[i];
                using var ecb = new EntityCommandBuffer(Allocator.Persistent);
                var preProcessor = _savePreProcessorFactory.GetPreProcessor(saveArcheType);
                var args = new ArmyGroupUnitPreProcessorArgs(armyGroup,
                    ecb, EntityManager);
                await preProcessor.Run(args);
                ecb.Playback(EntityManager);
                var savePath =
                    SaveUtilities.GetArmyGroupSubDataPath(armyGroupSingleIds[i].value, playerSaveSlot, shouldSaveToTmp);
                await SingleQuerySinglePathSave(_archeTypeInfos[saveArcheType], savePath);
                DisableNeedSaveTag(true, isSpecificArmyGroup);
            }
        }


        private async Task SaveGameMainDataAsync(SaveArcheType saveArcheType)
        {
            // Load config
            var playerSaveSlot = SystemAPI.GetSingleton<CurrentSaveSlot>().Value;

            var savePath = saveArcheType switch
            {
                SaveArcheType.GameMain => SaveUtilities.GetGameMainDataPath(playerSaveSlot),
                SaveArcheType.City => SaveUtilities.GetCityMainDataPath(playerSaveSlot),
                SaveArcheType.ArmyGroup => SaveUtilities.GetArmyGroupMainDataPath(playerSaveSlot),
                _ => BurstSafe.UnexpectedEnum(saveArcheType, "")
            };

            // object args = saveArcheType switch
            // {
            //     SaveArcheType.City => new CitySavePreProcessorArgs(_needSaveTagLookup, Dependency),
            //     SaveArcheType.ArmyGroup => new ArmyGroupSavePreProcessorArgs(_needSaveTagLookup, Dependency),
            //     SaveArcheType.GameMain => new GameMainDataPreProcessorArgs(),
            //     _ => BurstSafe.UnexpectedEnum(saveArcheType, "")
            // };
            // Preprocess all archetypes
            // var preProcessor = _savePreProcessorFactory.GetPreProcessor(saveArcheType);
            // await preProcessor.Run(args);
            switch (saveArcheType)
            {
                case SaveArcheType.GameMain:
                    break;
                case SaveArcheType.ArmyGroup:
                    // ArmyGroupSavePreProcess();
                    break;
                case SaveArcheType.City:
                    // CitySavePreProcess();
                    break;
                case SaveArcheType.CityUnit:
                case SaveArcheType.CityBuilding:
                case SaveArcheType.ArmyGroupUnit:
                default:
                    BurstSafe.UnexpectedEnum(saveArcheType);
                    break;
            }
            var archeTypeInfo = _archeTypeInfos[saveArcheType];
            await SingleQuerySinglePathSave(archeTypeInfo, savePath);
        }


        private async Task SingleQuerySinglePathSave(SaveArcheTypeInfo archeTypeInfo, string savePath)
        {
            var auxPath = savePath + ".aux";
            {
                await using var fs = new FileStream(auxPath, FileMode.Create, FileAccess.Write, FileShare.None,
                    65536, useAsync: true);
                if (archeTypeInfo.saveEntityType == SaveEntityType.Prefab)
                {
                    // When in prefab made, only save entity with need save tag
                    var queryDesc = new EntityQueryDesc
                    {
                        All = archeTypeInfo.QueryWithAllComponentTypes.ToArray(),
                        None = archeTypeInfo.QueryWithNoneComponentTypes.ToArray()
                    };
                    using var query = EntityManager.CreateEntityQuery(queryDesc);
                    await EcsReflectSaveMethod.WriteFileHeaderAsync(fs, archeTypeInfo, query);
                    await EcsReflectSaveMethod.WriteFixedComponentDataArrayBytesAsync(fs, archeTypeInfo, EntityManager,
                        query);
                    using var entities = query.ToEntityArray(Allocator.Persistent);
                    foreach (var entity in entities)
                    {
                        await EcsReflectSaveMethod.WriteFixedBufferOfSingleEntityAsync(fs, archeTypeInfo, EntityManager,
                            entity);
                        await EcsReflectSaveMethod.WriteConditionalComponentsOfSingleEntityAsync(fs, archeTypeInfo,
                            EntityManager, entity);
                        await EcsReflectSaveMethod.WriteConditionalBuffersOfSingleEntityAsync(fs, archeTypeInfo,
                            EntityManager, entity);
                    }
                }
                // Singleton entity save
                else
                {
                    // This query is no use, only to pass to function
                    using var noUseQuery = EntityManager.CreateEntityQuery(typeof(GameStatusData));
                    await EcsReflectSaveMethod.WriteFileHeaderAsync(fs, archeTypeInfo);
                    await EcsReflectSaveMethod.WriteFixedComponentDataArrayBytesAsync(fs, archeTypeInfo, EntityManager,
                        noUseQuery);
                    await EcsReflectSaveMethod.WriteFixedBufferSingletonsAsync(fs, archeTypeInfo, EntityManager);
                }

                await fs.FlushAsync();
            }

            // 3. 覆盖保存
            File.Copy(auxPath, savePath, true);
            File.Delete(auxPath);
        }

        /// <summary>
        /// This method is only used for army group sub data save
        /// </summary>
        /// <param name="shouldMakeSureAllJobComplete"></param>
        /// <param name="shouldDestroySavedEntities"></param>
        private void DisableNeedSaveTag(bool shouldMakeSureAllJobComplete, bool shouldDestroySavedEntities)
        {
            if (shouldMakeSureAllJobComplete)
            {
                using var ecb = new EntityCommandBuffer(Allocator.Persistent);
                var job = new DisableNeedSaveTagJob
                {
                    ShouldDestroySelf = shouldDestroySavedEntities,
                    ECB = ecb.AsParallelWriter(),
                }.ScheduleParallel(Dependency);
                job.Complete();
                ecb.Playback(EntityManager);
            }
            else
            {
                var ecb = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>()
                    .CreateCommandBuffer(World.Unmanaged);
                new DisableNeedSaveTagJob
                {
                    ShouldDestroySelf = shouldDestroySavedEntities,
                    ECB = ecb.AsParallelWriter(),
                }.ScheduleParallel();
            }
        }

        // Only check sub datas
        private void DeleteInvalidSubDataFiles()
        {
            var currentSlot = SystemAPI.GetSingleton<CurrentSaveSlot>();
            // Delete non-player city sub data if it exists, delete dead army group sub data if it exists
            var playerCityQuery = SystemAPI.QueryBuilder().WithAll<CityAttr>().WithAll<GlobalSingleId>()
                .WithAll<PlayerTag>()
                .Build();
            var playerArmyGroups = SystemAPI.QueryBuilder().WithAll<ArmyGroupAttr>().WithAll<GlobalSingleId>()
                .WithAll<PlayerTag>().Build();
            var citySingleIds = playerCityQuery.ToComponentDataArray<GlobalSingleId>(Allocator.Temp);
            var armyGroupAttrs = playerArmyGroups.ToComponentDataArray<GlobalSingleId>(Allocator.Temp);
            var cityValidSaveIds = new NativeHashSet<long>(12, Allocator.Temp);
            var armyGroupValidSaveIds = new NativeHashSet<long>(5, Allocator.Temp);

            foreach (var singleId in citySingleIds)
            {
                cityValidSaveIds.Add(singleId.value);
            }

            foreach (var globalSingleId in armyGroupAttrs)
            {
                armyGroupValidSaveIds.Add(globalSingleId.value);
            }

            var citySubDataFolder = SaveUtilities.GetCitySubDataFolder(currentSlot.Value);
            var armyGroupSubDataFolder = SaveUtilities.GetArmyGroupSubDataFolder(currentSlot.Value);
            var citySubDataFiles = Directory.GetFiles(citySubDataFolder);
            var armyGroupSubDataFiles = Directory.GetFiles(armyGroupSubDataFolder);

            foreach (var citySubDataFile in citySubDataFiles)
            {
                if (citySubDataFile.EndsWith("tmp"))
                {
                    File.Delete(citySubDataFile);
                    continue;
                }

                var fileName = citySubDataFile.Split(".")[0];
                if (long.TryParse(fileName, out var cityGlobalId))
                {
                    if (!cityValidSaveIds.Contains(cityGlobalId))
                    {
                        File.Delete(citySubDataFile);
                    }
                }
            }

            foreach (var armyGroupSubDataFile in armyGroupSubDataFiles)
            {
                if (armyGroupSubDataFile.EndsWith("tmp"))
                {
                    File.Delete(armyGroupSubDataFile);
                    continue;
                }

                var fileName = armyGroupSubDataFile.Split(".")[0];
                if (long.TryParse(fileName, out var armyGroupSaveId))
                {
                    if (!armyGroupValidSaveIds.Contains(armyGroupSaveId))
                    {
                        File.Delete(armyGroupSubDataFile);
                    }
                }
            }
        }

        private void CopyDeleteTmpSubDatas(int targetSaveSlot)
        {
            var currentSaveSlot = SystemAPI.GetSingleton<CurrentSaveSlot>();

            // Copy city sub data from tmp to true save path and delete tmp path
            foreach (var id in SystemAPI.Query<RefRO<GlobalSingleId>>().WithAll<CityAttr>())
            {
                var tmpPath = SaveUtilities.GetCitySubDataPath(id.ValueRO.value,
                    currentSaveSlot.Value, true);
                if (File.Exists(tmpPath))
                {
                    var truePath = SaveUtilities.GetCitySubDataPath(id.ValueRO.value,
                        targetSaveSlot, false);
                    File.Copy(tmpPath, truePath, overwrite: true);
                    File.Delete(tmpPath);
                }
            }
            // Copy army group sub data from tmp to true save path and delete tmp path

            foreach (var globalSingleId in SystemAPI.Query<RefRO<GlobalSingleId>>().WithAll<ArmyGroupAttr>())
            {
                var tmpPath = SaveUtilities.GetArmyGroupSubDataPath(globalSingleId.ValueRO.value, currentSaveSlot.Value,
                    true);
                if (File.Exists(tmpPath))
                {
                    var truePath = SaveUtilities.GetArmyGroupSubDataPath(globalSingleId.ValueRO.value,
                        targetSaveSlot, false);
                    File.Copy(tmpPath, truePath, overwrite: true);
                    File.Delete(tmpPath);
                }
            }
        }

        private void CopyOverrideSavingSlot(int targetSaveSlot)
        {
            var currentSaveSlot = SystemAPI.GetSingleton<CurrentSaveSlot>();
            var currentSlotFolder = SaveUtilities.GetSaveSlotFolder(currentSaveSlot.Value);
            var targetSlotFolder = SaveUtilities.GetSaveSlotFolder(targetSaveSlot);
            FileUtils.CopyDirectory(currentSlotFolder, targetSlotFolder);
        }

        private void CitySavePreProcess()
        {
            var ecb = new EntityCommandBuffer(Allocator.Persistent);
            Dependency = new MainGameplayCitySetNeedSaveTagJob
            {
                ECB = ecb.AsParallelWriter()
            }.ScheduleParallel(Dependency);
            Dependency.Complete();
            ecb.Playback(EntityManager);
            ecb.Dispose();
        }

        private void ArmyGroupSavePreProcess()
        {
            var ecb = new EntityCommandBuffer(Allocator.Persistent);
            Dependency = new MainGameplayArmyGroupSetNeedSaveTagJob
            {
                ECB = ecb.AsParallelWriter(),
            }.ScheduleParallel(Dependency);
            Dependency.Complete();
            ecb.Playback(EntityManager);
            ecb.Dispose();
        }

        private void CityBuildingSavePreProcess()
        {
            var ecb = new EntityCommandBuffer(Allocator.Persistent);

            Dependency = new CityBuildingSetNeedSaveTagJob
            {
                ECB = ecb.AsParallelWriter(),
            }.ScheduleParallel(Dependency);
            Dependency.Complete();
            ecb.Playback(EntityManager);
            ecb.Dispose();
        }

        private void CityUnitSavePreProcess()
        {
            var ecb = new EntityCommandBuffer(Allocator.Persistent);
            Dependency = new CityUnitSetNeedSaveTagJob
            {
                ECB = ecb.AsParallelWriter(),
            }.ScheduleParallel(Dependency);
            Dependency.Complete();
            ecb.Playback(EntityManager);
            ecb.Dispose();
        }
    }

    [BurstCompile]
    [WithAll(typeof(NeedSaveTag))]
    public partial struct DisableNeedSaveTagJob : IJobEntity
    {
        public EntityCommandBuffer.ParallelWriter ECB;
        [ReadOnly] public bool ShouldDestroySelf;

        private void Execute([ChunkIndexInQuery] int index, Entity selfEntity)
        {
            ECB.SetComponentEnabled<NeedSaveTag>(index, selfEntity,false);
            if (ShouldDestroySelf) ECB.DestroyEntity(index, selfEntity);
        }
    }
}