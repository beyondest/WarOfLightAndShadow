using SparFlame.Components.SubGameplay;
using Unity.Collections;
using Unity.Entities;
using Unity.Entities.Serialization;
using Unity.Physics;

namespace SparFlame.Systems.General.BasicControl
{
    public partial class SaveSystemPlus : SystemBase
    {
        private BufferLookup<GarrisonEntity> _garrisonEntitiesLookup;
        private BufferLookup<GarrisonTypeData> _garrisonTypeDataLookup;
        private ComponentLookup<InGarrison> _inGarrisonLookup;
        private ComponentLookup<PhysicsMass> _physicsMassLookup;

        private bool _initialized ;

        protected override void OnCreate()
        {
            RequireForUpdate<SaveConfig>();
            RequireForUpdate<SaveData>();

            _garrisonEntitiesLookup = GetBufferLookup<GarrisonEntity>(true);
            _garrisonTypeDataLookup = GetBufferLookup<GarrisonTypeData>(true);
            _inGarrisonLookup = GetComponentLookup<InGarrison>(true);
            _physicsMassLookup = GetComponentLookup<PhysicsMass>(true);
        }

        protected override void OnStartRunning()
        {
            if (!_initialized)
            {
                GameController.Instance.EcsSaveSityData += SaveCity;
            }
        }

        protected override void OnUpdate()
        {
        }

        private void SaveCity(int cityId)
        {
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
                seEm.RemoveComponent<SceneTag>(seEm.UniversalQuery);
                seEm.RemoveComponent<SceneSection>(seEm.UniversalQuery);
                // Save
                using (var writer =
                       new StreamBinaryWriter(SaveUtilities.GetCitySavePath(cityId)))
                {
                    SerializeUtility.SerializeWorld(seEm, writer);
                }
            }

            SystemAPI.SetSingleton(new SaveData
            {
                CityId = -1,
                Type = SaveLoadType.None
            });
        }
    }
}