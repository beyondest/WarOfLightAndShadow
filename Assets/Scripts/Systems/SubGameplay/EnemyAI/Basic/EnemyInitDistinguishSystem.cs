using System;
using SparFlame.Components.General;
using SparFlame.Components.SubGameplay;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Transforms;

namespace SparFlame.Systems.SubGameplay.EnemyAI
{
    // TODO : Move this system to a more appropriate place
    [UpdateInGroup(typeof(InitializationSystemGroup))]
    public partial struct EnemyInitDistinguishSystem : ISystem
    {
        private ComponentLookup<BuildingAttr> _buildingAttrLookup;
        private BufferLookup<LinkedEntityGroup> _linkedEntityGroupLookup;
        private ComponentLookup<UnitAttr> _unitAttrLookup;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<GameTimeData>();
            state.RequireForUpdate<EndInitializationEntityCommandBufferSystem.Singleton>();
            state.RequireForUpdate<EnemyInitDistinguishConfig>();
            state.RequireForUpdate<GeneralRandom>();
            state.RequireForUpdate<PlayerFactionData>();
            state.RequireForUpdate<SubGamingTag>();
            _buildingAttrLookup = state.GetComponentLookup<BuildingAttr>(true);
            _linkedEntityGroupLookup = state.GetBufferLookup<LinkedEntityGroup>(true);
            _unitAttrLookup = state.GetComponentLookup<UnitAttr>(true);
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var config = SystemAPI.GetSingleton<EnemyInitDistinguishConfig>();
            _buildingAttrLookup.Update(ref state);
            _linkedEntityGroupLookup.Update(ref state);
            _unitAttrLookup.Update(ref state);
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

            private void Execute([ChunkIndexInQuery] int index, in SubGameplayGeneralAttr attr,
                in LocalTransform transform, Entity selfEntity)
            {
                // General distinguish
                if (attr.FactionTag != EnemyFaction)
                {
                    ECB.AddComponent<PlayerTag>(index, selfEntity);
                    if (attr.BaseTag == BaseTag.Buildings)
                    {
                        var buildingAttr = BuildingAttrLookup[selfEntity];
                        // Only crystal and beacon can contribute to sight
                        if (buildingAttr is { Type: BuildingType.Ornaments, SubTypeIndex: (int)OrnamentType.Crystal } or
                            { Type: BuildingType.Ornaments, SubTypeIndex: (int)OrnamentType.Beacon })
                        {
                            ECB.AddBuffer<SurroundingData>(index, selfEntity);
                            ECB.AddComponent(index, selfEntity, new SurroundingValue
                            {
                                Value = 0f
                            });
                        }
                    }
                    return;
                }

                ECB.AddComponent<AITag>(index, selfEntity);
               

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

                        if (buildingAttr is { Type: BuildingType.Ornaments, SubTypeIndex: (int)OrnamentType.Crystal }
                            or { Type: BuildingType.Ornaments, SubTypeIndex: (int)OrnamentType.Beacon })
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