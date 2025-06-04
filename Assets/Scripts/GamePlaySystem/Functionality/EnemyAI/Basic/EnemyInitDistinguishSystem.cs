using System;
using SparFlame.GamePlaySystem.Building;
using SparFlame.GamePlaySystem.General;
using SparFlame.GamePlaySystem.Interact;
using SparFlame.GamePlaySystem.Resource;
using SparFlame.GamePlaySystem.Units;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Transforms;

namespace SparFlame.GamePlaySystem.EnemyAI
{
    // TODO : Move this system to a more appropriate place
    [UpdateInGroup(typeof(InitializationSystemGroup))]
    public partial struct EnemyInitDistinguishSystem : ISystem
    {
        private ComponentLookup<BuildingAttr> _buildingAttrLookup;
        private BufferLookup<LinkedEntityGroup> _linkedEntityGroupLookup;
        private ComponentLookup<UnitAttr> _unitAttrLookup;
        private ComponentLookup<ExpData> _expLookup;

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
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var config = SystemAPI.GetSingleton<EnemyInitDistinguishConfig>();
            _buildingAttrLookup.Update(ref state);
            _linkedEntityGroupLookup.Update(ref state);
            _unitAttrLookup.Update(ref state);
            _expLookup.Update(ref state);
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
                LightShieldBuffConfigs = SystemAPI.GetSingletonBuffer<LightShieldBuffConfig>(),
                LightShieldBuffGeneralConfig = SystemAPI.GetSingleton<LightShieldBuffGeneralConfig>(),
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
            [ReadOnly] public DynamicBuffer<LightShieldBuffConfig> LightShieldBuffConfigs;
            [ReadOnly] public LightShieldBuffGeneralConfig LightShieldBuffGeneralConfig;

            private void Execute([ChunkIndexInQuery] int index, in GeneralAttr attr,
                in LocalTransform transform, Entity selfEntity)
            {

                // Apply buff
                if (attr is { FactionTag: FactionTag.Ally, BaseTag: BaseTag.Units })
                {
                    var unitAttr = UnitAttrLookup[selfEntity];
                    var expData = ExpDataLookup[selfEntity];
                    if (unitAttr.Type != UnitType.Shield)
                    {
                        // Only light units except shield can be defended by light shield
                        ECB.AddComponent<LightShieldUnderDefend>(index, selfEntity);
                        ECB.SetComponentEnabled<LightShieldUnderDefend>(index, selfEntity, false);
                    }
                    else
                    {
                        ECB.AddComponent(index, selfEntity, new AoeTriggerRequest
                        {
                            Prefab = LightShieldBuffGeneralConfig.LightShieldAoeTriggerPrefab
                        });
                        ECB.AddBuffer<AoeTarget>(index, selfEntity);
                        ECB.AddComponent(index, selfEntity, new LightShieldBuff
                        {
                            ShieldGetPhysicalDamageScale = LightShieldBuffConfigs[(int)expData.CurTier - 3].shieldGetPhysicalDamageScale,
                            SelfGetPhysicalDamageScale = LightShieldBuffConfigs[(int)expData.CurTier - 3].selfGetPhysicalDamageScale,
                            ShieldGetMagicDamageScale = LightShieldBuffConfigs[(int)expData.CurTier - 3].shieldGetMagicDamageScale,
                            SelfGetMagicDamageScale = LightShieldBuffConfigs[(int)expData.CurTier - 3].selfGetMagicDamageScale,
                            MaxDefendCount = LightShieldBuffConfigs[(int)expData.CurTier - 3].maxDefendCount
                        });
                    }

                    if (unitAttr.Type != UnitType.Shield && (unitAttr.Type != UnitType.Magic ||
                                                             unitAttr.SubTypeIndex != (int)MagicType.Cleric))
                    {
                        // Only light units except shield and cleric can be taunted by dark shield
                        ECB.AddComponent<DarkShieldTauntedBuff>(index, selfEntity);
                        ECB.SetComponentEnabled<DarkShieldTauntedBuff>(index, selfEntity, false);
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
                            ReflectPhysicalDamageScale = DarkShieldBuffConfigs[(int)expData.CurTier - 3].reflectPhysicalDamageScale,
                            MaxTauntCount = DarkShieldBuffConfigs[(int)expData.CurTier - 3].maxTauntCount,
                            ReflectMagicDamageScale = DarkShieldBuffConfigs[(int)expData.CurTier - 3].reflectMagicDamageScale
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