using SparFlame.Components.General;
using SparFlame.Components.SubGameplay;
using SparFlame.Components.VFX;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Transforms;

namespace SparFlame.Systems.SubGameplay.Interact
{
    public partial struct UnitGarrisonBuffSystem : ISystem
    {
        private ComponentLookup<UnitGarrisonBuff> _garrisonBuffLookup;
        private ComponentLookup<InGarrison> _inGarrisonLookup;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<GarrisonBuffConfig>();
            state.RequireForUpdate<EndSimulationEntityCommandBufferSystem.Singleton>();
            state.RequireForUpdate<SubGamingTag>();
            _garrisonBuffLookup = state.GetComponentLookup<UnitGarrisonBuff>();
            _inGarrisonLookup = state.GetComponentLookup<InGarrison>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            _garrisonBuffLookup.Update(ref state);
            _inGarrisonLookup.Update(ref state);

            new UnitGarrisonBuffJob
            {
                ECB = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>()
                    .CreateCommandBuffer(state.WorldUnmanaged).AsParallelWriter(),
                GarrisonBuffLookup = _garrisonBuffLookup,
                InGarrisonLookup = _inGarrisonLookup,
            }.ScheduleParallel();
        }

        [BurstCompile]
        [WithAll(typeof(UnitAttr))]
        [WithNone(typeof(DualSpearTag))]
        public partial struct UnitGarrisonBuffJob : IJobEntity
        {
            public EntityCommandBuffer.ParallelWriter ECB;
            [NativeDisableParallelForRestriction] public ComponentLookup<UnitGarrisonBuff> GarrisonBuffLookup;
            [ReadOnly] public ComponentLookup<InGarrison> InGarrisonLookup;
            
            private void Execute([ChunkIndexInQuery] int index, in LocalTransform transform,
                in SubGameplayGeneralAttr subGameplayGeneralAttr, in ExpData expData,
                Entity selfEntity)
            {
                if (GarrisonBuffLookup.HasComponent(selfEntity)&&
                    InGarrisonLookup.HasComponent(selfEntity)
                    && !GarrisonBuffLookup.IsComponentEnabled(selfEntity))
                {
                    GarrisonBuffLookup.SetComponentEnabled(selfEntity, true);
                    var vfxRequest = ECB.CreateEntity(index);
                    ECB.AddComponent<SubGameplayEntityTag>(index, vfxRequest);
                    ECB.AddComponent(index, vfxRequest, new VFXRequest
                    {
                        RequestType = VFXRequestType.Spawn,
                        VFXName = VFXName.UnitGarrisonBuff,
                        VFXTrackTarget = selfEntity,
                        KeepDuration = float.MaxValue,
                        Filter = new VFXSubFilter
                        {
                            Faction = subGameplayGeneralAttr.Faction,
                            FactionFilterEnable =  true
                        },
                        SpawnPosition = transform.Position,
                    });
                }

                if (!InGarrisonLookup.HasComponent(selfEntity) &&
                    GarrisonBuffLookup.HasComponent(selfEntity) &&
                    GarrisonBuffLookup.IsComponentEnabled(selfEntity))
                {
                    GarrisonBuffLookup.SetComponentEnabled(selfEntity, false);
                    var vfxRequest = ECB.CreateEntity(index);
                    ECB.AddComponent<SubGameplayEntityTag>(index, vfxRequest);
                    ECB.AddComponent(index, vfxRequest, new VFXRequest
                    {
                        RequestType = VFXRequestType.Kill,
                        VFXName = VFXName.UnitGarrisonBuff,
                        VFXTrackTarget = selfEntity,
                    });
                }
            }
        }
    }
}