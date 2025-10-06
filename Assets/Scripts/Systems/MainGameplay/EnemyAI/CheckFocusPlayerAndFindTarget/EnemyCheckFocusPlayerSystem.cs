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
        private ComponentLookup<GlobalSingleId> _singleIdLookup;
        private EntityQuery _assignIdQuery;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<EndSimulationEntityCommandBufferSystem.Singleton>();
            state.RequireForUpdate<EnemyCheckFocusPlayerConfig>();
            state.RequireForUpdate<PlayerFactionData>();
            state.RequireForUpdate<CheckFocusPlayerRequest>();
            state.RequireForUpdate<CityAIData>();
            state.RequireForUpdate<GameStatusData>();
            _singleIdLookup = state.GetComponentLookup<GlobalSingleId>(true);
            _assignIdQuery = SystemAPI.QueryBuilder().WithAll<AssignGlobalSingleIDRequest>().Build();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var gameStatusData = SystemAPI.GetSingleton<GameStatusData>().Value;
            if (gameStatusData != GameStatus.MainGaming && gameStatusData != GameStatus.SubGaming) return;
            if(!_assignIdQuery.IsEmpty)return;
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

            var playerFactionData = SystemAPI.GetSingleton<PlayerFactionData>();
            var cityQuery = SystemAPI.QueryBuilder().WithAll<CityAttr>().WithAll<PrefabId>()
                .WithAll<MainGameplayGeneralAttr>().Build();
            var cityIdToPlayerRelations = new NativeParallelHashMap<int, Relationship>(12, Allocator.TempJob);
            var cityIdToEntities = new NativeParallelHashMap<int, Entity>(12, Allocator.TempJob);
            var generalAttrs = cityQuery.ToComponentDataArray<MainGameplayGeneralAttr>(Allocator.TempJob);
            var cityAttrs = cityQuery.ToComponentDataArray<PrefabId>(Allocator.TempJob);
            var cityEntities = cityQuery.ToEntityArray(Allocator.TempJob);
            for (var i = 0; i < generalAttrs.Length; i++)
            {
                var generalAttr = generalAttrs[i];
                var cityAttr = cityAttrs[i];
                var entity = cityEntities[i];

                var relationWithPlayer = FactionUtils.GetRelationship(playerFactionData.faction,
                    playerFactionData.subFaction, generalAttr.faction, generalAttr.subFaction);
                cityIdToPlayerRelations.Add(cityAttr.value, relationWithPlayer);
                cityIdToEntities.Add(cityAttr.value, entity);
            }

            _singleIdLookup.Update(ref state);
            state.Dependency = new EnemyCityCheckShouldFocusOnPlayerJob
            {
                ECB = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>().CreateCommandBuffer(state.WorldUnmanaged).AsParallelWriter(),
                Config = SystemAPI.GetSingleton<EnemyCheckFocusPlayerConfig>(),
                CityIdRelationships = cityIdToPlayerRelations,
                CityIdToEntities = cityIdToEntities,
                SupportFightCityId = SystemAPI.HasSingleton<SupportFightTag>()
                    ? SystemAPI.GetComponent<PrefabId>(SystemAPI.GetSingletonEntity<SupportFightTag>()).value
                    : -1,
                DirectRoadPoints = SystemAPI.GetSingletonBuffer<DirectRoadPointData>(),
                SingleIdLookup = _singleIdLookup,
            }.ScheduleParallel(state.Dependency);

            cityIdToPlayerRelations.Dispose(state.Dependency);
            cityIdToEntities.Dispose(state.Dependency);
            generalAttrs.Dispose(state.Dependency);
            cityAttrs.Dispose(state.Dependency);
            cityEntities.Dispose(state.Dependency);
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
            [ReadOnly] public ComponentLookup<GlobalSingleId> SingleIdLookup;

            private void Execute([ChunkIndexInQuery] int index,
                in DynamicBuffer<CheckCity> checkCities,
                ref DynamicBuffer<InvadeTarget> targets,
                in PrefabId prefabId,
                ref CityAIData cityAIData)
            {
                targets.Clear();
                var isCountExceed = cityAIData.FightCountWithPlayer >= Config.countThresholdToFocusPlayer;

                var playerHaveCheckCity =
                    SupportFightCityId == -1 || cityAIData.IsFocusOnPlayer
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
                    EnemyAIUtils.FindReachablePlayerCities(DirectRoadPoints, prefabId.value,
                        CityIdRelationships, playerCities);
                    foreach (var cityId in playerCities)
                    {
                        if (cityId == SupportFightCityId) continue; // Skip support fight city
                        var cityEntity = CityIdToEntities[cityId];
                        targets.Add(new InvadeTarget
                        {
                            City = cityEntity,
                            CityPrefabId = cityId,
                            SingleId = SingleIdLookup[cityEntity].value
                        });
                    }

                    if (!cityAIData.IsFocusOnPlayer)
                    {
                        var hintRequest = ECB.CreateEntity(index);
                        ECB.AddComponent<MainGameplayEntityTag>(index,hintRequest);
                        ECB.AddComponent(index, hintRequest, new HintRequest
                        {
                            Name = HintName.EnemyBeginFocusOnPlayer
                        });
                    }
                    cityAIData.IsFocusOnPlayer = true;
                }
                else
                {
                    var supportCity = CityIdToEntities[SupportFightCityId];
                    targets.Add(new InvadeTarget
                    {
                        City = supportCity,
                        CityPrefabId = SupportFightCityId,
                        SingleId = SingleIdLookup[supportCity].value
                    });
                    if (cityAIData.IsFocusOnPlayer)
                    {
                        var hintRequest = ECB.CreateEntity(index);
                        ECB.AddComponent<MainGameplayEntityTag>(index,hintRequest);
                        ECB.AddComponent(index, hintRequest, new HintRequest
                        {
                            Name = HintName.EnemyStopFocusOnPlayer
                        });
                    }
                    cityAIData.IsFocusOnPlayer = false;
                }
            }
        }
    }
}