using SparFlame.GamePlaySystem.Building;
using SparFlame.GamePlaySystem.CustomParticleSystem;
using SparFlame.GamePlaySystem.Garrison;
using SparFlame.GamePlaySystem.General;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace SparFlame.GamePlaySystem.Interact
{
    // [UpdateBefore(typeof(StatSystem))]
    public partial struct GarrisonBuffSystem : ISystem
    {
        private ComponentLookup<BuildingGarrisonBuff> _garrisonBuffLookup;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<GarrisonBuffConfig>();
            state.RequireForUpdate<EndSimulationEntityCommandBufferSystem.Singleton>();
            state.RequireForUpdate<GarrisonAttr>();
            state.RequireForUpdate<SubGamingTag>();
            _garrisonBuffLookup = state.GetComponentLookup<BuildingGarrisonBuff>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            _garrisonBuffLookup.Update(ref state);
            var config = SystemAPI.GetSingleton<GarrisonBuffConfig>();

            new BuildingUnderDefenseJob
            {
                ECB = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>()
                    .CreateCommandBuffer(state.WorldUnmanaged).AsParallelWriter(),
                Config = config,
                GarrisonBuffLookup = _garrisonBuffLookup,
            }.ScheduleParallel();
        }

        [BurstCompile]
        [WithAll(typeof(GarrisonAttr))]
        public partial struct BuildingUnderDefenseJob : IJobEntity
        {
            [ReadOnly] public GarrisonBuffConfig Config;
            public EntityCommandBuffer.ParallelWriter ECB;
            [NativeDisableParallelForRestriction] public ComponentLookup<BuildingGarrisonBuff> GarrisonBuffLookup;

            private void Execute([ChunkIndexInQuery] int index, in LocalTransform transform,
                in SubGameplayGeneralAttr subGameplayGeneralAttr, in DynamicBuffer<GarrisonEntity> entities,
                in ExpData expData, in BuildingAttr buildingAttr,
                Entity selfEntity)
            {
                if (entities.Length >= Config.minCountToTriggerGarrisonBuff &&
                    GarrisonBuffLookup.HasComponent(selfEntity) &&
                    !GarrisonBuffLookup.IsComponentEnabled(selfEntity))
                {
                    GarrisonBuffLookup.SetComponentEnabled(selfEntity, true);
                    var vfxRequest = ECB.CreateEntity(index);
                    ECB.AddComponent(index, vfxRequest, new VFXRequest
                    {
                        Filter = new VFXSubFilter
                        {
                            Faction = subGameplayGeneralAttr.FactionTag,
                            FactionFilterEnable = true,
                            TierFilterEnable = true,
                            Tier = expData.CurTier
                        },
                        KeepDuration = float.MaxValue,
                        RequestType = VFXRequestType.Spawn,
                        SpawnPosition = transform.Position,
                        ParabolaTargetPosition = transform.Position,
                        StatChangeRequest = default,
                        VFXName = buildingAttr.SubTypeIndex == (int)FortificationType.Tower ? VFXName.GarrisonUnderDefense : VFXName.GarrisonUnderDefenseTier4,
                        VFXTrackTarget = selfEntity,
                    });
                }

                if (entities.Length < Config.minCountToTriggerGarrisonBuff &&
                    GarrisonBuffLookup.HasComponent(selfEntity) &&
                    GarrisonBuffLookup.IsComponentEnabled(selfEntity))
                {
                    GarrisonBuffLookup.SetComponentEnabled(selfEntity, false);
                    var vfxRequest = ECB.CreateEntity(index);
                    ECB.AddComponent(index, vfxRequest, new VFXRequest
                    {
                        Filter = new VFXSubFilter
                        {
                            Faction = subGameplayGeneralAttr.FactionTag,
                            FactionFilterEnable = true,
                            TierFilterEnable = true,
                            Tier = expData.CurTier
                        },
                        KeepDuration = float.MaxValue,
                        RequestType = VFXRequestType.Kill,
                        SpawnPosition = transform.Position,
                        ParabolaTargetPosition = transform.Position,
                        StatChangeRequest = default,
                        VFXName = VFXName.GarrisonUnderDefense ,
                        VFXTrackTarget = selfEntity,
                    });
                }
            }
        }
    }
}