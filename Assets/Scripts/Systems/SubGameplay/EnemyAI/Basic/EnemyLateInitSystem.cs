using SparFlame.Components.General;
using SparFlame.Components.SubGameplay;
using SparFlame.Core.Utils;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace SparFlame.Systems.SubGameplay.EnemyAI
{
    [UpdateInGroup(typeof(InitializationSystemGroup))]
    [UpdateAfter(typeof(SubGameplayInitDistinguishSystem))]
    public partial struct EnemyLateInitSystem : ISystem
    {
        private ComponentLookup<BuildingAttr> _buildingAttrLookup;
        private ComponentLookup<SubGameplayGeneralAttr> _generalAttrLookup;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<GameWaveData>();
            state.RequireForUpdate<PlayerFactionData>();
            state.RequireForUpdate<GameStatusData>();
            _buildingAttrLookup = state.GetComponentLookup<BuildingAttr>(true);
            _generalAttrLookup = state.GetComponentLookup<SubGameplayGeneralAttr>(true);
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var gameStatus = SystemAPI.GetSingleton<GameStatusData>().Value;
            if (gameStatus == GameStatus.Init)
            {
                // SystemAPI.SetSingleton(new EnemyCrystalInfo
                // {
                //     CurTotalHp = 1,
                //     MaxTotalHp = 1
                // });
                return;
            }

            if (gameStatus != GameStatus.SubGaming) return;
            _buildingAttrLookup.Update(ref state);
            _generalAttrLookup.Update(ref state);
            var ecb = new EntityCommandBuffer(Allocator.TempJob);
            var playerFactionData = SystemAPI.GetSingleton<PlayerFactionData>();
            var ecbP = ecb.AsParallelWriter();
            var job = new InitCrystalPackDataJob
            {
                ECB = ecbP,
                ElapsedTime = (float)SystemAPI.Time.ElapsedTime,
                SeedBias = SystemAPI.GetSingletonRW<GeneralRandom>().ValueRW.Rnd.NextInt(),
                BuildingAttrLookup = _buildingAttrLookup,
                PlayerFactionData = playerFactionData,
                GeneralAttrLookup = _generalAttrLookup,
            }.ScheduleParallel(state.Dependency);
            state.Dependency = job;
            job.Complete();
            AddMonitorToNoMonitorPlayerCrystal(ref state, playerFactionData, ecb);
            ecb.Playback(state.EntityManager);
            ecb.Dispose();
        }

        private void AddMonitorToNoMonitorPlayerCrystal(ref SystemState state, in PlayerFactionData playerFactionData,
            EntityCommandBuffer ecb)
        {
            foreach (var (generalAttr, entity) in SystemAPI.Query<RefRO<SubGameplayGeneralAttr>>()
                         .WithAll<CrystalDef>()
                         .WithNone<UnderMonitorTag>()
                         .WithAll<PlayerTag>().WithEntityAccess())
            {
                var relationship = FactionUtils.GetRelationship(playerFactionData, generalAttr.ValueRO.Faction,
                    generalAttr.ValueRO.SubFaction);
                // Only monitor self and ally crystal for enemy AI. Ally AI don't need that.
                if (relationship != Relationship.Player && relationship != Relationship.Ally) continue;
                var generateMonitorRequest = ecb.CreateEntity();
                ecb.AddComponent<SubGameplayEntityTag>(generateMonitorRequest);
                ecb.AddComponent(generateMonitorRequest, new GenerateMonitorRequest
                {
                    TargetToMonitor = entity
                });
            }
        }

        /*private void DealDeadEnemyBaseRequest(ref SystemState state, EntityCommandBuffer ecb, in EnemyInitDistinguishConfig config)
        {
            foreach (var (request, entity) in SystemAPI.Query<RefRO<DestroyEnemyBaseRequest>>().WithEntityAccess())
            {
                ecb.DestroyEntity(entity);
                var buildingPack = SystemAPI.GetComponent<EnemyBaseBelongsTo>(request.ValueRO.Base);
                var linkedEntityGroup = SystemAPI.GetBuffer<LinkedEntityGroup>(buildingPack.BuildingPack);
                if(linkedEntityGroup.Length == 1)continue;
                for (int i = 1; i < linkedEntityGroup.Length; i++)
                {
                    var baseLevel = SystemAPI.GetBuffer<LinkedEntityGroup>(linkedEntityGroup[i].Value);
                    if(baseLevel[config.baseIndex].Value !=request.ValueRO.Base )continue;
                    UnNormalKillEnemyBuilding
                }
            }
        } */


        [BurstCompile]
        [WithAll(typeof(CrystalPackNeedInitTag))]
        private partial struct InitCrystalPackDataJob : IJobEntity
        {
            [ReadOnly] public float ElapsedTime;
            [ReadOnly] public int SeedBias;
            [ReadOnly] public PlayerFactionData PlayerFactionData;
            public EntityCommandBuffer.ParallelWriter ECB;
            [ReadOnly] public ComponentLookup<BuildingAttr> BuildingAttrLookup;
            [ReadOnly] public ComponentLookup<SubGameplayGeneralAttr> GeneralAttrLookup;

            private void Execute([EntityIndexInQuery] int index, Entity selfEntity,
                in DynamicBuffer<LinkedEntityGroup> children)
            {
                ECB.RemoveComponent<CrystalPackNeedInitTag>(index, selfEntity);
                var baseEntity = Entity.Null;
                for (var i = 1; i < children.Length; i++)
                {
                    var group = children[i];
                    var child = group.Value;

                    if (BuildingAttrLookup.TryGetComponent(child, out var buildingAttr))
                    {
                        if (!GeneralAttrLookup.TryGetComponent(child, out var generalAttr)) return;
                        var relationship = FactionUtils.GetRelationship(PlayerFactionData, generalAttr.Faction,
                            generalAttr.SubFaction);
                        // Player's crystal pack don't need AI Logic
                        if (relationship == Relationship.Player) return;
                        if (buildingAttr is { Type: BuildingType.Ornaments, SubTypeIndex: (int)OrnamentType.Crystal }
                            or { Type: BuildingType.Ornaments, SubTypeIndex: (int)OrnamentType.Beacon })
                        {
                            baseEntity = child;
                            ECB.AddBuffer<AIBaseGarrisonTowerData>(index, baseEntity);
                        }

                        if (buildingAttr is { Type: BuildingType.ConjuringShrines })
                        {
                            ECB.AddComponent<AIConjureShrineData>(index, child);
                            ECB.SetComponent(index, child, new AIConjureShrineData
                            {
                                Base = baseEntity,
                                ConjureTime = 0f,
                                Rnd = new Random(MathUtils.GetSeedByIndexTimeBias(index, SeedBias, ElapsedTime)),
                            });
                        }

                        if (buildingAttr is
                            { Type: BuildingType.Fortifications, SubTypeIndex: (int)FortificationType.Tower }
                            or { Type: BuildingType.Fortifications, SubTypeIndex: (int)FortificationType.BigTower })
                        {
                            ECB.AppendToBuffer(index, baseEntity, new AIBaseGarrisonTowerData
                            {
                                Tower = child,
                                AvailableCount = 1
                            });
                        }
                    }
                }
            }
        }
    }
}