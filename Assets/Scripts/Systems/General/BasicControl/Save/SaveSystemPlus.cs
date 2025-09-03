using SparFlame.Components.General;
using SparFlame.Components.MainGameplay;
using SparFlame.Components.SubGameplay;
using SparFlame.Core.Utils;
using Unity.Collections;
using Unity.Entities;
using Unity.Entities.Serialization;
using Unity.Physics;
using Unity.Transforms;
// ReSharper disable ConvertToUsingDeclaration

namespace SparFlame.Systems.General.BasicControl
{
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
                _initialized = true;
            }
        }

        protected override void OnUpdate()
        {
        }


        private void SaveArmyGroupSubData()
        {
            var playerSaveSlot = SystemAPI.GetSingleton<PlayerSaveSlot>().Value;
            var armyGroups = _saveArmyGroupQuery.ToEntityArray(Allocator.Temp);
            var armyGroupAttrs = _saveArmyGroupQuery.ToComponentDataArray<ArmyGroupAttr>(Allocator.Temp);
            for (var i = 0; i < armyGroups.Length; i++)
            {
                var armyGroup = armyGroups[i];
                var armyGroupAttr = armyGroupAttrs[i];
                var armyGroupUnits = SystemAPI.GetBuffer<ArmyGroupUnit>(armyGroup);
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
                    ecb.AddComponent(saveEntity, new SeTransform
                    {
                        position = transform.Position,
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

                    // Units in army group cannot be in garrison state
                    // if (SystemAPI.HasComponent<InGarrison>(unit))
                    // {
                    //     var inGarrison = SystemAPI.GetComponent<InGarrison>(unit);
                    //     var physicsMass = SystemAPI.GetComponent<PhysicsMass>(unit);
                    //     ecb.AddComponent(saveEntity, new SeInverseMass { value = physicsMass.InverseMass });
                    //     ecb.AddComponent(saveEntity, new SeInGarrison
                    //     {
                    //         buildingTmpId = SaveUtilities.GetTmpIdForSaving(inGarrison.BuildingEntity),
                    //         inBuilding = inGarrison.InBuilding,
                    //         priorMass = inGarrison.PriorMass,
                    //     });
                    //     ecb.AddComponent(saveEntity,
                    //         new SeTmpId { value = unitTmpId });
                    // }
                }

                using (var serializeWorld = new World("Serialization World"))
                {
                    var seEm = serializeWorld.EntityManager;
                    ecb.Playback(seEm);
                    ecb.Dispose();
                    seEm.CreateSingleton(new SaveTmpTag());
                    seEm.RemoveComponent<SceneTag>(seEm.UniversalQuery);
                    seEm.RemoveComponent<SceneSection>(seEm.UniversalQuery);
                    var armyGroupSavePath = SaveUtilities.GetArmyGroupSubDataPath(armyGroupAttr.saveId, playerSaveSlot);
                    using (var writer = new StreamBinaryWriter(armyGroupSavePath))
                    {
                        SerializeUtility.SerializeWorld(seEm, writer);
                    }
                }
            }

            armyGroups.Dispose();
            armyGroupAttrs.Dispose();
        }

        private void SaveCitySubData()
        {
            var city = SystemAPI.GetSingleton<SubGameStatusData>().City;
            var cityId = SystemAPI.GetComponent<CityAttr>(city).globalId;
            var citySavePath = SaveUtilities.GetCitySubDataPath(cityId,
                SystemAPI.GetSingleton<PlayerSaveSlot>().Value);
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
                ConjuringDataLookup =   _conjuringDataLookup,
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
            var path = SaveUtilities.GetArmyGroupMainDataPath(SystemAPI.GetSingleton<PlayerSaveSlot>().Value);
            var ecb = new EntityCommandBuffer(Allocator.TempJob);
            var job = new SaveArmyGroupMainDataJob
            {
                ECB = ecb.AsParallelWriter(),
                ArmyGroupCalculateEnableLookup = _calculateEnableLookup,
                ArmyGroupMovingTagLookup = _movingTagLookup,
                ArmyGroupInGarrisonLookup = _armyGroupInGarrisonLookup
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
            entities.Add( SystemAPI.GetSingletonEntity<PlayerFactionData>());
            entities.Add(SystemAPI.GetSingletonEntity<WorldTimeData>());
            entities.Add(SystemAPI.GetSingletonEntity<LastUniqueId>());
            entities.Add(SystemAPI.GetSingletonEntity<ResourceData>());
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
    }
}