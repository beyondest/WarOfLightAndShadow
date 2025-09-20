using SparFlame.Components.General;
using SparFlame.Components.MainGameplay;
using Unity.Entities;
using Unity.Burst;
using Unity.Collections;

// ReSharper disable Unity.Entities.SingletonMustBeRequested

namespace SparFlame.Systems.MainGameplay.EnemyAI
{
    public partial struct EnemyCheckFocusPlayerSystem : ISystem
    {
        private ComponentLookup<FocusOnPlayerTag> _focusOnPlayerTagLookup;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<EndSimulationEntityCommandBufferSystem.Singleton>();
            state.RequireForUpdate<EnemyCheckFocusPlayerConfig>();
            state.RequireForUpdate<PlayerFactionData>();
            state.RequireForUpdate<CheckFocusPlayerRequest>();
            state.RequireForUpdate<CityAIData>();
            _focusOnPlayerTagLookup = state.GetComponentLookup<FocusOnPlayerTag>(true);
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var ecb = new EntityCommandBuffer(Allocator.Temp);
            foreach (var (request, entity) in SystemAPI.Query<RefRO<CheckFocusPlayerRequest>>().WithEntityAccess())
            {
                ecb.DestroyEntity(entity);
                if (!SystemAPI.HasComponent<CityAIData>(request.ValueRO.EnemyCity)) continue;
                var count = SystemAPI.GetComponentRW<CityAIData>(request.ValueRO.EnemyCity);
                count.ValueRW.FightCountWithPlayer++;
            }

            ecb.Playback(state.EntityManager);
            ecb.Dispose();

            var ecb2 = new EntityCommandBuffer(Allocator.TempJob);
            var ecbP = ecb2.AsParallelWriter();
            var playerFactionData = SystemAPI.GetSingleton<PlayerFactionData>();
            var cityQuery = SystemAPI.QueryBuilder().WithAll<CityAttr>().WithAll<MainGameplayGeneralAttr>().Build();
            var cityIdToPlayerRelations = new NativeParallelHashMap<int, Relationship>(12, Allocator.TempJob);
            var cityIdToEntities = new NativeParallelHashMap<int, Entity>(12, Allocator.TempJob);
            var generalAttrs = cityQuery.ToComponentDataArray<MainGameplayGeneralAttr>(Allocator.Temp);
            var cityAttrs = cityQuery.ToComponentDataArray<CityAttr>(Allocator.Temp);
            var cityEntities = cityQuery.ToEntityArray(Allocator.Temp);
            for (var i = 0; i < generalAttrs.Length; i++)
            {
                var generalAttr = generalAttrs[i];
                var cityAttr = cityAttrs[i];
                var entity = cityEntities[i];

                var relationWithPlayer = FactionUtils.GetRelationship(playerFactionData.faction,
                    playerFactionData.subFaction, generalAttr.faction, generalAttr.subFaction);
                cityIdToPlayerRelations.Add(cityAttr.globalId, relationWithPlayer);
                cityIdToEntities.Add(cityAttr.globalId, entity);
            }
            _focusOnPlayerTagLookup.Update(ref state);

            var job = new EnemyCityCheckShouldFocusOnPlayerJob
            {
                Config = SystemAPI.GetSingleton<EnemyCheckFocusPlayerConfig>(),
                CityIdRelationships = cityIdToPlayerRelations,
                CityIdToEntities = cityIdToEntities,
                SupportFightCityId = SystemAPI.HasSingleton<SupportFightTag>()
                    ? SystemAPI.GetComponent<CityAttr>(SystemAPI.GetSingletonEntity<SupportFightTag>()).globalId
                    : -1,
                ECB = ecbP,
                DirectRoadPoints = SystemAPI.GetSingletonBuffer<DirectRoadPointData>(),
                FocusOnPlayerTagLookup = _focusOnPlayerTagLookup,
            }.ScheduleParallel(state.Dependency);

            job.Complete();

            ecb2.Playback(state.EntityManager);
            ecb2.Dispose();

            cityIdToPlayerRelations.Dispose();
            cityIdToEntities.Dispose();
            generalAttrs.Dispose();
            cityAttrs.Dispose();
        }

        [BurstCompile]
        public partial struct EnemyCityCheckShouldFocusOnPlayerJob : IJobEntity
        {
            public EntityCommandBuffer.ParallelWriter ECB;
            [ReadOnly] public NativeParallelHashMap<int, Relationship> CityIdRelationships;
            [ReadOnly] public NativeParallelHashMap<int, Entity> CityIdToEntities;
            [ReadOnly] public EnemyCheckFocusPlayerConfig Config;
            [ReadOnly] public int SupportFightCityId;
            [ReadOnly] public DynamicBuffer<DirectRoadPointData> DirectRoadPoints;
            [ReadOnly] public ComponentLookup<FocusOnPlayerTag> FocusOnPlayerTagLookup;

            private void Execute([ChunkIndexInQuery] int index,
                in DynamicBuffer<CheckCity> checkCities,
                ref DynamicBuffer<InvadeTarget> targets,
                in CityAttr cityAttr, Entity selfEntity,
                in CityAIData aiData)
            {
                targets.Clear();
                var isCountExceed = aiData.FightCountWithPlayer >= Config.countThresholdToFocusPlayer;

                var playerHaveCheckCity =
                    SupportFightCityId == -1 || FocusOnPlayerTagLookup.IsComponentEnabled(selfEntity)
                                             || isCountExceed;
                if (!playerHaveCheckCity)
                {
                    foreach (var checkCity in checkCities)
                    {
                        var relationWithPlayer = CityIdRelationships[checkCity.CityId];
                        if (relationWithPlayer is Relationship.Self or Relationship.Ally)
                        {
                            playerHaveCheckCity = true;
                            break;
                        }
                    }
                }

                if (playerHaveCheckCity)
                {
                    var playerCities = new NativeList<int>(Allocator.Temp);
                    EnemyAIUtils.FindReachablePlayerCities(DirectRoadPoints, cityAttr.globalId,
                        CityIdRelationships, playerCities);
                    foreach (var cityId in playerCities)
                    {
                        if (cityId == SupportFightCityId) continue; // Skip support fight city
                        var cityEntity = CityIdToEntities[cityId];
                        targets.Add(new InvadeTarget
                        {
                            City = cityEntity,
                            CityId = cityId
                        });
                    }

                    ECB.SetComponentEnabled<FocusOnPlayerTag>(index, selfEntity, true);
                }
                else
                {
                    targets.Add(new InvadeTarget
                    {
                        City = CityIdToEntities[SupportFightCityId],
                        CityId = SupportFightCityId
                    });
                    ECB.SetComponentEnabled<FocusOnPlayerTag>(index, selfEntity, false);
                }
            }
        }
    }
}