// using System.IO;
// using SparFlame.Components.General;
// using SparFlame.Components.MainGameplay;
// using SparFlame.Components.SubGameplay;
// using SparFlame.Core.Utils;
// using Unity.Collections;
// using Unity.Entities;
// using Unity.Entities.Serialization;
// using Unity.Mathematics;
// using Unity.Physics;
// using Unity.Transforms;
//
// // ReSharper disable ConvertToUsingDeclaration
//
// namespace SparFlame.Systems.General.BasicControl
// {
//     [UpdateInGroup(typeof(InitializationSystemGroup)), UpdateAfter(typeof(GameBasicControlSystem))]
//     public partial class SaveSystemPlus : SystemBase
//     {
//         private EntityQuery _saveArmyGroupQuery;
//         private EntityQuery _enemySpecificArmyGroupSaveQuery;
//
//         public struct SaveTmpTag : IComponentData
//         {
//             public bool NoUseButCannotDelete;
//         }
//
//         private BufferLookup<GarrisonEntity> _garrisonEntitiesLookup;
//         private BufferLookup<GarrisonTypeData> _garrisonTypeDataLookup;
//         private BufferLookup<ConjuringData> _conjuringDataLookup;
//
//         private ComponentLookup<InGarrison> _inGarrisonLookup;
//         private ComponentLookup<PhysicsMass> _physicsMassLookup;
//         private ComponentLookup<ArmyGroupMovingTag> _movingTagLookup;
//         private ComponentLookup<ArmyGroupCalculateEnable> _calculateEnableLookup;
//         private ComponentLookup<ArmyGroupInGarrison> _armyGroupInGarrisonLookup;
//         private ComponentLookup<ConstructingTimer> _constructingTimerLookup;
//         private ComponentLookup<CityTaskUniqueId> _cityTaskUniqueIdLookup;
//         private ComponentLookup<CityAttr> _cityAttrLookup;
//         private ComponentLookup<GlobalSingleId> _globalSingleIdLookup;
//
//
//         private bool _initialized;
//
//         protected override void OnCreate()
//         {
//             RequireForUpdate<SaveLoadConfig>();
//             RequireForUpdate<PlayerSaveSlot>();
//             RequireForUpdate<SubGameStatusData>();
//             _garrisonEntitiesLookup = GetBufferLookup<GarrisonEntity>(true);
//             _garrisonTypeDataLookup = GetBufferLookup<GarrisonTypeData>(true);
//             _conjuringDataLookup = GetBufferLookup<ConjuringData>(true);
//
//             _inGarrisonLookup = GetComponentLookup<InGarrison>(true);
//             _physicsMassLookup = GetComponentLookup<PhysicsMass>(true);
//             _movingTagLookup = GetComponentLookup<ArmyGroupMovingTag>(true);
//             _calculateEnableLookup = GetComponentLookup<ArmyGroupCalculateEnable>(true);
//             _armyGroupInGarrisonLookup = GetComponentLookup<ArmyGroupInGarrison>(true);
//             _constructingTimerLookup = GetComponentLookup<ConstructingTimer>(true);
//             _cityTaskUniqueIdLookup = GetComponentLookup<CityTaskUniqueId>(true);
//             _cityAttrLookup = GetComponentLookup<CityAttr>(true);
//             _globalSingleIdLookup = GetComponentLookup<GlobalSingleId>(true);
//
//             _saveArmyGroupQuery = SystemAPI.QueryBuilder().WithAll<InSubGameTag>().WithAll<ArmyGroupAttr>().Build();
//             _enemySpecificArmyGroupSaveQuery = SystemAPI.QueryBuilder().WithAll<NeedSaveTag>()
//                 .WithAll<ArmyGroupAttr>().Build();
//         }
//
//         protected override void OnStartRunning()
//         {
//             if (!_initialized)
//             {
//                 SaveLoadController.Instance.OnEcsSaveArmyGroupSubData += SaveArmyGroupSubData;
//                 SaveLoadController.Instance.OnEcsSaveCitySubData += SaveCitySubData;
//                 SaveLoadController.Instance.OnEcsSaveCityMainData += SaveCityMainData;
//                 SaveLoadController.Instance.OnEcsSaveArmyGroupMainData += SaveArmyGroupMainData;
//                 SaveLoadController.Instance.OnEcsSaveGameMainData += SaveGameMainData;
//                 SaveLoadController.Instance.OnEcsCopyAndDeleteTmpSubData += CopyDeleteTmpOrInvalidSubData;
//                 SaveLoadController.Instance.OnEcsSaveEnemySpecificArmyGroupSubData += SaveEnemySpecificArmyGroupSubData;
//                 _initialized = true;
//             }
//         }
//
//         protected override void OnUpdate()
//         {
//         }
//
//
//         private void SaveArmyGroupSubData(bool shouldSaveToTmp)
//         {
//             var playerSaveSlot = SystemAPI.GetSingleton<PlayerSaveSlot>().Value;
//             var armyGroups = _saveArmyGroupQuery.ToEntityArray(Allocator.Temp);
//
//
//             foreach (var armyGroup in armyGroups)
//             {
//                 SaveSingleArmyGroupSubData(shouldSaveToTmp, armyGroup, playerSaveSlot);
//             }
//
//             armyGroups.Dispose();
//         }
//
//         private void SaveEnemySpecificArmyGroupSubData()
//         {
//             var playerSaveSlot = SystemAPI.GetSingleton<PlayerSaveSlot>().Value;
//             var ecb = new EntityCommandBuffer(Allocator.Temp);
//             var enemyArmyGroups = _enemySpecificArmyGroupSaveQuery.ToEntityArray(Allocator.Temp);
//             foreach (var armyGroup in enemyArmyGroups)
//             {
//                 ecb.SetComponentEnabled<NeedSaveTag>(armyGroup,false);
//                 SaveSingleArmyGroupSubData(true, armyGroup, playerSaveSlot,
//                     true);
//             }
//
//             ecb.Playback(EntityManager);
//             ecb.Dispose();
//             enemyArmyGroups.Dispose();
//         }
//
//         private void SaveSingleArmyGroupSubData(
//             bool shouldSaveToTmp, Entity armyGroup, int playerSaveSlot,
//             bool shouldDestroyUnit = false)
//         {
//             var armyGroupUnits = SystemAPI.GetBuffer<ArmyGroupUnit>(armyGroup);
//             var armyGroupAttr = SystemAPI.GetComponent<ArmyGroupAttr>(armyGroup);
//             var armyGroupSingleId = SystemAPI.GetComponent<GlobalSingleId>(armyGroup).value;
//             // Calculate center position of all units in this army group
//             var sum = float3.zero;
//             for (var j = 0; j < armyGroupUnits.Length; j++)
//             {
//                 var transform = SystemAPI.GetComponent<LocalTransform>(armyGroupUnits[j].Unit);
//                 sum += transform.Position;
//             }
//
//             var center = sum / armyGroupUnits.Length;
//             float2 boundingMin = float2.zero, boundingMax = float2.zero;
//
//             var ecb = new EntityCommandBuffer(Allocator.Temp);
//             var ecbMainWorld = new EntityCommandBuffer(Allocator.Temp);
//
//             for (var index = 0; index < armyGroupUnits.Length; index++)
//             {
//                 var armyGroupUnit = armyGroupUnits[index];
//                 var unit = armyGroupUnit.Unit;
//                 if (shouldDestroyUnit)
//                     ecbMainWorld.DestroyEntity(unit);
//
//                 var generalAttr = SystemAPI.GetComponent<SubGameplayGeneralAttr>(unit);
//
//                 var unitSingleId = SystemAPI.GetComponent<GlobalSingleId>(unit).value;
//                 armyGroupUnit.SingleId = unitSingleId;
//                 armyGroupUnits[index] = armyGroupUnit;
//
//                 var transform = SystemAPI.GetComponent<LocalTransform>(unit);
//                 var statData = SystemAPI.GetComponent<StatData>(unit);
//                 var expData = SystemAPI.GetComponent<ExpData>(unit);
//
//                 var saveEntity = ecb.CreateEntity();
//                 var relative = transform.Position - center;
//                 boundingMin = math.min(boundingMin, relative.xz);
//                 boundingMax = math.max(boundingMax, relative.xz);
//
//                 ecb.AddComponent(saveEntity, new SeTransform
//                 {
//                     position = relative, // Save relative position
//                     rotation = transform.Rotation,
//                     scale = transform.Scale,
//                 });
//
//                 ecb.AddComponent(saveEntity, new SePrefabId { value = generalAttr.PrefabID });
//                 ecb.AddComponent(saveEntity, new SeSingleId { value = unitSingleId });
//                 ecb.AddComponent(saveEntity, statData);
//                 ecb.AddComponent(saveEntity, expData);
//                 ecb.AddComponent(saveEntity, new SeInArmyGroup
//                 {
//                     armyGroupSaveId = armyGroupSingleId
//                 });
//             }
//
//             // Record the bounding box
//             armyGroupAttr.boundingBoxDelta = boundingMax - boundingMin;
//             armyGroupAttr.loadingCenter = center;
//             armyGroupAttr.loadingScale = 1f;
//             SystemAPI.SetComponent(armyGroup, armyGroupAttr);
//
//             using (var serializeWorld = new World("Serialization World"))
//             {
//                 var seEm = serializeWorld.EntityManager;
//                 ecb.Playback(seEm);
//                 ecb.Dispose();
//                 seEm.CreateSingleton(new SaveTmpTag());
//                 seEm.RemoveComponent<SceneTag>(seEm.UniversalQuery);
//                 seEm.RemoveComponent<SceneSection>(seEm.UniversalQuery);
//                 var armyGroupSavePath =
//                     SaveUtilities.GetArmyGroupSubDataPath(armyGroupSingleId, playerSaveSlot, shouldSaveToTmp);
//                 using (var writer = new StreamBinaryWriter(armyGroupSavePath))
//                 {
//                     SerializeUtility.SerializeWorld(seEm, writer);
//                 }
//             }
//
//             ecbMainWorld.Playback(EntityManager);
//             ecbMainWorld.Dispose();
//         }
//
//         private void SaveCitySubData(bool shouldSaveToTmp)
//         {
//             var city = SystemAPI.GetSingleton<SubGameStatusData>().City;
//             var cityId = SystemAPI.GetComponent<CityAttr>(city).globalId;
//             var citySavePath = SaveUtilities.GetCitySubDataPath(cityId,
//                 SystemAPI.GetSingleton<PlayerSaveSlot>().Value, shouldSaveToTmp);
//             _garrisonEntitiesLookup.Update(this);
//             _garrisonTypeDataLookup.Update(this);
//             _inGarrisonLookup.Update(this);
//             _physicsMassLookup.Update(this);
//             _constructingTimerLookup.Update(this);
//             _conjuringDataLookup.Update(this);
//             _cityTaskUniqueIdLookup.Update(this);
//             _globalSingleIdLookup.Update(this);
//             var ecb = new EntityCommandBuffer(Allocator.Persistent);
//             var ecbP = ecb.AsParallelWriter();
//
//             var saveJob = new SaveSubGameplayJob
//             {
//                 ECB = ecbP,
//                 GarrisonEntitiesLookup = _garrisonEntitiesLookup,
//                 GarrisonTypeDataLookup = _garrisonTypeDataLookup,
//                 ConjuringDataLookup = _conjuringDataLookup,
//                 InGarrisonLookup = _inGarrisonLookup,
//                 PhysicsMassLookup = _physicsMassLookup,
//                 ConstructingTimerLookup = _constructingTimerLookup,
//                 CityTaskUniqueIdLookup = _cityTaskUniqueIdLookup,
//                 GlobalSingleIdLookup = _globalSingleIdLookup
//             }.ScheduleParallel(Dependency);
//             saveJob.Complete();
//             using (var serializeWorld = new World("Serialization World"))
//             {
//                 EntityManager seEm = serializeWorld.EntityManager;
//                 ecb.Playback(seEm);
//                 ecb.Dispose();
//                 seEm.CreateSingleton(new SaveTmpTag());
//                 seEm.RemoveComponent<SceneTag>(seEm.UniversalQuery);
//                 seEm.RemoveComponent<SceneSection>(seEm.UniversalQuery);
//                 // Save
//                 using (var writer =
//                        new StreamBinaryWriter(citySavePath))
//                 {
//                     SerializeUtility.SerializeWorld(seEm, writer);
//                 }
//             }
//         }
//
//         private void SaveCityMainData()
//         {
//             var path = SaveUtilities.GetCityMainDataPath(SystemAPI.GetSingleton<PlayerSaveSlot>().Value);
//             var ecb = new EntityCommandBuffer(Allocator.Persistent);
//             _globalSingleIdLookup.Update(this);
//             var job = new SaveCityMainDataJob
//             {
//                 ECB = ecb.AsParallelWriter(),
//                 GlobalSingleIdLookup = _globalSingleIdLookup,
//             }.ScheduleParallel(Dependency);
//             job.Complete();
//
//             using (var serializeWorld = new World("Serialization World"))
//             {
//                 EntityManager seEm = serializeWorld.EntityManager;
//                 ecb.Playback(seEm);
//                 ecb.Dispose();
//                 seEm.CreateSingleton(new SaveTmpTag());
//                 seEm.RemoveComponent<SceneTag>(seEm.UniversalQuery);
//                 seEm.RemoveComponent<SceneSection>(seEm.UniversalQuery);
//                 // Save
//                 using (var writer =
//                        new StreamBinaryWriter(path))
//                 {
//                     SerializeUtility.SerializeWorld(seEm, writer);
//                 }
//             }
//         }
//
//         private void SaveArmyGroupMainData()
//         {
//             _movingTagLookup.Update(this);
//             _calculateEnableLookup.Update(this);
//             _armyGroupInGarrisonLookup.Update(this);
//             _cityAttrLookup.Update(this);
//             _globalSingleIdLookup.Update(this);
//             var path = SaveUtilities.GetArmyGroupMainDataPath(SystemAPI.GetSingleton<PlayerSaveSlot>().Value);
//             var ecb = new EntityCommandBuffer(Allocator.Persistent);
//             var job = new SaveArmyGroupMainDataJob
//             {
//                 ECB = ecb.AsParallelWriter(),
//                 ArmyGroupCalculateEnableLookup = _calculateEnableLookup,
//                 ArmyGroupMovingTagLookup = _movingTagLookup,
//                 ArmyGroupInGarrisonLookup = _armyGroupInGarrisonLookup,
//                 PrefabIdLookup = _cityAttrLookup,
//                 GlobalSingleIdLookup = _globalSingleIdLookup
//             }.ScheduleParallel(Dependency);
//             job.Complete();
//             using (var serializeWorld = new World("Serialization World"))
//             {
//                 var seEm = serializeWorld.EntityManager;
//                 ecb.Playback(seEm);
//                 ecb.Dispose();
//                 seEm.CreateSingleton(new SaveTmpTag());
//                 seEm.RemoveComponent<SceneTag>(seEm.UniversalQuery);
//                 seEm.RemoveComponent<SceneSection>(seEm.UniversalQuery);
//                 // Save
//                 using (var writer =
//                        new StreamBinaryWriter(path))
//                 {
//                     SerializeUtility.SerializeWorld(seEm, writer);
//                 }
//             }
//         }
//
//         private void SaveGameMainData()
//         {
//             var path = SaveUtilities.GetGameMainDataPath(SystemAPI.GetSingleton<PlayerSaveSlot>().Value);
//
//             var entities = new NativeList<Entity>(Allocator.Temp);
//             entities.Add(SystemAPI.GetSingletonEntity<PlayerFactionData>());
//             entities.Add(SystemAPI.GetSingletonEntity<WorldTimeData>());
//             entities.Add(SystemAPI.GetSingletonEntity<LastUniqueId>());
//             entities.Add(SystemAPI.GetSingletonEntity<ResourceData>());
//             entities.Add(SystemAPI.GetSingletonEntity<PopulationResourceData>());
//             entities.Add(SystemAPI.GetSingletonEntity<PopulationStorageAddTask>());
//             entities.Add(SystemAPI.GetSingletonEntity<PopulationConjureTask>());
//             entities.Add(SystemAPI.GetSingletonEntity<CameraMainGameplayHistory>());
//             entities.Add(SystemAPI.GetSingletonEntity<GlobalSingIDCounter>());
//             var subGameStatusData = SystemAPI.GetSingleton<SubGameStatusData>();
//             if (subGameStatusData.SubGameStatus != SubGameStatus.None)
//             {
//                 var cityAttr = SystemAPI.GetComponent<CityAttr>(subGameStatusData.City);
//                 SystemAPI.SetSingleton(new LastTimeSaveCityId
//                 {
//                     value = cityAttr.globalId,
//                 });
//             }
//             else
//             {
//                 SystemAPI.SetSingleton(new LastTimeSaveCityId());
//             }
//
//             entities.Add(SystemAPI.GetSingletonEntity<LastTimeSaveCityId>());
//
//
//             using (var serializeWorld = new World("Serialization World"))
//             {
//                 EntityManager seEm = serializeWorld.EntityManager;
//                 seEm.CopyEntitiesFrom(EntityManager, entities.AsArray());
//                 seEm.CreateSingleton(new SaveTmpTag());
//                 seEm.RemoveComponent<SceneTag>(seEm.UniversalQuery);
//                 seEm.RemoveComponent<SceneSection>(seEm.UniversalQuery);
//
//                 // Save
//                 using (var writer =
//                        new StreamBinaryWriter(path))
//                 {
//                     SerializeUtility.SerializeWorld(seEm, writer);
//                 }
//             }
//         }
//
//         private void CopyDeleteTmpOrInvalidSubData()
//         {
//             var playerSaveSlot = SystemAPI.GetSingleton<PlayerSaveSlot>();
//
//             // Copy city sub data from tmp to true save path and delete tmp path
//             foreach (var cityAttr in SystemAPI.Query<RefRO<CityAttr>>())
//             {
//                 var tmpPath = SaveUtilities.GetCitySubDataPath(cityAttr.ValueRO.globalId,
//                     playerSaveSlot.Value, true);
//                 if (File.Exists(tmpPath))
//                 {
//                     var truePath = SaveUtilities.GetCitySubDataPath(cityAttr.ValueRO.globalId,
//                         playerSaveSlot.Value, false);
//                     File.Copy(tmpPath, truePath, overwrite: true);
//                     File.Delete(tmpPath);
//                 }
//             }
//             // Copy army group sub data from tmp to true save path and delete tmp path
//
//             foreach (var globalSingleId in SystemAPI.Query<RefRO<GlobalSingleId>>().WithAll<ArmyGroupAttr>())
//             {
//                 var tmpPath = SaveUtilities.GetArmyGroupSubDataPath(globalSingleId.ValueRO.value, playerSaveSlot.Value,
//                     true);
//                 if (File.Exists(tmpPath))
//                 {
//                     var truePath = SaveUtilities.GetArmyGroupSubDataPath(globalSingleId.ValueRO.value,
//                         playerSaveSlot.Value, false);
//                     File.Copy(tmpPath, truePath, overwrite: true);
//                     File.Delete(tmpPath);
//                 }
//             }
//
//             // Delete non-player city sub data if it exists, delete dead army group sub data if it exists
//
//             var playerCityQuery = SystemAPI.QueryBuilder().WithAll<CityAttr>().WithAll<PlayerTag>().Build();
//             var playerArmyGroups = SystemAPI.QueryBuilder().WithAll<ArmyGroupAttr>().WithAll<GlobalSingleId>()
//                 .WithAll<PlayerTag>().Build();
//             var cityAttrs = playerCityQuery.ToComponentDataArray<CityAttr>(Allocator.Temp);
//             var armyGroupAttrs = playerArmyGroups.ToComponentDataArray<GlobalSingleId>(Allocator.Temp);
//             var cityValidSaveIds = new NativeHashSet<int>(3, Allocator.Temp);
//             var armyGroupValidSaveIds = new NativeHashSet<long>(3, Allocator.Temp);
//
//             foreach (var cityAttr in cityAttrs)
//             {
//                 cityValidSaveIds.Add(cityAttr.globalId);
//             }
//
//             foreach (var globalSingleId in armyGroupAttrs)
//             {
//                 armyGroupValidSaveIds.Add(globalSingleId.value);
//             }
//
//             var citySubDataFolder = SaveUtilities.GetCitySubDataFolder(playerSaveSlot.Value);
//             var armyGroupSubDataFolder = SaveUtilities.GetArmyGroupSubDataFolder(playerSaveSlot.Value);
//             var citySubDataFiles = Directory.GetFiles(citySubDataFolder);
//             var armyGroupSubDataFiles = Directory.GetFiles(armyGroupSubDataFolder);
//
//             foreach (var citySubDataFile in citySubDataFiles)
//             {
//                 var fileName = citySubDataFile.Split(".")[0];
//                 if (int.TryParse(fileName, out var cityGlobalId))
//                 {
//                     if (!cityValidSaveIds.Contains(cityGlobalId))
//                     {
//                         File.Delete(citySubDataFile);
//                     }
//                 }
//             }
//
//             foreach (var armyGroupSubDataFile in armyGroupSubDataFiles)
//             {
//                 var fileName = armyGroupSubDataFile.Split(".")[0];
//                 if (long.TryParse(fileName, out var armyGroupSaveId))
//                 {
//                     if (!armyGroupValidSaveIds.Contains(armyGroupSaveId))
//                     {
//                         File.Delete(armyGroupSubDataFile);
//                     }
//                 }
//             }
//         }
//     }
// }