using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using SparFlame.Components.General;
using SparFlame.Components.MainGameplay;
using SparFlame.Components.SubGameplay;
using SparFlame.Core.Utils;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using UnityEngine;

namespace SparFlame.Systems.General.BasicControl
{
    public partial class LoadSystemAsync : SystemBase
    {
        private Dictionary<SaveArcheType, SaveArcheTypeInfo> _archeTypeInfos;

        private bool _initialized;

        // Database
        private NativeHashMap<int, Entity> _globalIdxToPrefabs;
        private NativeHashMap<int, ExpStaticConfig> _expDatabase;
        private ComponentLookup<EnemyArmyGroupBelongsToCity> _belongsToCityLookup;
        private ComponentLookup<InGarrison> _inGarrisonLookup;
        private BufferLookup<ConjuringData> _conjuringDataLookup;
        private ComponentLookup<ArmyGroupAttr> _armyGroupAttrLookup;
        private BufferLookup<GarrisonEntity> _garrisonEntitiesLookup;
        private ComponentLookup<ArmyGroupInGarrison> _armyGroupInGarrisonLookup;


        private delegate string GetPath(long singleId, int playerSaveSlot, bool isTmp);


        protected override void OnCreate()
        {
            RequireForUpdate<SaveLoadConfig>();
            RequireForUpdate<ExpStaticConfig>();
            RequireForUpdate<BuildingEntityPrefabData>();
            _armyGroupAttrLookup = GetComponentLookup<ArmyGroupAttr>(true);
            _inGarrisonLookup = GetComponentLookup<InGarrison>(true);
            _belongsToCityLookup = GetComponentLookup<EnemyArmyGroupBelongsToCity>(true);
            _armyGroupInGarrisonLookup = GetComponentLookup<ArmyGroupInGarrison>(true);

            
            _conjuringDataLookup = GetBufferLookup<ConjuringData>();
            _garrisonEntitiesLookup = GetBufferLookup<GarrisonEntity>();
        }

        protected override void OnStartRunning()
        {
            if (!_initialized)
            {
                Initialize();
                SaveLoadController.Instance.OnEcsStartLoadCitySubData += data =>
                {
                    _ = LoadAbstractAsync(SaveLoadTaskType.CitySubData, data.City).ContinueWith(t =>
                        Debug.LogException(t.Exception));
                };
                SaveLoadController.Instance.OnEcsStartLoadArmyGroupSubData += () =>
                {
                    _ = LoadAbstractAsync(SaveLoadTaskType.ArmyGroupSubData, Entity.Null).ContinueWith(t =>
                        Debug.LogException(t.Exception));
                };
                SaveLoadController.Instance.OnEcsStartLoadGameMainData += () =>
                {
                    _ = LoadAbstractAsync(SaveLoadTaskType.GameMainData, Entity.Null)
                        .ContinueWith(t => Debug.LogException(t.Exception));
                };

                _initialized = true;
            }
        }

        protected override void OnUpdate()
        {
            
        }

        private async Task LoadAbstractAsync(SaveLoadTaskType taskType, Entity city)
        {
            // var postProcessor = _savePostProcessorFactory.GetPostProcessor(taskType);
            // object args = null;
            using var ecb = new EntityCommandBuffer(Allocator.Persistent);
            switch (taskType)
            {
                case SaveLoadTaskType.GameMainData:
                    var loadMainGameDataTask = LoadGameMainDataAsync(SaveArcheType.GameMain, ecb);
                    var loadCityTask = LoadGameMainDataAsync(SaveArcheType.City, ecb);
                    var loadArmyGroupTask = LoadGameMainDataAsync(SaveArcheType.ArmyGroup, ecb);
                    await Task.WhenAll(loadMainGameDataTask, loadCityTask, loadArmyGroupTask);
                    break;
                case SaveLoadTaskType.CitySubData:
                    var loadCityUnitTask = LoadCitySubDataAsync(SaveArcheType.CityUnit, city, ecb);
                    var loadCityBuildingTask = LoadCitySubDataAsync(SaveArcheType.CityBuilding, city, ecb);
                    await Task.WhenAll(loadCityUnitTask, loadCityBuildingTask);
                    break;
                case SaveLoadTaskType.ArmyGroupSubData:
                    await LoadArmyGroupSubDataAsync(ecb);
                    break;
                default:
                    BurstSafe.UnexpectedEnum(taskType);
                    break;
            }

            ecb.Playback(EntityManager);
            // Reassign entity by single id
            _conjuringDataLookup.Update(this);
            _inGarrisonLookup.Update(this);
            _armyGroupAttrLookup.Update(this);
            _belongsToCityLookup.Update(this);
            _garrisonEntitiesLookup.Update(this);
            _armyGroupInGarrisonLookup.Update(this);
            // if (taskType == SaveLoadTaskType.CitySubData)
            //     PostProcessForCitySubData();
            switch (taskType)
            {
                case SaveLoadTaskType.GameMainData:
                    // args = new GameMainDataPostProcessorArgs(EntityManager, Dependency,
                    //     _belongsToCityLookup);
                    PostProcessForGameMainData();
                    break;
                case SaveLoadTaskType.ArmyGroupSubData:
                    // args = new ArmyGroupSubDataPostProcessorArgs
                    //     (EntityManager, Dependency, _armyGroupAttrLookup);
                    PostProcessForArmyGroupSubData();
                    break;
                case SaveLoadTaskType.CitySubData:
                    // args = new CitySubDataPostProcessorArgs(EntityManager, Dependency,
                    //     _conjuringDataLookup, _inGarrisonLookup);
                    PostProcessForCitySubData();
                    break;
                default:
                    BurstSafe.UnexpectedEnum(taskType);
                    break;
            }

            // await postProcessor.Run(args);
            SaveLoadController.Instance.OneTaskLoadComplete();
        }

        private async Task LoadCitySubDataAsync(SaveArcheType saveArcheType, Entity city, EntityCommandBuffer ecb)
        {
            var playerSavSlot = SystemAPI.GetSingleton<CurrentSaveSlot>();
            var singleId = SystemAPI.GetComponent<GlobalSingleId>(city).value;
            GetPath method = saveArcheType == SaveArcheType.CityUnit
                ? SaveUtilities.GetCityUnitSubDataPath
                : SaveUtilities.GetCityBuildingSubDataPath;
            var savePath = method(singleId, playerSavSlot.Value, true);
            if (!File.Exists(savePath))
                savePath = method(singleId, playerSavSlot.Value, false);
            if (!File.Exists(savePath))
            {
                return;
            }

            await SinglePathLoadData(savePath, ecb);
        }


        private async Task LoadGameMainDataAsync(SaveArcheType saveArcheType, EntityCommandBuffer ecb)
        {
            var playerSaveSlot = SystemAPI.GetSingleton<CurrentSaveSlot>().Value;
            var savePath = saveArcheType switch
            {
                SaveArcheType.GameMain => SaveUtilities.GetGameMainDataPath(playerSaveSlot),
                SaveArcheType.City => SaveUtilities.GetCityMainDataPath(playerSaveSlot),
                SaveArcheType.ArmyGroup => SaveUtilities.GetArmyGroupMainDataPath(playerSaveSlot),
                _ => BurstSafe.UnexpectedEnum(saveArcheType, "")
            };
            await SinglePathLoadData(savePath, ecb);
        }


        private async Task LoadArmyGroupSubDataAsync(EntityCommandBuffer ecb)
        {
            var playerSaveSlot = SystemAPI.GetSingleton<CurrentSaveSlot>().Value;

            var loadArmyGroupQuery = SystemAPI.QueryBuilder().WithAll<InSubGameTag>().WithAll<GlobalSingleId>()
                .WithAll<ArmyGroupAttr>().Build();
            using var armyGroupSingleIds = loadArmyGroupQuery.ToComponentDataArray<GlobalSingleId>(Allocator.Persistent);

            foreach (var armyGroupSingleId in armyGroupSingleIds)
            {
                // Get tmp file first, if not exist, try prior saving
                var armyGroupPath =
                    SaveUtilities.GetArmyGroupSubDataPath(armyGroupSingleId.value, playerSaveSlot, true);
                if (!File.Exists(armyGroupPath))
                    armyGroupPath =
                        SaveUtilities.GetArmyGroupSubDataPath(armyGroupSingleId.value, playerSaveSlot, false);
                // If still not exist, this should never happen
                if (!File.Exists(armyGroupPath))
                {
                    Debug.LogError($"Losing saving :army group {armyGroupPath}");
                    continue;
                }

                await SinglePathLoadData(armyGroupPath, ecb);
            }
        }

        /// <summary>
        /// Load data and record commands into ecb
        /// </summary>
        /// <param name="savePath"></param>
        /// <param name="ecb"></param>
        /// <exception cref="EndOfStreamException"></exception>
        private async Task SinglePathLoadData(string savePath, EntityCommandBuffer ecb)
        {
            await using var fs = new FileStream(
                savePath,
                FileMode.Open,
                FileAccess.Read,
                FileShare.Read,
                bufferSize: 65536,
                useAsync: true
            );
            // --- Read header : ArchetypeId + entity count  ---
            var header = await EcsReflectLoadMethod.LoadFileHeaderAsync(fs);
            var info = _archeTypeInfos[header.SaveArcheType];
            // Prefab entity load
            if (info.saveEntityType == SaveEntityType.Prefab)
            {
                using var prefabIds = await EcsReflectLoadMethod.LoadPrefabIdsAsync(fs, header.EntityCount);

                var typeToArray = new Dictionary<Type, Array>();
                await EcsReflectLoadMethod.PreLoadFixedComponentDataArraysAsync(fs, info, typeToArray,
                    header.EntityCount);

                for (var i = 0; i < header.EntityCount; i++)
                {
                    var prefab = _globalIdxToPrefabs[prefabIds[i].value];
                    var e = ecb.Instantiate(prefab);
                    EcsReflectLoadMethod.LoadFixedComponentDatas(info, ecb, e, i, typeToArray);
                    await EcsReflectLoadMethod.LoadFixedBuffers(fs, info, ecb, e);
                    await EcsReflectLoadMethod.LoadConditionalComponentDatas(fs, info, ecb, e, EntityManager, prefab);
                    await EcsReflectLoadMethod.LoadConditionalBuffers(fs, info, ecb, e, EntityManager, prefab);
                }
            }
            else // Singleton entity load. Actually set component and set
            {
                await EcsReflectLoadMethod.LoadFixedComponentSingletonsAsync(fs, info, EntityManager, ecb);
                await EcsReflectLoadMethod.LoadFixedBufferSingletonsAsync(fs, info, EntityManager, ecb);
            }
        }


        private void Initialize()
        {
            var buildingDatabase = SystemAPI.GetSingletonBuffer<BuildingEntityPrefabData>();
            var unitDatabase = SystemAPI.GetSingletonBuffer<UnitEntityPrefabData>();
            var expDatabase = SystemAPI.GetSingletonBuffer<ExpStaticConfig>();
            var cityDatabase = SystemAPI.GetSingletonBuffer<CityEntityPrefabData>();
            var armyGroupDatabase = SystemAPI.GetSingletonBuffer<ArmyGroupEntityPrefabData>();
            _globalIdxToPrefabs =
                new NativeHashMap<int, Entity>(buildingDatabase.Length + unitDatabase.Length, Allocator.Persistent);
            foreach (var prefabData in buildingDatabase)
            {
                _globalIdxToPrefabs.Add(prefabData.PrefabId, prefabData.Prefab);
            }

            foreach (var prefabData in unitDatabase)
            {
                _globalIdxToPrefabs.Add(prefabData.PrefabId, prefabData.Prefab);
            }

            foreach (var prefabData in cityDatabase)
            {
                _globalIdxToPrefabs.Add(prefabData.PrefabId, prefabData.Prefab);
            }

            foreach (var prefabData in armyGroupDatabase)
            {
                _globalIdxToPrefabs.Add(prefabData.PrefabId, prefabData.Prefab);
            }

            _expDatabase = new NativeHashMap<int, ExpStaticConfig>(expDatabase.Length, Allocator.Persistent);
            foreach (var expData in expDatabase)
            {
                _expDatabase.Add(expData.PrefabId, expData);
            }

            _archeTypeInfos = new Dictionary<SaveArcheType, SaveArcheTypeInfo>();
            var config = SystemAPI.ManagedAPI.GetSingleton<SaveLoadConfig>();
            foreach (var info in config.archetypes)
            {
                _archeTypeInfos[info.saveArcheType] = info;
            }
        }

        protected override void OnDestroy()
        {
            if (_globalIdxToPrefabs.IsCreated)
                _globalIdxToPrefabs.Dispose();

            if (_expDatabase.IsCreated)
                _expDatabase.Dispose();
        }

        private void PostProcessForGameMainData()
        {
            var map = new NativeHashMap<long, Entity>(100, Allocator.Persistent);
            using var query = EntityManager.CreateEntityQuery(typeof(GlobalSingleId),typeof(MainGameplayGeneralAttr));
            using var entities = query.ToEntityArray(Allocator.Persistent);
            using var ids = query.ToComponentDataArray<GlobalSingleId>(Allocator.Persistent);
            for (var i = 0; i < entities.Length; i++)
            {
                map.Add(ids[i].value, entities[i]);
            }

            var ecb = new EntityCommandBuffer(Allocator.Persistent);
            var ecbP = ecb.AsParallelWriter();
            var ecb2 = new EntityCommandBuffer(Allocator.Persistent);
            var ecbP2 = ecb2.AsParallelWriter();
            var job1 = new ArmyGroupPostProcessJob
            {
                Map = map,
                ECB = ecbP,
                BillboardConfig = SystemAPI.GetSingleton<ArmyGroupBillboardConfig>(),
                ArmyGroupBelongsToCityLookup = _belongsToCityLookup,
                ArmyGroupInGarrisonLookup = _armyGroupInGarrisonLookup,
            }.ScheduleParallel(Dependency);
            var job2 = new CityPostProcessJob
            {
                Map = map,
                ECB = ecbP2
            }.ScheduleParallel(Dependency);
            Dependency = JobHandle.CombineDependencies(job1, job2);
            Dependency.Complete();
            ecb.Playback(EntityManager);
            ecb2.Playback(EntityManager);
            ecb2.Dispose();
            ecb.Dispose();
            map.Dispose();
        }

        private void PostProcessForCitySubData()
        {
            var unitQueryDesc = new EntityQueryDesc
            {
                All = new ComponentType[] { typeof(GlobalSingleId), typeof(InGarrison) },
                None = new ComponentType[] { typeof(AssignGlobalSingleIDRequest) }
            };
            using var cityUnitsQuery = EntityManager.CreateEntityQuery(unitQueryDesc);
            var buildingQueryDesc = new EntityQueryDesc
            {
                All = new ComponentType[] { typeof(GlobalSingleId), typeof(GarrisonAttr) },
                None = new ComponentType[] { typeof(AssignGlobalSingleIDRequest) }
            };
            using var cityBuildingsQuery =
                EntityManager.CreateEntityQuery(buildingQueryDesc);
            using var units = cityUnitsQuery.ToEntityArray(Allocator.Persistent);
            using var buildings = cityBuildingsQuery.ToEntityArray(Allocator.Persistent);
            using var unitSingleIds = cityUnitsQuery.ToComponentDataArray<GlobalSingleId>(Allocator.Persistent);
            using var buildingSingleIds =
                cityBuildingsQuery.ToComponentDataArray<GlobalSingleId>(Allocator.Persistent);
            var singleIdToEntities = new NativeHashMap<long, Entity>(100, Allocator.Persistent);
            for (var i = 0; i < units.Length; i++)
            {
                singleIdToEntities.Add(unitSingleIds[i].value, units[i]);
            }
            

            for (var i = 0; i < buildings.Length; i++)
            {
                singleIdToEntities.Add(buildingSingleIds[i].value, buildings[i]);
            }

            var ecb = new EntityCommandBuffer(Allocator.Persistent);
            var ecbP = ecb.AsParallelWriter();
            var ecb2 = new EntityCommandBuffer(Allocator.Persistent);
            var ecbP2 = ecb2.AsParallelWriter();
            var job1 = new CityUnitPostProcessJob
            {
                InGarrisonLookup = _inGarrisonLookup,
                Map = singleIdToEntities,
                ECB = ecbP
            }.ScheduleParallel(Dependency);

            var job2 = new CityBuildingPostProcessJob
            {
                Map = singleIdToEntities,
                ECB = ecbP2,
                ConjuringDataLookup = _conjuringDataLookup,
                GarrisonEntitiesLookup = _garrisonEntitiesLookup,
                PrefabDatabase = _globalIdxToPrefabs
            }.ScheduleParallel(Dependency);
            Dependency = JobHandle.CombineDependencies(job1, job2);
            Dependency.Complete();
            ecb.Playback(EntityManager);
            ecb.Dispose();
            ecb2.Playback(EntityManager);
            ecb2.Dispose();
            singleIdToEntities.Dispose();
        }

        private void PostProcessForArmyGroupSubData()
        {
            var ecb = new EntityCommandBuffer(Allocator.Persistent);
            var tmpIdxToInstances = new NativeHashMap<long, Entity>(100, Allocator.Persistent);

            using var loadedUnitQuery = EntityManager.CreateEntityQuery(typeof(InArmyGroup), typeof(GlobalSingleId));
            using var loadedUnits = loadedUnitQuery.ToEntityArray(Allocator.Persistent);
            using var unitSingleIds = loadedUnitQuery.ToComponentDataArray<GlobalSingleId>(Allocator.Persistent);
            for (var i = 0; i < loadedUnits.Length; i++)
            {
                tmpIdxToInstances.Add(unitSingleIds[i].value, loadedUnits[i]);
            }

            using var inSubGameArmyGroupQuery = EntityManager.CreateEntityQuery(typeof(InSubGameTag),
                typeof(GlobalSingleId),
                typeof(ArmyGroupAttr));
            using var armyGroups = inSubGameArmyGroupQuery.ToEntityArray(Allocator.Persistent);
            using var armyGroupSingleIds =
                inSubGameArmyGroupQuery.ToComponentDataArray<GlobalSingleId>(Allocator.Persistent);

            for (var i = 0; i < armyGroups.Length; i++)
            {
                tmpIdxToInstances.Add(armyGroupSingleIds[i].value, armyGroups[i]);
            }

            var ecbP = ecb.AsParallelWriter();
            var job1 = new ArmyGroupUnitPostProcessJob
            {
                Map = tmpIdxToInstances,
                ArmyGroupAttrLookup = _armyGroupAttrLookup,
                ECB = ecbP
            }.ScheduleParallel(Dependency);

            var job2 = new ArmyGroupReplaceUnitIdJob
            {
                Map = tmpIdxToInstances
            }.ScheduleParallel(Dependency);
            Dependency = JobHandle.CombineDependencies(job1, job2);
            Dependency.Complete();
            ecb.Playback(EntityManager);
            ecb.Dispose();
            tmpIdxToInstances.Dispose();
        }
    }
}