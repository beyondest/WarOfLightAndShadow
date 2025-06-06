using SparFlame.BootStrapper;
using SparFlame.GamePlaySystem.Building;
using SparFlame.GamePlaySystem.CustomParticleSystem;
using SparFlame.GamePlaySystem.Garrison;
using SparFlame.GamePlaySystem.General;
using SparFlame.GamePlaySystem.Movement;
using SparFlame.GamePlaySystem.Resource;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Transforms;

namespace SparFlame.GamePlaySystem.Interact
{
    public struct UpgradeRequest : IComponentData
    {
        public Entity FromEntity;
    }

    [BurstCompile]
    [UpdateBefore(typeof(TransformSystemGroup))]
    public partial struct UpgradeSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<GamingTag>();
            state.RequireForUpdate<UpgradeRequest>();
            state.RequireForUpdate<ExpSystemConfig>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var ecb = new EntityCommandBuffer(Allocator.Temp);
            foreach (var (req, entity) in SystemAPI.Query<RefRO<UpgradeRequest>>().WithEntityAccess())
            {
                ecb.DestroyEntity(entity);
                if(!SystemAPI.HasComponent<GeneralAttr>(req.ValueRO.FromEntity))continue;
                
                var expData = SystemAPI.GetComponent<ExpData>(req.ValueRO.FromEntity);
                var generalAttr = SystemAPI.GetComponent<GeneralAttr>(req.ValueRO.FromEntity);
                var upgradeEntity = ecb.Instantiate(expData.NextTierPrefab);
                ecb.AddComponent<GameplayEntityTag>(upgradeEntity);
                var trans = SystemAPI.GetComponent<LocalTransform>(req.ValueRO.FromEntity);
                ecb.SetComponent(upgradeEntity, trans);
                
                // If this unit is a garrisoned unit, the upgraded unit still needs to be garrisoned
                if (SystemAPI.HasComponent<InGarrison>(req.ValueRO.FromEntity))
                {
                    var inGarrison = SystemAPI.GetComponent<InGarrison>(req.ValueRO.FromEntity);
                    ecb.AddComponent(upgradeEntity, inGarrison);
                    if (SystemAPI.IsComponentEnabled<GarrisonStateTag>(req.ValueRO.FromEntity))
                    {
                        ecb.SetComponentEnabled<GarrisonStateTag>(req.ValueRO.FromEntity, true);
                        ecb.SetComponentEnabled<IdleStateTag>(req.ValueRO.FromEntity, false);
                    }
                }

                if (generalAttr.BaseTag == BaseTag.Buildings)
                {
                    ecb.AddComponent(upgradeEntity, new ConstructingData
                    {
                        LastTime = SystemAPI.GetComponent<BuildingAttr>(expData.NextTierPrefab).ConstructTime
                    });
                    if(SystemAPI.HasComponent<DwellingGeneratePopulationTag>(expData.NextTierPrefab))
                        ecb.SetComponentEnabled<DwellingGeneratePopulationTag>(upgradeEntity, false);
                    ecb.SetComponentEnabled<VolumeObstacleSpawnRequest>(upgradeEntity, false);
                }
                
                var vfx = ecb.CreateEntity();
                ecb.AddComponent<GameplayEntityTag>(vfx);
                ecb.AddComponent(vfx, new VFXRequest
                {
                    SpawnPosition = trans.Position,
                    VFXName = VFXName.Upgrade,
                    RequestType = VFXRequestType.Spawn,
                    KeepDuration = 0f,
                    VFXTrackTarget =upgradeEntity,
                    Filter = new VFXSubFilter
                    {
                        FactionFilterEnable = true,
                        Faction = generalAttr.FactionTag,
                        TierFilterEnable = false
                    },
                    StatChangeRequest = default,
                    ParabolaTargetPosition = default,
                });
                AudioUtils.PlayAudioClip(AudioName.Upgrade, trans.Position,ecb);
                
                
                var destroyOriginalRequest = ecb.CreateEntity();
                ecb.AddComponent<GameplayEntityTag>(destroyOriginalRequest);
                ecb.AddComponent(destroyOriginalRequest, new StatChangeRequest
                {
                    AbsAmount = 9999,
                    Interactee = req.ValueRO.FromEntity,
                    Interactor = Entity.Null,
                    Type = StatChangeType.SimpleCleanUsedAsUpgrade,
                    InteractorGeneralAttr = default
                });
            }

            ecb.Playback(state.EntityManager);
            ecb.Dispose();
        }
    }
}