using System.IO;
using SparFlame.Components.General;
using SparFlame.Components.MainGameplay;
using SparFlame.Components.SubGameplay;
using SparFlame.Core.Utils;
using Unity.Collections;
using Unity.Entities;
using Unity.Entities.Serialization;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Transforms;

// ReSharper disable ConvertToUsingDeclaration

namespace SparFlame.Systems.General.BasicControl
{
    [UpdateInGroup(typeof(InitializationSystemGroup)), UpdateAfter(typeof(GameBasicControlSystem))]
    public partial class SaveSystemPlus : SystemBase
    {
        private EntityQuery _saveArmyGroupQuery;

        public struct SaveTmpTag : IComponentData
        {
            public bool NoUseButCannotDelete;
        }

        private BufferLookup<GarrisonEntity> _garrisonEntitiesLookup;
        private BufferLookup<GarrisonTypeData> _garrisonTypeDataLookup;
        private BufferLookup<ConjuringData> _conjuringDataLookup;

        private ComponentLookup<InGarrison> _inGarrisonLookup;
        private ComponentLookup<PhysicsMass> _physicsMassLookup;
        private ComponentLookup<ArmyGroupMovingTag> _movingTagLookup;
        private ComponentLookup<ArmyGroupCalculateEnable> _calculateEnableLookup;
        private ComponentLookup<ArmyGroupInGarrison> _armyGroupInGarrisonLookup;
        private ComponentLookup<ArmyGroupAttr> _armyGroupAttrLookup;
        private ComponentLookup<ConstructingTimer> _constructingTimerLookup;
        private ComponentLookup<CityTaskUniqueId> _cityTaskUniqueIdLookup;
        private ComponentLookup<CityAttr> _cityAttrLookup;

        private bool _initialized;

        protected override void OnCreate()
        {
            RequireForUpdate<SaveLoadConfig>();
            RequireForUpdate<PlayerSaveSlot>();
            RequireForUpdate<SubGameStatusData>();
            _garrisonEntitiesLookup = GetBufferLookup<GarrisonEntity>(true);
            _garrisonTypeDataLookup = GetBufferLookup<GarrisonTypeData>(true);
            _conjuringDataLookup = GetBufferLookup<ConjuringData>(true);

            _inGarrisonLookup = GetComponentLookup<InGarrison>(true);
            _physicsMassLookup = GetComponentLookup<PhysicsMass>(true);
            _movingTagLookup = GetComponentLookup<ArmyGroupMovingTag>(true);
            _calculateEnableLookup = GetComponentLookup<ArmyGroupCalculateEnable>(true);
            _armyGroupInGarrisonLookup = GetComponentLookup<ArmyGroupInGarrison>(true);
            _armyGroupAttrLookup = GetComponentLookup<ArmyGroupAttr>(true);
            _constructingTimerLookup = GetComponentLookup<ConstructingTimer>(true);
            _cityTaskUniqueIdLookup = GetComponentLookup<CityTaskUniqueId>(true);
            _cityAttrLookup = GetComponentLookup<CityAttr>(true);

            _saveArmyGroupQuery = SystemAPI.QueryBuilder().WithAll<InSubGameTag>().WithAll<ArmyGroupAttr>().Build();
        }

        protected override void OnStartRunning()
        {
            if (!_initialized)
            {
                SaveLoadController.Instance.OnEcsSaveArmyGroupSubData += SaveArmyGroupSubData;
                SaveLoadController.Instance.OnEcsSaveCitySubData += SaveCitySubData;
                SaveLoadController.Instance.OnEcsSaveCityMainData += SaveCityMainData;
                SaveLoadController.Instance.OnEcsSaveArmyGroupMainData += SaveArmyGroupMainData;
                SaveLoadController.Instance.OnEcsSaveGameMainData += SaveGameMainData;
                SaveLoadController.Instance.OnEcsCopyAndDeleteTmpSubData += CopyDeleteTmpOrInvalidSubData;
                _initialized = true;
            }
        }

        protected override void OnUpdate()
        {
        }


        private void SaveArmyGroupSubData(bool shouldSaveToTmp)
        {
            var playerSaveSlot = SystemAPI.GetSingleton<PlayerSaveSlot>().Value;
            var armyGroups = _saveArmyGroupQuery.ToEntityArray(Allocator.Temp);
            var armyGroupAttrs = _saveArmyGroupQuery.ToComponentDataArray<ArmyGroupAttr>(Allocator.Temp);


            for (var i = 0; i < armyGroups.Length; i++)
            {
                var armyGroup = armyGroups[i];
                var armyGroupAttr = armyGroupAttrs[i];
                var armyGroupUnits = SystemAPI.GetBuffer<ArmyGroupUnit>(armyGroup);

                // Calculate center position of all units in this army group
                var sum = float3.zero;
                for (var j = 0; j < armyGroupUnits.Length; j++)
                {
                    var transform = SystemAPI.GetComponent<LocalTransform>(armyGroupUnits[j].Unit);
                    sum += transform.Position;
                }

                var center = sum / armyGroupUnits.Length;
                float2 boundingMin = float2.zero, boundingMax = float2.zero;

                var ecb = new EntityCommandBuffer(Allocator.Temp);
                for (var index = 0; index < armyGroupUnits.Length; index++)
                {
                    var armyGroupUnit = armyGroupUnits[index];
                    var unit = armyGroupUnit.Unit;
                    var unitTmpId = SaveUtilities.GetTmpIdForSaving(unit);
                    armyGroupUnit.SaveTmpId = unitTmpId;
                    armyGroupUnits[index] = armyGroupUnit;

                    var transform = SystemAPI.GetComponent<LocalTransform>(unit);
                    var generalAttr = SystemAPI.GetComponent<SubGameplayGeneralAttr>(unit);
                    var statData = SystemAPI.GetComponent<StatData>(unit);
                    var expData = SystemAPI.GetComponent<ExpData>(unit);

                    var saveEntity = ecb.CreateEntity();
                    var relative = transform.Position - center;
                    boundingMin = math.min(boundingMin, relative.xz);
                    boundingMax = math.max(boundingMax, relative.xz);

                    ecb.AddComponent(saveEntity, new SeTransform
                    {
                        position = relative, // Save relative position
                        rotation = transform.Rotation,
                        scale = transform.Scale,
                    });

                    ecb.AddComponent(saveEntity, new SeGlobalId { value = generalAttr.PrefabID });
                    ecb.AddComponent(saveEntity, new SeTmpId { value = unitTmpId });
                    ecb.AddComponent(saveEntity, statData);
                    ecb.AddComponent(saveEntity, expData);
                    ecb.AddComponent(saveEntity, new SeInArmyGroup
                    {
                        armyGroupSaveId = armyGroupAttr.saveId
                    });
                }

                // Record the bounding box
                armyGroupAttr.boundingBoxDelta = boundingMax - boundingMin;
                armyGroupAttr.loadingCenter = center;
                armyGroupAttr.loadingScale = 1f;
                SystemAPI.SetComponent(armyGroup, armyGroupAttr);

                using (var serializeWorld = new World("Serialization World"))
                {
                    var seEm = serializeWorld.EntityManager;
                    ecb.Playback(seEm);
                    ecb.Dispose();
                    seEm.CreateSingleton(new SaveTmpTag());
                    seEm.RemoveComponent<SceneTag>(seEm.UniversalQuery);
                    seEm.RemoveComponent<SceneSection>(seEm.UniversalQuery);
                    var armyGroupSavePath =
                        SaveUtilities.GetArmyGroupSubDataPath(armyGroupAttr.saveId, playerSaveSlot, shouldSaveToTmp);
                    using (var writer = new StreamBinaryWriter(armyGroupSavePath))
                    {
                        SerializeUtility.SerializeWorld(seEm, writer);
                    }
                }
            }

            armyGroups.Dispose();
            armyGroupAttrs.Dispose();
        }

        private void SaveCitySubData(bool shouldSaveToTmp)
        {
            var city = SystemAPI.GetSingleton<SubGameStatusData>().City;
            var cityId = SystemAPI.GetComponent<CityAttr>(city).globalId;
            var citySavePath = SaveUtilities.GetCitySubDataPath(cityId,
                SystemAPI.GetSingleton<PlayerSaveSlot>().Value, shouldSaveToTmp);
            _garrisonEntitiesLookup.Update(this);
            _garrisonTypeDataLookup.Update(this);
            _inGarrisonLookup.Update(this);
            _physicsMassLookup.Update(this);
            _constructingTimerLookup.Update(this);
            _conjuringDataLookup.Update(this);
            _cityTaskUniqueIdLookup.Update(this);
            var ecb = new EntityCommandBuffer(Allocator.TempJob);
            var ecbP = ecb.AsParallelWriter();
            var saveJob = new SaveSubGameplayJob
            {
                ECB = ecbP,
                GarrisonEntitiesLookup = _garrisonEntitiesLookup,
                GarrisonTypeDataLookup = _garrisonTypeDataLookup,
                ConjuringDataLookup = _conjuringDataLookup,
                InGarrisonLookup = _inGarrisonLookup,
                PhysicsMassLookup = _physicsMassLookup,
                ConstructingTimerLookup = _constructingTimerLookup,
                CityTaskUniqueIdLookup = _cityTaskUniqueIdLookup,
            }.ScheduleParallel(Dependency);
            saveJob.Complete();
            using (var serializeWorld = new World("Serialization World"))
            {
                EntityManager seEm = serializeWorld.EntityManager;
                ecb.Playback(seEm);
                ecb.Dispose();
                seEm.CreateSingleton(new SaveTmpTag());
                seEm.RemoveComponent<SceneTag>(seEm.UniversalQuery);
                seEm.RemoveComponent<SceneSection>(seEm.UniversalQuery);
                // Save
                using (var writer =
                       new StreamBinaryWriter(citySavePath))
                {
                    SerializeUtility.SerializeWorld(seEm, writer);
                }
            }
        }

        private void SaveCityMainData()
        {
            var path = SaveUtilities.GetCityMainDataPath(SystemAPI.GetSingleton<PlayerSaveSlot>().Value);
            var ecb = new EntityCommandBuffer(Allocator.TempJob);
            _armyGroupAttrLookup.Update(this);
            var job = new SaveCityMainDataJob
            {
                ECB = ecb.AsParallelWriter(),
                ArmyGroupAttrLookup = _armyGroupAttrLookup,
            }.ScheduleParallel(Dependency);
            job.Complete();

            using (var serializeWorld = new World("Serialization World"))
            {
                EntityManager seEm = serializeWorld.EntityManager;
                ecb.Playback(seEm);
                ecb.Dispose();
                seEm.CreateSingleton(new SaveTmpTag());
                seEm.RemoveComponent<SceneTag>(seEm.UniversalQuery);
                seEm.RemoveComponent<SceneSection>(seEm.UniversalQuery);
                // Save
                using (var writer =
                       new StreamBinaryWriter(path))
                {
                    SerializeUtility.SerializeWorld(seEm, writer);
                }
            }
        }

        private void SaveArmyGroupMainData()
        {
            _movingTagLookup.Update(this);
            _calculateEnableLookup.Update(this);
            _armyGroupInGarrisonLookup.Update(this);
            _cityAttrLookup.Update(this);
            var path = SaveUtilities.GetArmyGroupMainDataPath(SystemAPI.GetSingleton<PlayerSaveSlot>().Value);
            var ecb = new EntityCommandBuffer(Allocator.TempJob);
            var job = new SaveArmyGroupMainDataJob
            {
                ECB = ecb.AsParallelWriter(),
                ArmyGroupCalculateEnableLookup = _calculateEnableLookup,
                ArmyGroupMovingTagLookup = _movingTagLookup,
                ArmyGroupInGarrisonLookup = _armyGroupInGarrisonLookup,
                CityAttrLookup = _cityAttrLookup
            }.ScheduleParallel(Dependency);
            job.Complete();
            using (var serializeWorld = new World("Serialization World"))
            {
                EntityManager seEm = serializeWorld.EntityManager;
                ecb.Playback(seEm);
                ecb.Dispose();
                seEm.CreateSingleton(new SaveTmpTag());
                seEm.RemoveComponent<SceneTag>(seEm.UniversalQuery);
                seEm.RemoveComponent<SceneSection>(seEm.UniversalQuery);
                // Save
                using (var writer =
                       new StreamBinaryWriter(path))
                {
                    SerializeUtility.SerializeWorld(seEm, writer);
                }
            }
        }

        private void SaveGameMainData()
        {
            var path = SaveUtilities.GetGameMainDataPath(SystemAPI.GetSingleton<PlayerSaveSlot>().Value);

            var entities = new NativeList<Entity>(Allocator.Temp);
            entities.Add(SystemAPI.GetSingletonEntity<PlayerFactionData>());
            entities.Add(SystemAPI.GetSingletonEntity<WorldTimeData>());
            entities.Add(SystemAPI.GetSingletonEntity<LastUniqueId>());
            entities.Add(SystemAPI.GetSingletonEntity<ResourceData>());
            entities.Add(SystemAPI.GetSingletonEntity<PopulationResourceData>());
            entities.Add(SystemAPI.GetSingletonEntity<PopulationStorageAddTask>());
            entities.Add(SystemAPI.GetSingletonEntity<PopulationConjureTask>());
            var subGameStatusData = SystemAPI.GetSingleton<SubGameStatusData>();
            if (subGameStatusData.SubGameStatus != SubGameStatus.None)
            {
                var cityAttr = SystemAPI.GetComponent<CityAttr>(subGameStatusData.City);
                SystemAPI.SetSingleton(new LastTimeSaveCityId
                {
                    value = cityAttr.globalId,
                });
            }
            else
            {
                SystemAPI.SetSingleton(new LastTimeSaveCityId());
            }

            entities.Add(SystemAPI.GetSingletonEntity<LastTimeSaveCityId>());


            using (var serializeWorld = new World("Serialization World"))
            {
                EntityManager seEm = serializeWorld.EntityManager;
                seEm.CopyEntitiesFrom(EntityManager, entities.AsArray());
                seEm.CreateSingleton(new SaveTmpTag());
                seEm.RemoveComponent<SceneTag>(seEm.UniversalQuery);
                seEm.RemoveComponent<SceneSection>(seEm.UniversalQuery);

                // Save
                using (var writer =
                       new StreamBinaryWriter(path))
                {
                    SerializeUtility.SerializeWorld(seEm, writer);
                }
            }
        }

        private void CopyDeleteTmpOrInvalidSubData()
        {
            var playerSaveSlot = SystemAPI.GetSingleton<PlayerSaveSlot>();

            // Copy city sub data from tmp to true save path and delete tmp path
            foreach (var cityAttr in SystemAPI.Query<RefRO<CityAttr>>())
            {
                var tmpPath = SaveUtilities.GetCitySubDataPath(cityAttr.ValueRO.globalId,
                    playerSaveSlot.Value, true);
                if (File.Exists(tmpPath))
                {
                    var truePath = SaveUtilities.GetCitySubDataPath(cityAttr.ValueRO.globalId,
                        playerSaveSlot.Value, false);
                    File.Copy(tmpPath, truePath, overwrite: true);
                    File.Delete(tmpPath);
                }
            }
            // Copy army group sub data from tmp to true save path and delete tmp path

            foreach (var armyGroupAttr in SystemAPI.Query<RefRO<ArmyGroupAttr>>())
            {
                var tmpPath = SaveUtilities.GetArmyGroupSubDataPath(armyGroupAttr.ValueRO.saveId, playerSaveSlot.Value,
                    true);
                if (File.Exists(tmpPath))
                {
                    var truePath = SaveUtilities.GetArmyGroupSubDataPath(armyGroupAttr.ValueRO.saveId,
                        playerSaveSlot.Value, false);
                    File.Copy(tmpPath, truePath, overwrite: true);
                    File.Delete(tmpPath);
                }
            }
            
            // Delete non-player city sub data if it exists, delete dead army group sub data if it exists
            
            var playerCityQuery = SystemAPI.QueryBuilder().WithAll<CityAttr>().WithAll<PlayerTag>().Build();
            var playerArmyGroups = SystemAPI.QueryBuilder().WithAll<ArmyGroupAttr>()
                .WithAll<PlayerTag>().Build();
            var cityAttrs = playerCityQuery.ToComponentDataArray<CityAttr>(Allocator.Temp);
            var armyGroupAttrs = playerArmyGroups.ToComponentDataArray<ArmyGroupAttr>(Allocator.Temp);
            var cityValidSaveIds = new NativeHashSet<int>(3, Allocator.Temp);
            var armyGroupValidSaveIds = new NativeHashSet<long>(3, Allocator.Temp);

            foreach (var cityAttr in cityAttrs)
            {
                cityValidSaveIds.Add(cityAttr.globalId);
            }

            foreach (var armyGroupAttr in armyGroupAttrs)
            {
                armyGroupValidSaveIds.Add(armyGroupAttr.saveId);
            }

            var citySubDataFolder = SaveUtilities.GetCitySubDataFolder(playerSaveSlot.Value);
            var armyGroupSubDataFolder = SaveUtilities.GetArmyGroupSubDataFolder(playerSaveSlot.Value);
            var citySubDataFiles = Directory.GetFiles(citySubDataFolder);
            var armyGroupSubDataFiles = Directory.GetFiles(armyGroupSubDataFolder);
            
            foreach (var citySubDataFile in citySubDataFiles)
            {
                var fileName = citySubDataFile.Split(".")[0];
                if (int.TryParse(fileName, out var cityGlobalId))
                {
                    if (!cityValidSaveIds.Contains(cityGlobalId))
                    {
                        File.Delete(citySubDataFile);
                    }
                }
            }

            foreach (var armyGroupSubDataFile in armyGroupSubDataFiles)
            {
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
    }
}