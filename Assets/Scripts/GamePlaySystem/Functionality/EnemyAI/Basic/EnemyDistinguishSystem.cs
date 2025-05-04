using System;
using SparFlame.BootStrapper;
using SparFlame.GamePlaySystem.Building;
using SparFlame.GamePlaySystem.General;
using SparFlame.GamePlaySystem.Resource;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Transforms;
using Random = Unity.Mathematics.Random;

namespace SparFlame.GamePlaySystem.EnemyAI
{
    [UpdateInGroup(typeof(InitializationSystemGroup))]
    [UpdateAfter(typeof(EnemyBuildingPackSpawnSystem))]
    [UpdateBefore(typeof(OccupiedTagManageSystem))]
    public partial struct EnemyDistinguishSystem : ISystem
    {
        private ComponentLookup<BuildingAttr> _buildingAttrLookup;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<EndInitializationEntityCommandBufferSystem.Singleton>();
            state.RequireForUpdate<EnemyInitConfig>();
            state.RequireForUpdate<GeneralRandom>();
            state.RequireForUpdate<PlayerFactionData>();
            state.RequireForUpdate<GamingTag>();
            _buildingAttrLookup = state.GetComponentLookup<BuildingAttr>(true);
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var config = SystemAPI.GetSingleton<EnemyInitConfig>();
            var ecbSingleton = SystemAPI.GetSingleton<EndInitializationEntityCommandBufferSystem.Singleton>();
            _buildingAttrLookup.Update(ref state);
            var playerFaction = SystemAPI.GetSingleton<PlayerFactionData>().Value;
            new DistinguishEnemyJob
            {
                ECB = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged).AsParallelWriter(),
                BuildingAttrLookup = _buildingAttrLookup,
                Config = config,
                ElapsedTime = (float)SystemAPI.Time.ElapsedTime,
                SeedBias = SystemAPI.GetSingletonRW<GeneralRandom>().ValueRW.Rnd.NextInt(),
                EnemyFaction = ~playerFaction,
            }.ScheduleParallel();
            AddMonitorToNoMonitorPlayerCrystal(ref state, playerFaction);
        }

        private void AddMonitorToNoMonitorPlayerCrystal(ref SystemState state, FactionTag playerFaction)
        {
            var ecb = new EntityCommandBuffer(Allocator.Temp);
            foreach (var (coreCrystal, entity) in SystemAPI.Query<RefRO<CoreCrystalTag>>().WithNone<UnderMonitorTag>()
                         .WithNone<AITag>().WithEntityAccess())
            {
                if (coreCrystal.ValueRO.Faction != playerFaction) continue;
                var generateMonitorRequest = ecb.CreateEntity();
                ecb.AddComponent(generateMonitorRequest, new GenerateMonitorRequest
                {
                    TargetToMonitor = entity
                });
            }
            ecb.Playback(state.EntityManager);
            ecb.Dispose();
        }


        [BurstCompile]
        [WithNone(typeof(AITag))]
        [WithNone(typeof(PlayerTag))]
        [WithNone(typeof(ResourceAttr))]
        public partial struct DistinguishEnemyJob : IJobEntity
        {
            public EntityCommandBuffer.ParallelWriter ECB;
            [ReadOnly] public ComponentLookup<BuildingAttr> BuildingAttrLookup;
            [ReadOnly] public int SeedBias;
            [ReadOnly] public float ElapsedTime;
            [ReadOnly] public EnemyInitConfig Config;
            [ReadOnly] public FactionTag EnemyFaction;

            private void Execute([ChunkIndexInQuery] int index, in GeneralAttr attr,
                in LocalTransform transform, Entity selfEntity)
            {
                if (attr.FactionTag != EnemyFaction)
                {
                    ECB.AddComponent<PlayerTag>(index,selfEntity);
                    return;
                };
                ECB.AddComponent<AITag>(index, selfEntity);
                switch (attr.BaseTag)
                {
                    case BaseTag.Buildings:
                    {
                        var buildingAttr = BuildingAttrLookup[selfEntity];
                        if (buildingAttr is { Type: BuildingType.ConjuringShrines })
                        {
                            var seed = GeneralUtils.GetSeedByIndexTimeBias(selfEntity.Index, SeedBias, ElapsedTime);
                            ECB.AddComponent(index, selfEntity, new EnemyConjureShrineData
                            {
                                Rnd = new Random(seed)
                            });
                        }

                        if (buildingAttr is { Type: BuildingType.Ornaments, SubTypeIndex: (int)OrnamentType.Crystal })
                        {
                            ECB.AddBuffer<EnemyBaseTeamGeneralData>(index,
                                selfEntity); // This buffer must be initialized here, and never change length whole game
                            ECB.AddBuffer<EnemyBaseTeamAvailableData>(index,
                                selfEntity); // This buffer is initialized by other systems, and may change length
                            for (var i = 0; i < Config.AITeamTypeCount; i++)
                            {
                                ECB.AppendToBuffer(index, selfEntity, new EnemyBaseTeamGeneralData
                                {
                                    TeamType = (AITeamType)i,
                                    CurCount = 0
                                });
                            }
                            var request = ECB.CreateEntity(index);
                            ECB.AddComponent(index, request, new ChangeOccupiedTagRequest
                            {
                                CrystalFaction = EnemyFaction,
                                CrystalPos = transform.Position,
                                IsDestroyed = false
                            });
                        }

                        break;
                    }
                    case BaseTag.Units:
                        ECB.AddComponent<EnemyUnitCommandData>(index, selfEntity);
                        ECB.AddComponent<EnemyUnitCommandUpdate>(index, selfEntity);
                        ECB.SetComponentEnabled<EnemyUnitCommandUpdate>(index, selfEntity, false);
                        break;
                    case BaseTag.Resources:
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
        }
    }
}