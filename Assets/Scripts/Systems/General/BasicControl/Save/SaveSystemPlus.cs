using SparFlame.Components.General;
using SparFlame.Components.MainGameplay;
using SparFlame.Components.SubGameplay;
using Unity.Collections;
using Unity.Entities;
using Unity.Entities.Serialization;
using Unity.Physics;

namespace SparFlame.Systems.General.BasicControl
{
    public partial class SaveSystemPlus : SystemBase
    {
        private EntityQuery _saveArmyGroupQuery;

        public struct SaveTmpTag : IComponentData
        {
            public bool Value;
        }

        private BufferLookup<GarrisonEntity> _garrisonEntitiesLookup;
        private BufferLookup<GarrisonTypeData> _garrisonTypeDataLookup;
        private ComponentLookup<InGarrison> _inGarrisonLookup;
        private ComponentLookup<PhysicsMass> _physicsMassLookup;

        private bool _initialized;

        protected override void OnCreate()
        {
            RequireForUpdate<SaveLoadConfig>();
            RequireForUpdate<PlayerSaveSlot>();
            RequireForUpdate<SubGameStatusData>();
            _garrisonEntitiesLookup = GetBufferLookup<GarrisonEntity>(true);
            _garrisonTypeDataLookup = GetBufferLookup<GarrisonTypeData>(true);
            _inGarrisonLookup = GetComponentLookup<InGarrison>(true);
            _physicsMassLookup = GetComponentLookup<PhysicsMass>(true);
            _saveArmyGroupQuery = SystemAPI.QueryBuilder().WithAll<InSubGameTag>().WithAll<ArmyGroupAttr>().Build();
            
        }

        protected override void OnStartRunning()
        {
            if (!_initialized)
            {
                SaveLoadController.Instance.OnEcsSaveArmyGroupSubData += SaveArmyGroupSubData;
                SaveLoadController.Instance.OnEcsSaveCitySubData += SaveCitySubData;
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
                    var armyGroupUnits = SystemAPI.GetBuffer<ArmyGroupUnit>(armyGroup);
                    var tmp = new NativeArray<Entity>(armyGroupUnits.Length, Allocator.Temp);
                    for (var index = 0; index < armyGroupUnits.Length; index++)
                    {
                        var armyGroupUnit = armyGroupUnits[index];
                        tmp[index] = armyGroupUnit.Unit;
                    }
                    using (var serializeWorld = new World("Serialization World"))
                    {
                        var seEm = serializeWorld.EntityManager;
                        seEm.CopyEntitiesFrom(EntityManager, tmp);
                        seEm.CreateSingleton(new SaveTmpTag());
                        seEm.RemoveComponent<SceneTag>(seEm.UniversalQuery);
                        seEm.RemoveComponent<SceneSection>(seEm.UniversalQuery);
                        var armyGroupAttr = armyGroupAttrs[i];
                        var armyGroupSavePath = SaveUtilities.GetArmyGroupSavePath(armyGroupAttr.SaveId,playerSaveSlot);
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
            var cityId = SystemAPI.GetComponent<CityAttr>(city).ID;
            var citySavePath = SaveUtilities.GetCitySavePath(cityId,
                SystemAPI.GetSingleton<PlayerSaveSlot>().Value);
            _garrisonEntitiesLookup.Update(this);
            _garrisonTypeDataLookup.Update(this);
            _inGarrisonLookup.Update(this);
            _physicsMassLookup.Update(this);
            
            var ecb = new EntityCommandBuffer(Allocator.TempJob);
            var ecbP = ecb.AsParallelWriter();
            var saveJob = new SaveSubGameplayJob
            {
                ECB = ecbP,
                GarrisonEntitiesLookup = _garrisonEntitiesLookup,
                GarrisonTypeDataLookup = _garrisonTypeDataLookup,
                InGarrisonLookup = _inGarrisonLookup,
                PhysicsMassLookup = _physicsMassLookup,
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
    }
}