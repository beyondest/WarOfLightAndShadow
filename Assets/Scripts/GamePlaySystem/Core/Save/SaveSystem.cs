using System;
using SparFlame.BootStrapper;
using SparFlame.Database;
using SparFlame.GamePlaySystem.Garrison;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Entities.Serialization;
using Unity.Physics;

namespace SparFlame.GamePlaySystem.Save
{
  
    [UpdateAfter(typeof(EndSimulationEntityCommandBufferSystem))]
    public partial struct SaveSystem : ISystem
    {
        private BufferLookup<GarrisonEntity> _garrisonEntitiesLookup;
        private BufferLookup<GarrisonTypeData> _garrisonTypeDataLookup;
        private ComponentLookup<InGarrison> _inGarrisonLookup;
        private ComponentLookup<PhysicsMass> _physicsMassLookup;
        
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<SaveConfig>();
            state.RequireForUpdate<SaveData>();

            _garrisonEntitiesLookup = state.GetBufferLookup<GarrisonEntity>(true);
            _garrisonTypeDataLookup = state.GetBufferLookup<GarrisonTypeData>(true);
            _inGarrisonLookup = state.GetComponentLookup<InGarrison>(true);
            _physicsMassLookup = state.GetComponentLookup<PhysicsMass>(true);
            
            
            
        }

        public void OnUpdate(ref SystemState state)
        {
            var saveRequestData = SystemAPI.GetSingleton<SaveData>();
            var config = SystemAPI.GetSingleton<SaveConfig>();
            _garrisonEntitiesLookup.Update(ref state);
            _garrisonTypeDataLookup.Update(ref state);
            _inGarrisonLookup.Update(ref state);
            _physicsMassLookup.Update(ref state);
            switch (saveRequestData.Type)
            {
                case SaveLoadType.None:
                    break;
                case SaveLoadType.OnlyCity:
                    var ecb = new EntityCommandBuffer(Allocator.TempJob);
                    var ecbP = ecb.AsParallelWriter();
                    var saveJob = new SaveSubGameplayJob
                    {
                        ECB = ecbP,
                        GarrisonEntitiesLookup = _garrisonEntitiesLookup,
                        GarrisonTypeDataLookup = _garrisonTypeDataLookup,
                        InGarrisonLookup = _inGarrisonLookup,
                        PhysicsMassLookup = _physicsMassLookup,

                    }.ScheduleParallel(state.Dependency);
                    saveJob.Complete();
                    using (var serializeWorld = new World("Serialization World"))
                    {
                        EntityManager seEm = serializeWorld.EntityManager;
                        ecb.Playback(seEm);
                        ecb.Dispose();
                        seEm.RemoveComponent<SceneTag>(seEm.UniversalQuery);
                        seEm.RemoveComponent<SceneSection>(seEm.UniversalQuery);
                        // Save
                        using (var writer = new StreamBinaryWriter(SaveUtilities.GetCitySavePath(saveRequestData.CityId,config)))
                        {
                            SerializeUtility.SerializeWorld(seEm, writer);
                        }
                    }
                    
                    SystemAPI.SetSingleton(new SaveData
                    {
                        CityId = -1,
                        Type = SaveLoadType.None
                    });
                    
                    break;
                case SaveLoadType.OnlyMainGameplay:
                    break;
               
                case SaveLoadType.AllFromCity:
                    break;
                case SaveLoadType.AllFromWildBattle:
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

      
    }
}