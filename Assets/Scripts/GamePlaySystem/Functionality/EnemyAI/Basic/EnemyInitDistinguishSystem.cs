using System;
using SparFlame.GamePlaySystem.Building;
using SparFlame.GamePlaySystem.Fow;
using SparFlame.GamePlaySystem.General;
using SparFlame.GamePlaySystem.Interact;
using SparFlame.GamePlaySystem.Interact.Blindness;
using SparFlame.GamePlaySystem.Interact.GamePlaySystem.Functionality.Interact.Buff.Authoring;
using SparFlame.GamePlaySystem.Interact.ShieldDefense;
using SparFlame.GamePlaySystem.Resource;
using SparFlame.GamePlaySystem.Units;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Transforms;

namespace SparFlame.GamePlaySystem.EnemyAI
{
    [UpdateInGroup(typeof(InitializationSystemGroup))]
    [UpdateAfter(typeof(EnemyBuildingPackSpawnSystem))]
    public partial struct EnemyInitDistinguishSystem : ISystem
    {
        private ComponentLookup<BuildingAttr> _buildingAttrLookup;
        private BufferLookup<LinkedEntityGroup> _linkedEntityGroupLookup;
        private ComponentLookup<UnitAttr> _unitAttrLookup;
        private ComponentLookup<ExpData> _expLookup;
        private ComponentLookup<FowAgentData> _fowAgentLookup;

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
            _expLookup = state.GetComponentLookup<ExpData>(true);
            _fowAgentLookup = state.GetComponentLookup<FowAgentData>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var config = SystemAPI.GetSingleton<EnemyInitDistinguishConfig>();
            _buildingAttrLookup.Update(ref state);
            _linkedEntityGroupLookup.Update(ref state);
            _unitAttrLookup.Update(ref state);
            _expLookup.Update(ref state);
            _fowAgentLookup.Update(ref state);
            var playerFaction = SystemAPI.GetSingleton<PlayerFactionData>().Value;
            var ecb = new EntityCommandBuffer(Allocator.TempJob);
            var ecbP = ecb.AsParallelWriter();
            var job = new DistinguishEnemyJob
            {
                ECB = ecbP,
                BuildingAttrLookup = _buildingAttrLookup,
                DistinguishConfig = config,
                EnemyFaction = ~playerFaction,
                UnitAttrLookup = _unitAttrLookup,
                ExpDataLookup = _expLookup,
                DarkShieldBuffConfigs = SystemAPI.GetSingletonBuffer<DarkShieldBuffConfig>(),
                BlindnessConfigs = SystemAPI.GetSingletonBuffer<BlindnessConfigs>(),
                LightCavalryBuffConfigs = SystemAPI.GetSingletonBuffer<LightCavalryBuffConfigs>(),
                FowAgentLookup = _fowAgentLookup
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
            [ReadOnly] public ComponentLookup<ExpData> ExpDataLookup;
            [ReadOnly] public EnemyInitDistinguishConfig DistinguishConfig;
            [ReadOnly] public FactionTag EnemyFaction;
            [ReadOnly] public DynamicBuffer<DarkShieldBuffConfig> DarkShieldBuffConfigs;
            [ReadOnly] public DynamicBuffer<BlindnessConfigs> BlindnessConfigs;
            [ReadOnly] public DynamicBuffer<LightCavalryBuffConfigs> LightCavalryBuffConfigs;
            [NativeDisableParallelForRestriction] public ComponentLookup<FowAgentData> FowAgentLookup;

            private void Execute([ChunkIndexInQuery] int index, in GeneralAttr attr,
                in LocalTransform transform, Entity selfEntity)
            {
                var fowAgent = new FowAgentData();

                // Apply buff
                if (attr is { FactionTag: FactionTag.Ally, BaseTag: BaseTag.Units })
                {
                    var unitAttr = UnitAttrLookup[selfEntity];
                    var expData = ExpDataLookup[selfEntity];
                    if (unitAttr.Type != UnitType.Shield)
                    {
                        // Only light units except shield can be defended by light shield
                        ECB.AddBuffer<LightShieldDefenderData>(index, selfEntity); 
                    }
                    else
                    {
                        var shieldBuffRequest = ECB.CreateEntity(index);
                        ECB.AddComponent<GameplayEntityTag>(index, shieldBuffRequest);
                        ECB.AddComponent(index, shieldBuffRequest, new BuffRequest
                        {
                            Filter = new BuffFilter
                            {
                                tierFilterEnabled = true,
                                tier = ExpDataLookup[selfEntity].CurTier,
                            },
                            TrackTarget = selfEntity,
                            Name = BuffName.LightShield,
                            SpawnPosition = transform.Position,
                            SpawnRotation = transform.Rotation,
                        });
                    }

                    if (unitAttr.Type != UnitType.Shield && (unitAttr.Type != UnitType.Magic ||
                                                             unitAttr.SubTypeIndex != (int)MagicType.Cleric))
                    {
                        // Only light units except shield and cleric can be taunted by dark shield
                        ECB.AddComponent<DarkShieldTauntedBuff>(index, selfEntity);
                        ECB.SetComponentEnabled<DarkShieldTauntedBuff>(index, selfEntity, false);
                    }
                    

                    if (unitAttr.Type == UnitType.Cavalry)
                    {
                        ECB.AddComponent(index, selfEntity, new LightCavalryBuffData
                        {
                            LastDuration = LightCavalryBuffConfigs[(int)expData.CurTier - 3].LastDuration,
                            ReduceCurrentHpRatio =
                                LightCavalryBuffConfigs[(int)expData.CurTier - 3].ReduceCurrentHpRatio,
                            StopTime = 0
                        });
                    }
                }
                else if (attr is { FactionTag: FactionTag.Enemy, BaseTag: BaseTag.Units })
                {
                    var unitAttr = UnitAttrLookup[selfEntity];
                    var expData = ExpDataLookup[selfEntity];
                    if (unitAttr.Type == UnitType.Shield)
                    {
                        ECB.AddComponent(index, selfEntity, new DarkShieldTauntBuff
                        {
                            ReflectDamageScale = DarkShieldBuffConfigs[(int)expData.CurTier - 3].reflectDamageScale,
                            MaxTauntCount = DarkShieldBuffConfigs[(int)expData.CurTier - 3].maxTauntCount,
                        });
                    }

                    if (unitAttr.Type == UnitType.Cavalry)
                    {
                        ECB.AddComponent(index, selfEntity, new BlindnessAttackBuff
                        {
                            LastDuration = BlindnessConfigs[(int)expData.CurTier - 3].LastDuration,
                            ReduceCurrentHpRatio = BlindnessConfigs[(int)expData.CurTier - 3].ReduceCurrentHpRatio,
                        });
                    }
                }


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
                                ECB.SetComponentEnabled<ContributeSightTag>(index, selfEntity, true);
                                ECB.AddComponent<BlindnessLastData>(index, selfEntity);
                            }
                        }
                    }
                    else
                    {
                        var unitAttr = UnitAttrLookup[selfEntity];
                        if (attr.FactionTag == FactionTag.Ally)
                        {
                            if (unitAttr.Type == UnitType.Cavalry)
                            {
                                ECB.AddComponent<ContributeSightTag>(index, selfEntity);
                                // Only light cavalry can contribute to light, other light unit may have light by cavalry buff
                                ECB.SetComponentEnabled<ContributeSightTag>(index, selfEntity, false);
                            }
                        }
                    }

                    if (FowAgentLookup.HasComponent(selfEntity))
                        FowAgentLookup.GetRefRW(selfEntity).ValueRW.IsInsight = true;

                    return;
                }

                ECB.AddComponent<AITag>(index, selfEntity);
                if (EnemyFaction == FactionTag.Ally)
                {
                    fowAgent.IsInsight =
                        true; // When first spawn, enemy unit or building is visible when it is light faction
                }
                else
                {
                    fowAgent.IsInsight = false;
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

                            // var request = ECB.CreateEntity(index);
                            // ECB.AddComponent(index, request, new ChangeOccupiedTagRequest
                            // {
                            //     CrystalFaction = EnemyFaction,
                            //     CrystalPos = new float3(transform.Position.x, 0f, transform.Position.z),
                            //     IsDestroyed = false
                            // });
                            // ECB.AddComponent<GameplayEntityTag>(index, selfEntity);
                            if (attr.FactionTag == FactionTag.Ally)
                            {
                                ECB.AddComponent<ContributeSightTag>(index, selfEntity);
                                ECB.SetComponentEnabled<ContributeSightTag>(index, selfEntity, true);
                                ECB.AddComponent<BlindnessLastData>(index, selfEntity);
                            }
                        }

                        break;
                    }
                    case BaseTag.Units:
                        ECB.AddComponent<EnemyUnitCommandData>(index, selfEntity);
                        ECB.AddComponent<EnemyUnitCommandUpdate>(index, selfEntity);
                        ECB.SetComponentEnabled<EnemyUnitCommandUpdate>(index, selfEntity, false);

                        if (attr.FactionTag == FactionTag.Ally && UnitAttrLookup[selfEntity].Type == UnitType.Cavalry)
                        {
                            ECB.AddComponent<ContributeSightTag>(index, selfEntity);
                            ECB.SetComponentEnabled<ContributeSightTag>(index, selfEntity, false);
                        }

                        break;
                    case BaseTag.Resources:
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
                if (FowAgentLookup.HasComponent(selfEntity))
                    FowAgentLookup.GetRefRW(selfEntity).ValueRW.IsInsight = fowAgent.IsInsight;
            }
        }
    }
}