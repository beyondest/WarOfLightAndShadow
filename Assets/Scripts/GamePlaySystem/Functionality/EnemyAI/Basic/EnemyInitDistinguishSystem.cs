using System;
using SparFlame.GamePlaySystem.Building;
using SparFlame.GamePlaySystem.Conjure;
using SparFlame.GamePlaySystem.Fow;
using SparFlame.GamePlaySystem.General;
using SparFlame.GamePlaySystem.Interact;
using SparFlame.GamePlaySystem.Resource;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using Random = Unity.Mathematics.Random;

namespace SparFlame.GamePlaySystem.EnemyAI
{
    [UpdateInGroup(typeof(InitializationSystemGroup))]
    [UpdateAfter(typeof(EnemyBuildingPackSpawnSystem))]
    public partial struct EnemyInitDistinguishSystem : ISystem
    {
        private ComponentLookup<BuildingAttr> _buildingAttrLookup;
        private BufferLookup<LinkedEntityGroup> _linkedEntityGroupLookup;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<GameTimeData>();
            state.RequireForUpdate<EndInitializationEntityCommandBufferSystem.Singleton>();
            state.RequireForUpdate<EnemyInitDistinguishConfig>();
            state.RequireForUpdate<GeneralRandom>();
            state.RequireForUpdate<PlayerFactionData>();
            state.RequireForUpdate<GamingTag>();
            _buildingAttrLookup = state.GetComponentLookup<BuildingAttr>(true);
            _linkedEntityGroupLookup = state.GetBufferLookup<LinkedEntityGroup>(true);
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var config = SystemAPI.GetSingleton<EnemyInitDistinguishConfig>();
            _buildingAttrLookup.Update(ref state);
            _linkedEntityGroupLookup.Update(ref state);
            var playerFaction = SystemAPI.GetSingleton<PlayerFactionData>().Value;
            var ecb = new EntityCommandBuffer(Allocator.TempJob);
            var ecbP = ecb.AsParallelWriter();
            var job = new DistinguishEnemyJob
            {
                ECB = ecbP,
                BuildingAttrLookup = _buildingAttrLookup,
                DistinguishConfig = config,
                EnemyFaction = ~playerFaction,
            }.ScheduleParallel(state.Dependency);
            
            job.Complete();
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
            [ReadOnly] public EnemyInitDistinguishConfig DistinguishConfig;
            [ReadOnly] public FactionTag EnemyFaction;

            private void Execute([ChunkIndexInQuery] int index, in GeneralAttr attr, ref FowAgentData fowAgent,
                in LocalTransform transform, Entity selfEntity)
            {
                // General distinguish
                if (attr.FactionTag != EnemyFaction)
                {
                    ECB.AddComponent<PlayerTag>(index, selfEntity);
                    ECB.AddComponent<ContributeSightTag>(index, selfEntity);
                    if (attr.BaseTag == BaseTag.Buildings)
                    {
                        var buildingAttr = BuildingAttrLookup[selfEntity];
                        if (buildingAttr.SubTypeIndex == (int)OrnamentType.Crystal)
                        {
                            ECB.AddBuffer<SurroundingData>(index, selfEntity);
                            ECB.AddComponent(index,selfEntity, new SurroundingValue
                            {
                                Value = 0f
                            });
                        }
                    }
                    fowAgent.IsInsight = true;
                    return;
                }

                ECB.AddComponent<AITag>(index, selfEntity);
                fowAgent.IsInsight = false;
                ECB.AddComponent<DisappearInFowTag>(index, selfEntity);
                ECB.AddComponent(index, selfEntity, new HideFowAgentRequest
                {
                    Hide = true
                });

                // Detail distinguishes
                switch (attr.BaseTag)
                {
                    case BaseTag.Buildings:
                    {
                        var buildingAttr = BuildingAttrLookup[selfEntity];
                        if (buildingAttr is { Type: BuildingType.ConjuringShrines })
                        {
                            // ECB.AddComponent<EnemyConjureShrineData>(index, selfEntity);
                        }

                        if (buildingAttr is { Type: BuildingType.Ornaments, SubTypeIndex: (int)OrnamentType.Crystal })
                        {
                            ECB.AddComponent<EnemyBaseBelongsTo>(index, selfEntity);

                            ECB.AddBuffer<EnemyBaseTeamGeneralData>(index,
                                selfEntity); // This buffer must be initialized here, and never change length whole game
                            ECB.AddBuffer<EnemyBaseTeamAvailableData>(index,
                                selfEntity); // This buffer is initialized by other systems, and may change length
                            for (var i = 0; i < DistinguishConfig.aiTeamTypeCount; i++)
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
                                CrystalPos = new float3(transform.Position.x, 0f, transform.Position.z),
                                IsDestroyed = false
                            });
                            ECB.AddComponent<GameplayEntityTag>(index, selfEntity);
                            
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