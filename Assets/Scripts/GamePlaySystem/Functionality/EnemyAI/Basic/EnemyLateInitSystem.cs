using SparFlame.GamePlaySystem.Building;
using SparFlame.GamePlaySystem.Conjure;
using SparFlame.GamePlaySystem.General;
using SparFlame.GamePlaySystem.Waves;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace SparFlame.GamePlaySystem.EnemyAI
{
    [UpdateInGroup(typeof(InitializationSystemGroup))]
    [UpdateAfter(typeof(EnemyInitDistinguishSystem))]
    public partial struct EnemyLateInitSystem : ISystem
    {
        private ComponentLookup<BuildingAttr> _buildingAttrLookup;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<GameWaveData>();
            state.RequireForUpdate<PlayerFactionData>();
            state.RequireForUpdate<GameStatusData>();
            _buildingAttrLookup = state.GetComponentLookup<BuildingAttr>(true);
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var gameStatus = SystemAPI.GetSingleton<GameStatusData>().Value;
            if (gameStatus == GameStatus.Init)
            {
                SystemAPI.SetSingleton(new EnemyCrystalInfo
                {
                    TotalCount = 0,
                    InSightValidCount = 0
                });
                return;
            }
            if(gameStatus != GameStatus.Gaming)return;
            _buildingAttrLookup.Update(ref state);
            var ecb = new EntityCommandBuffer(Allocator.TempJob);
            var playerFaction = SystemAPI.GetSingleton<PlayerFactionData>().Value;
            var ecbP = ecb.AsParallelWriter();
            var job = new InitCrystalPackDataJob
            {
                ECB = ecbP,
                ElapsedTime = (float)SystemAPI.Time.ElapsedTime,
                SeedBias = SystemAPI.GetSingletonRW<GeneralRandom>().ValueRW.Rnd.NextInt(),
                BuildingAttrLookup = _buildingAttrLookup,
            }.ScheduleParallel(state.Dependency);
            state.Dependency = job;
            job.Complete();
            AddMonitorToNoMonitorPlayerCrystal(ref state, playerFaction, ecb);
            ecb.Playback(state.EntityManager);
            ecb.Dispose();
        }

   

        private void AddMonitorToNoMonitorPlayerCrystal(ref SystemState state, FactionTag playerFaction,
            EntityCommandBuffer ecb)
        {
            foreach (var (coreCrystal, entity) in SystemAPI.Query<RefRO<CrystalDef>>().WithNone<UnderMonitorTag>()
                         .WithAll<PlayerTag>().WithEntityAccess())
            {
                if (coreCrystal.ValueRO.Faction != playerFaction) continue;
                var generateMonitorRequest = ecb.CreateEntity();
                ecb.AddComponent<GameplayEntityTag>(generateMonitorRequest);
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
            public EntityCommandBuffer.ParallelWriter ECB;
            [ReadOnly] public ComponentLookup<BuildingAttr> BuildingAttrLookup;

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
                        if (buildingAttr is { Type: BuildingType.Ornaments, SubTypeIndex: (int)OrnamentType.Crystal }
                            or {Type: BuildingType.Ornaments, SubTypeIndex: (int)OrnamentType.Beacon})
                        {
                            baseEntity = child;
                            ECB.AddBuffer<EnemyBaseGarrisonTowerData>(index, baseEntity);
                        }

                        if (buildingAttr is { Type: BuildingType.ConjuringShrines })
                        {
                            ECB.AddComponent<EnemyConjureShrineData>(index, child);
                            ECB.SetComponent(index, child, new EnemyConjureShrineData
                            {
                                Base = baseEntity,
                                ConjureTime = 0f,
                                Rnd = new Random(GeneralUtils.GetSeedByIndexTimeBias(index, SeedBias, ElapsedTime)),
                            });
                        }

                        if (buildingAttr is
                            { Type: BuildingType.Fortifications, SubTypeIndex: (int)FortificationType.Tower })
                        {
                            ECB.AppendToBuffer(index, baseEntity, new EnemyBaseGarrisonTowerData
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