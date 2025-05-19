using System;
using SparFlame.GamePlaySystem.Building;
using SparFlame.GamePlaySystem.Conjure;
using SparFlame.GamePlaySystem.Fow;
using SparFlame.GamePlaySystem.General;
using SparFlame.GamePlaySystem.Interact;
using SparFlame.GamePlaySystem.Resource;
using SparFlame.GamePlaySystem.Units;
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
        private ComponentLookup<UnitAttr> _unitAttrLookup;

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
                UnitAttrLookup = _unitAttrLookup
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
            [ReadOnly] public ComponentLookup<UnitAttr> UnitAttrLookup;
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
                            // Only light crystal and beacon can contribute to sight, no matter what player faction is
                            if (attr.FactionTag == FactionTag.Ally)
                            {
                                ECB.AddComponent<ContributeSightTag>(index, selfEntity);
                                ECB.SetComponentEnabled<ContributeSightTag>(index, selfEntity,true);
                            }
                        }
                    }
                    else
                    {
                        var unitAttr = UnitAttrLookup[selfEntity];
                        if ( attr.FactionTag == FactionTag.Ally)
                        {
                            ECB.AddComponent<ContributeSightTag>(index, selfEntity);
                            // Only light cavalry can contribute to light, other light unit may have light by cavalry buff
                            ECB.SetComponentEnabled<ContributeSightTag>(index, selfEntity, unitAttr.Type == UnitType.Cavalry);
                        }
                    }
                    fowAgent.IsInsight = true; // When first spawn, player unit or building must be visible
                    return;
                }

                ECB.AddComponent<AITag>(index, selfEntity);
                if (EnemyFaction == FactionTag.Ally)
                {
                    fowAgent.IsInsight = true; // When first spawn, enemy unit or building is visible when it is light faction
                }
                else
                {
                    fowAgent.IsInsight = false;
                    ECB.AddComponent<DisappearInFowTag>(index, selfEntity);
                    ECB.AddComponent(index, selfEntity, new HideFowAgentRequest
                    {
                        Hide = true
                    });
                }
               

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
                            or {Type: BuildingType.Ornaments , SubTypeIndex: (int)OrnamentType.Beacon})
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
                            if (attr.FactionTag == FactionTag.Ally)
                            {
                                ECB.AddComponent<ContributeSightTag>(index, selfEntity);
                                ECB.SetComponentEnabled<ContributeSightTag>(index, selfEntity,true);
                            }
                        }

                        break;
                    }
                    case BaseTag.Units:
                        ECB.AddComponent<EnemyUnitCommandData>(index, selfEntity);
                        ECB.AddComponent<EnemyUnitCommandUpdate>(index, selfEntity);
                        ECB.SetComponentEnabled<EnemyUnitCommandUpdate>(index, selfEntity, false);
                        
                        if (attr.FactionTag == FactionTag.Ally)
                        {
                            ECB.AddComponent<ContributeSightTag>(index, selfEntity);
                            ECB.SetComponentEnabled<ContributeSightTag>(index, selfEntity,UnitAttrLookup[selfEntity].Type == UnitType.Cavalry);
                        }
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