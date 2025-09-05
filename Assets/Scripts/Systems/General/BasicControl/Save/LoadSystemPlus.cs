using System.IO;
using SparFlame.Components.General;
using SparFlame.Components.MainGameplay;
using SparFlame.Components.SubGameplay;
using SparFlame.Core.GlobalMono;
using SparFlame.Core.Utils;
using Unity.Collections;
using Unity.Entities;
using Unity.Entities.Serialization;
using Unity.Physics;
using UnityEngine;

// ReSharper disable ConvertToUsingDeclaration

namespace SparFlame.Systems.General.BasicControl
{
    [UpdateInGroup(typeof(InitializationSystemGroup))]
    public partial class LoadSystemPlus : SystemBase
    {
        private EntityQuery _loadArmyGroupQuery;

        // Database
        private NativeHashMap<int, Entity> _globalIdxToPrefabs;
        private NativeHashMap<long, Entity> _tmpIdxToInstances;
        private NativeHashMap<int, ExpStaticConfig> _expDatabase;

        // Sub gameplay component lookup
        private ComponentLookup<SeInGarrison> _inGarrisonLookup;
        private ComponentLookup<SeTmpId> _tmpIdLookup;
        private ComponentLookup<PhysicsMass> _physicsMassLookup;
        private ComponentLookup<SeInverseMass> _inverseMassLookup;
        private ComponentLookup<ExpData> _expDataLookup;
        private ComponentLookup<MovableData> _movableDataLookup;
        private ComponentLookup<AttackAbility> _attackAbilityLookup;
        private ComponentLookup<HealAbility> _healAbilityLookup;
        private ComponentLookup<HarvestAbility> _harvestAbilityLookup;
        private ComponentLookup<CityTaskUniqueId> _cityTaskUniqueIdLookup;
        
        private ComponentLookup<ConstructingTimer> _constructingTimerLookup;

        private BufferLookup<SeGarrisonEntity> _garrisonEntitiesLookup;
        private BufferLookup<GarrisonTypeData> _garrisonTypeDataLookup;
        private BufferLookup<SeConjuringData> _seConjuringDataLookup;


        // Main gameplay component lookup
        private ComponentLookup<SeArmyGroupInGarrison> _armyGroupInGarrisonLookup;
        private BufferLookup<SeCityGarrisonEntity> _cityGarrisonEntitiesLookup;


        private bool _initialized;


        protected override void OnCreate()
        {
            RequireForUpdate<SaveLoadConfig>();
            RequireForUpdate<BuildingEntityPrefabData>();
            RequireForUpdate<UnitEntityPrefabData>();
            RequireForUpdate<PlayerSaveSlot>();
            _inGarrisonLookup = GetComponentLookup<SeInGarrison>(true);
            _inverseMassLookup = GetComponentLookup<SeInverseMass>(true);
            _tmpIdLookup = GetComponentLookup<SeTmpId>(true);
            _physicsMassLookup = GetComponentLookup<PhysicsMass>(true);
            _expDataLookup = GetComponentLookup<ExpData>(true);
            _movableDataLookup = GetComponentLookup<MovableData>(true);
            _attackAbilityLookup = GetComponentLookup<AttackAbility>(true);
            _healAbilityLookup = GetComponentLookup<HealAbility>(true);
            _harvestAbilityLookup = GetComponentLookup<HarvestAbility>(true);
            _constructingTimerLookup = GetComponentLookup<ConstructingTimer>(true);
            _cityTaskUniqueIdLookup = GetComponentLookup<CityTaskUniqueId>(true);
            
            _garrisonEntitiesLookup = GetBufferLookup<SeGarrisonEntity>(true);
            _garrisonTypeDataLookup = GetBufferLookup<GarrisonTypeData>(true);
            _seConjuringDataLookup = GetBufferLookup<SeConjuringData>(true);

            _armyGroupInGarrisonLookup = GetComponentLookup<SeArmyGroupInGarrison>(true);
            _cityGarrisonEntitiesLookup = GetBufferLookup<SeCityGarrisonEntity>(true);

            _loadArmyGroupQuery = SystemAPI.QueryBuilder().WithAll<InSubGameTag>().WithAll<ArmyGroupAttr>().Build();
        }

        protected override void OnDestroy()
        {
            if (_globalIdxToPrefabs.IsCreated)
                _globalIdxToPrefabs.Dispose();
            if (_tmpIdxToInstances.IsCreated)
                _tmpIdxToInstances.Dispose();
            if (_expDatabase.IsCreated)
                _expDatabase.Dispose();
        }

        protected override void OnStartRunning()
        {
            if (!_initialized)
            {
                Initialize();
                _initialized = true;
                SaveLoadController.Instance.OnEcsLoadCitySubData += LoadCitySubData;
                SaveLoadController.Instance.OnEcsLoadArmyGroupSubData += LoadArmyGroupSubData;
                SaveLoadController.Instance.OnEcsLoadGameMainData += LoadGameMainData;
                SaveLoadController.Instance.OnEcsLoadMainGameplayData += LoadMainGameplayData;
            }
        }

        protected override void OnUpdate()
        {
        }

        private void LoadArmyGroupSubData()
        {
            var playerSaveSlot = SystemAPI.GetSingleton<PlayerSaveSlot>().Value;
            var armyGroupAttrs = _loadArmyGroupQuery.ToComponentDataArray<ArmyGroupAttr>(Allocator.Temp);
            foreach (var armyGroupAttr in armyGroupAttrs)
            {
                var armyGroupPath = SaveUtilities.GetArmyGroupSubDataPath(armyGroupAttr.saveId, playerSaveSlot);
                if (!File.Exists(armyGroupPath)) continue;
                using (var deserializeWorld = new World("Deserialization World"))
                {
                    var transaction = deserializeWorld.EntityManager.BeginExclusiveEntityTransaction();
                    using (var reader =
                           new StreamBinaryReader(armyGroupPath))
                    {
                        SerializeUtility.DeserializeWorld(transaction, reader);
                    }

                    deserializeWorld.EntityManager.EndExclusiveEntityTransaction();
                    EntityManager.MoveEntitiesFrom(deserializeWorld.EntityManager);
                    EntityManager.DestroyEntity(SystemAPI.GetSingletonEntity<SaveSystemPlus.SaveTmpTag>());
                }
            }

            armyGroupAttrs.Dispose();

            _movableDataLookup.Update(this);
            _attackAbilityLookup.Update(this);
            _healAbilityLookup.Update(this);
            _harvestAbilityLookup.Update(this);

            _garrisonEntitiesLookup.Update(this);
            _garrisonTypeDataLookup.Update(this);
            var ecb = new EntityCommandBuffer(Allocator.TempJob);

            var loadJob = new LoadArmyGroupSubDataJob
            {
                ECB = ecb.AsParallelWriter(),
                ExpDatabase = _expDatabase,
                GlobalIdxToPrefabs = _globalIdxToPrefabs,
                AttackAbilityLookup = _attackAbilityLookup,
                HealAbilityLookup = _healAbilityLookup,
                HarvestAbilityLookup = _harvestAbilityLookup,
                MovableDataLookup = _movableDataLookup,
            }.ScheduleParallel(Dependency);
            loadJob.Complete();
            ecb.Playback(EntityManager);
            ecb.Dispose();
            _tmpIdxToInstances.Clear();
            foreach (var (tmpId, entity) in SystemAPI.Query<RefRO<SeTmpId>>().WithEntityAccess())
            {
                _tmpIdxToInstances.Add(tmpId.ValueRO.value, entity);
            }

            foreach (var (armyGroupAttr, entity) in SystemAPI.Query<RefRO<ArmyGroupAttr>>().WithAll<InSubGameTag>()
                         .WithEntityAccess())
            {
                _tmpIdxToInstances.Add(armyGroupAttr.ValueRO.saveId, entity);
            }

            var ecb2 = new EntityCommandBuffer(Allocator.TempJob);
            var unitReplaceTmpIdJob = new ArmyGroupSubDataReplaceUnitTmpIdJob
            {
                ECB = ecb2.AsParallelWriter(),
                TmpIdxToInstances = _tmpIdxToInstances,
            }.ScheduleParallel(Dependency);
            var armyGroupReplaceTmpJob = new ArmyGroupSubDataReplaceArmyGroupTmpIdJob
            {
                TmpIdxToInstances = _tmpIdxToInstances
            }.ScheduleParallel(Dependency);
            unitReplaceTmpIdJob.Complete();
            armyGroupReplaceTmpJob.Complete();
            ecb2.Playback(EntityManager);
            ecb2.Dispose();
            _tmpIdxToInstances.Clear();
        }

        private void LoadCitySubData(SubGameStatusData targetSubGameStatusData)
        {
            var city = targetSubGameStatusData.City;
            var cityAttr = SystemAPI.GetComponent<CityAttr>(city);
            var playerSaveSlot = SystemAPI.GetSingleton<PlayerSaveSlot>().Value;
            var citySavePath = SaveUtilities.GetCitySubDataPath(cityAttr.globalId, playerSaveSlot);

            // Load city data
            var fileExist = File.Exists(citySavePath);
            if (fileExist)
            {
                using (var deserializeWorld = new World("Deserialization World"))
                {
                    var transaction = deserializeWorld.EntityManager.BeginExclusiveEntityTransaction();
                    using (var reader =
                           new StreamBinaryReader(citySavePath))
                    {
                        SerializeUtility.DeserializeWorld(transaction, reader);
                    }

                    deserializeWorld.EntityManager.EndExclusiveEntityTransaction();
                    EntityManager.MoveEntitiesFrom(deserializeWorld.EntityManager);
                    EntityManager.DestroyEntity(SystemAPI.GetSingletonEntity<SaveSystemPlus.SaveTmpTag>());
                }
            }


            _expDataLookup.Update(this);
            _inverseMassLookup.Update(this);
            _physicsMassLookup.Update(this);
            _tmpIdLookup.Update(this);
            _inGarrisonLookup.Update(this);
            _movableDataLookup.Update(this);
            _attackAbilityLookup.Update(this);
            _healAbilityLookup.Update(this);
            _harvestAbilityLookup.Update(this);
            _constructingTimerLookup.Update(this);
            _cityTaskUniqueIdLookup.Update(this);

            _garrisonEntitiesLookup.Update(this);
            _garrisonTypeDataLookup.Update(this);
            _seConjuringDataLookup.Update(this);
            var ecb = new EntityCommandBuffer(Allocator.TempJob);


            var loadJob = new LoadSubGameplayJob
            {
                ECB = ecb.AsParallelWriter(),
                GarrisonEntitiesLookup = _garrisonEntitiesLookup,
                GarrisonTypeDataLookup = _garrisonTypeDataLookup,
                InGarrisonLookup = _inGarrisonLookup,
                TmpIdLookup = _tmpIdLookup,
                PhysicsMassLookup = _physicsMassLookup,
                GlobalIdxToPrefabs = _globalIdxToPrefabs,
                InverseMassLookup = _inverseMassLookup,
                ExpLookup = _expDataLookup,
                HealAbilityLookup = _healAbilityLookup,
                AttackAbilityLookup = _attackAbilityLookup,
                HarvestAbilityLookup = _harvestAbilityLookup,
                MovableDataLookup = _movableDataLookup,
                ExpDatabase = _expDatabase,
                ConstructingTimerLookup = _constructingTimerLookup,
                SeConjuringDataLookup = _seConjuringDataLookup,
                CityTaskUniqueIdLookup = _cityTaskUniqueIdLookup,
            }.ScheduleParallel(Dependency);
            loadJob.Complete();
            ecb.Playback(EntityManager);
            ecb.Dispose();
            foreach (var (tmpId, entity) in SystemAPI.Query<RefRO<SeTmpId>>().WithEntityAccess())
            {
                _tmpIdxToInstances.Add(tmpId.ValueRO.value, entity);
            }

            _garrisonEntitiesLookup.Update(this);
            _inGarrisonLookup.Update(this);
            var ecb2 = new EntityCommandBuffer(Allocator.TempJob);
            var replaceTmpRefJob = new SubGameplayReplaceTmpIdJob
            {
                ECB = ecb2.AsParallelWriter(),
                SeGarrisonEntitiesLookup = _garrisonEntitiesLookup,
                SeInGarrisonLookup = _inGarrisonLookup,
                TmpIdxToInstances = _tmpIdxToInstances,
            }.ScheduleParallel(Dependency);
            replaceTmpRefJob.Complete();
            ecb2.Playback(EntityManager);
            ecb2.Dispose();
            _tmpIdxToInstances.Clear();


            /*var ecb3 = new EntityCommandBuffer(Allocator.TempJob);
            _volumeObstacleSpawnRequestsLookup.Update(this);
            var buildingSpawnObstacleJob = new BuildingSpawnObstacleJob
            {
                ECB = ecb3.AsParallelWriter(),
                Prefabs = _globalIdxToPrefabs,
                RequestLookup = _volumeObstacleSpawnRequestsLookup,
            }.ScheduleParallel(Dependency);
            buildingSpawnObstacleJob.Complete();
            ecb3.Playback(EntityManager);
            ecb3.Dispose();*/
        }

        private void LoadMainGameplayData()
        {
            var playerSaveSlot = SystemAPI.GetSingleton<PlayerSaveSlot>().Value;
            var cityMainDataPath = SaveUtilities.GetCityMainDataPath(playerSaveSlot);
            var armyGroupMainDataPath = SaveUtilities.GetArmyGroupMainDataPath(playerSaveSlot);


            // Load city data
            if (File.Exists(cityMainDataPath))
            {
                using (var deserializeWorld = new World("Deserialization World"))
                {
                    var transaction = deserializeWorld.EntityManager.BeginExclusiveEntityTransaction();
                    using (var reader =
                           new StreamBinaryReader(cityMainDataPath))
                    {
                        SerializeUtility.DeserializeWorld(transaction, reader);
                    }

                    deserializeWorld.EntityManager.EndExclusiveEntityTransaction();
                    EntityManager.MoveEntitiesFrom(deserializeWorld.EntityManager);
                    EntityManager.DestroyEntity(SystemAPI.GetSingletonEntity<SaveSystemPlus.SaveTmpTag>());
                }
            }
            else
            {
                Debug.LogError($"Losing saving : slot {playerSaveSlot} path {cityMainDataPath}");
            }

            // Load army group data
            if (File.Exists(armyGroupMainDataPath))
            {
                using (var deserializeWorld = new World("Deserialization World"))
                {
                    var transaction = deserializeWorld.EntityManager.BeginExclusiveEntityTransaction();
                    using (var reader =
                           new StreamBinaryReader(armyGroupMainDataPath))
                    {
                        SerializeUtility.DeserializeWorld(transaction, reader);
                    }

                    deserializeWorld.EntityManager.EndExclusiveEntityTransaction();
                    EntityManager.MoveEntitiesFrom(deserializeWorld.EntityManager);
                    EntityManager.DestroyEntity(SystemAPI.GetSingletonEntity<SaveSystemPlus.SaveTmpTag>());
                }
            }
            else
            {
                Debug.LogError($"Losing saving : slot {playerSaveSlot} path {armyGroupMainDataPath}");
            }

            var ecb = new EntityCommandBuffer(Allocator.TempJob);

            _armyGroupInGarrisonLookup.Update(this);
            _cityGarrisonEntitiesLookup.Update(this);
            _tmpIdLookup.Update(this);
            var loadCityJob = new LoadCityMainDataJob
            {
                ECB = ecb.AsParallelWriter(),
                TmpIdLookup = _tmpIdLookup,
                GlobalIdxToPrefabs = _globalIdxToPrefabs,
                CityGarrisonEntitiesLookup = _cityGarrisonEntitiesLookup
            }.ScheduleParallel(Dependency);
            loadCityJob.Complete();
            var loadArmyGroupJob = new LoadArmyGroupMainDataJob
            {
                ECB = ecb.AsParallelWriter(),
                InGarrisonLookup = _armyGroupInGarrisonLookup,
                ArmyGroupManageConfig = SystemAPI.GetSingleton<ArmyGroupManageConfig>()
            }.ScheduleParallel(Dependency);
            loadArmyGroupJob.Complete();
            ecb.Playback(EntityManager);
            ecb.Dispose();

            foreach (var (tmpId, entity) in SystemAPI.Query<RefRO<SeTmpId>>().WithEntityAccess())
            {
                _tmpIdxToInstances.Add(tmpId.ValueRO.value, entity);
            }

            _armyGroupInGarrisonLookup.Update(this);
            _cityGarrisonEntitiesLookup.Update(this);
            var ecb2 = new EntityCommandBuffer(Allocator.TempJob);
            var job2 = new MainGameplayReplaceTmpIdJob
            {
                ECB = ecb2.AsParallelWriter(),
                SeGarrisonEntitiesLookup = _cityGarrisonEntitiesLookup,
                SeInGarrisonLookup = _armyGroupInGarrisonLookup,
                TmpIdxToInstances = _tmpIdxToInstances,
            }.ScheduleParallel(Dependency);
            job2.Complete();
            ecb2.Playback(EntityManager);
            ecb2.Dispose();
            _tmpIdxToInstances.Clear();
        }

        private void LoadGameMainData()
        {
            var playerSaveSlot = SystemAPI.GetSingleton<PlayerSaveSlot>().Value;
            var gameMainDataPath = SaveUtilities.GetGameMainDataPath(playerSaveSlot);
            if (File.Exists(gameMainDataPath))
            {
                using (var deserializeWorld = new World("Deserialization World"))
                {
                    var transaction = deserializeWorld.EntityManager.BeginExclusiveEntityTransaction();
                    using (var reader =
                           new StreamBinaryReader(gameMainDataPath))
                    {
                        SerializeUtility.DeserializeWorld(transaction, reader);
                    }

                    deserializeWorld.EntityManager.EndExclusiveEntityTransaction();
                    var dem = deserializeWorld.EntityManager;

                    // Set Player faction data
                    var factionData = dem.CreateEntityQuery(typeof(PlayerFactionData))
                        .GetSingleton<PlayerFactionData>();
                    SystemAPI.SetSingleton(factionData);

                    // Set resource data
                    var savedResourceDatas = dem.CreateEntityQuery(typeof(ResourceData))
                        .GetSingletonBuffer<ResourceData>();
                    var currentResourceDatas =
                        SystemAPI.GetSingletonBuffer<ResourceData>();
                    for (var i = 0; i < savedResourceDatas.Length; i++)
                    {
                        var savedResourceData = savedResourceDatas[i];
                        currentResourceDatas[i] = savedResourceData;
                    }

                    var populationResourceData = dem.CreateEntityQuery(typeof(PopulationResourceData))
                        .GetSingleton<PopulationResourceData>();
                    SystemAPI.SetSingleton(populationResourceData);
                    var populationStorageAddTasks = dem.CreateEntityQuery(typeof(PopulationStorageAddTask))
                        .GetSingletonBuffer<PopulationStorageAddTask>();
                    var curPopulationStorageAddTasks = SystemAPI.GetSingletonBuffer<PopulationStorageAddTask>();
                    foreach (var task in populationStorageAddTasks)
                    {
                        curPopulationStorageAddTasks.Add(task);
                    }
                    var populationConjureTasks = dem.CreateEntityQuery(typeof(PopulationConjureTask))
                        .GetSingletonBuffer<PopulationConjureTask>();
                    var curPopulationConjureTasks = SystemAPI.GetSingletonBuffer<PopulationConjureTask>();
                    foreach (var task in populationConjureTasks)
                    {
                        curPopulationConjureTasks.Add(task);
                    }


                    // Set world time data
                    var worldTimeData = dem.CreateEntityQuery(typeof(WorldTimeData))
                        .GetSingleton<WorldTimeData>();
                    SystemAPI.SetSingleton(worldTimeData);
                    
                    // Set Unique Id
                    var uniqueIdData = dem.CreateEntityQuery(typeof(LastUniqueId))
                        .GetSingleton<LastUniqueId>();
                    SystemAPI.SetSingleton(uniqueIdData);
                    
                    // Set sub game status data
                    var saveCityId = dem.CreateEntityQuery(typeof(SaveCityId))
                        .GetSingleton<SaveCityId>();
                    if (saveCityId.value != 0) // Player save in city sub gameplay
                    {
                        FrameDelayInvoker.Instance.InvokeAfterFrames(1,
                            () =>
                            {
                                GameController.Instance.EnterPlayerCity(Entity.Null, true);
                            });
                    }
                    SystemAPI.SetSingleton(new SaveCityId
                    {
                        mainGameplayTransition = saveCityId.value != 0, // If player save exist in city last time, menu out controller should not hide loading screen
                        value = saveCityId.value
                    });
                }
            }
            else
            {
                Debug.LogError($"Losing saving : slot {playerSaveSlot} path {gameMainDataPath}");
            }
        }

        private void Initialize()
        {
            var buildingDatabase = SystemAPI.GetSingletonBuffer<BuildingEntityPrefabData>();
            var unitDatabase = SystemAPI.GetSingletonBuffer<UnitEntityPrefabData>();
            var expDatabase = SystemAPI.GetSingletonBuffer<ExpStaticConfig>();
            var cityDatabase = SystemAPI.GetSingletonBuffer<CityEntityPrefabData>();
            _globalIdxToPrefabs =
                new NativeHashMap<int, Entity>(buildingDatabase.Length + unitDatabase.Length, Allocator.Persistent);
            foreach (var prefabData in buildingDatabase)
            {
                _globalIdxToPrefabs.Add(prefabData.GlobalIdx, prefabData.Prefab);
            }

            foreach (var prefabData in unitDatabase)
            {
                _globalIdxToPrefabs.Add(prefabData.GlobalIdx, prefabData.Prefab);
            }

            foreach (var prefabData in cityDatabase)
            {
                _globalIdxToPrefabs.Add(prefabData.GlobalIdx, prefabData.Prefab);
            }

            _expDatabase = new NativeHashMap<int, ExpStaticConfig>(expDatabase.Length, Allocator.Persistent);
            foreach (var expData in expDatabase)
            {
                _expDatabase.Add(expData.GlobalIdx, expData);
            }

            _tmpIdxToInstances = new NativeHashMap<long, Entity>(100, allocator: Allocator.Persistent);
        }


        /*[BurstCompile]
        [WithNone(typeof(VolumeObstacleSpawnRequest))]
        [WithAll(typeof(BuildingAttr))]
        [WithNone(typeof(ConstructingData))]
        public partial struct BuildingSpawnObstacleJob : IJobEntity
        {
            public EntityCommandBuffer.ParallelWriter ECB;
            [ReadOnly] public NativeHashMap<int, Entity> Prefabs;
            [ReadOnly] public ComponentLookup<VolumeObstacleSpawnRequest> RequestLookup;

            private void Execute([ChunkIndexInQuery] int index, in SubGameplayGeneralAttr generalAttr,
                Entity selfEntity)
            {
                var prefab = Prefabs[generalAttr.ID];
                var request = RequestLookup[prefab];
                ECB.AddComponent(index, selfEntity, request);
                ECB.SetComponentEnabled<VolumeObstacleSpawnRequest>(index, selfEntity, true);
            }
        }*/
    }
}