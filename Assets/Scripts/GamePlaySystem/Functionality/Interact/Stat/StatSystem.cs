using System;
using System.Runtime.CompilerServices;
using SparFlame.BootStrapper;
using SparFlame.GamePlaySystem.Building;
using SparFlame.GamePlaySystem.Garrison;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using SparFlame.GamePlaySystem.General;
using SparFlame.GamePlaySystem.Movement;
using SparFlame.GamePlaySystem.Ooc;
using SparFlame.GamePlaySystem.Resource;
using SparFlame.GamePlaySystem.Units;
using SparFlame.GamePlaySystem.UnitSelection;
using Unity.Transforms;

// ReSharper disable SwitchStatementHandlesSomeKnownEnumValuesWithDefault

namespace SparFlame.GamePlaySystem.Interact
{
    [UpdateBefore(typeof(GarrisonSystem))]
    public partial struct StatSystem : ISystem
    {
        private ComponentLookup<GeneralAttr> _interactableAttrLookup;
        private ComponentLookup<VolumeObstacleTag> _volumeObstacleTagLookup;
        private ComponentLookup<StatData> _statDataLookup;
        private ComponentLookup<LocalTransform> _localTransformLookup;
        private ComponentLookup<ResourceAttr> _resourceAttrLookup;
        private ComponentLookup<RenewableData> _renewableResourceDataLookup;
        private ComponentLookup<InGarrison> _inGarrisonLookup;
        private ComponentLookup<OocTag> _oocTagLookup;
        private ComponentLookup<BuildingAttr> _buildingAttrLookup;
        private ComponentLookup<InTeamTag> _inTeamTagLookup;
        private ComponentLookup<UnitAttr> _unitAttrLookup;
        private ComponentLookup<DwellingAttr> _dwellingAttrLookup;

        private BufferLookup<InsightTarget> _insightTargetLookup;
        private BufferLookup<CostList> _costListLookup;
        private ComponentLookup<DarkShieldTauntBuff> _tauntBuffLookup;
        private ComponentLookup<DarkShieldTauntedBuff> _tauntedBuffLookup;
        private ComponentLookup<LightShieldBuff> _lightShieldBuffLookup;
        private ComponentLookup<LightShieldUnderDefend> _lightShieldUnderDefendLookup;


        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<GameTimeData>();
            state.RequireForUpdate<PlayerFactionData>();
            state.RequireForUpdate<EndSimulationEntityCommandBufferSystem.Singleton>();
            state.RequireForUpdate<GamingTag>();
            state.RequireForUpdate<EnemyResourceDataTag>();
            state.RequireForUpdate<AllyResourceDataTag>();
            state.RequireForUpdate<StatSystemConfig>();
            state.RequireForUpdate<SightSystemConfig>();
            state.RequireForUpdate<OocSystemConfig>();

            _interactableAttrLookup = state.GetComponentLookup<GeneralAttr>(true);
            _volumeObstacleTagLookup = state.GetComponentLookup<VolumeObstacleTag>(true);
            _statDataLookup = state.GetComponentLookup<StatData>();
            _localTransformLookup = state.GetComponentLookup<LocalTransform>(true);
            _resourceAttrLookup = state.GetComponentLookup<ResourceAttr>();
            _renewableResourceDataLookup = state.GetComponentLookup<RenewableData>(true);
            _oocTagLookup = state.GetComponentLookup<OocTag>();
            _buildingAttrLookup = state.GetComponentLookup<BuildingAttr>(true);
            _inGarrisonLookup = state.GetComponentLookup<InGarrison>(true);
            _insightTargetLookup = state.GetBufferLookup<InsightTarget>();
            _costListLookup = state.GetBufferLookup<CostList>(true);
            _inTeamTagLookup = state.GetComponentLookup<InTeamTag>(true);
            _unitAttrLookup = state.GetComponentLookup<UnitAttr>(true);
            _dwellingAttrLookup = state.GetComponentLookup<DwellingAttr>(true);
            _tauntBuffLookup = state.GetComponentLookup<DarkShieldTauntBuff>(true);
            _tauntedBuffLookup = state.GetComponentLookup<DarkShieldTauntedBuff>(true);
            _lightShieldBuffLookup = state.GetComponentLookup<LightShieldBuff>(true);
            _lightShieldUnderDefendLookup = state.GetComponentLookup<LightShieldUnderDefend>(true);
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            _statDataLookup.Update(ref state);
            _interactableAttrLookup.Update(ref state);
            _volumeObstacleTagLookup.Update(ref state);
            _localTransformLookup.Update(ref state);
            _resourceAttrLookup.Update(ref state);
            _renewableResourceDataLookup.Update(ref state);
            _insightTargetLookup.Update(ref state);
            _inGarrisonLookup.Update(ref state);
            _oocTagLookup.Update(ref state);
            _buildingAttrLookup.Update(ref state);
            _costListLookup.Update(ref state);
            _inTeamTagLookup.Update(ref state);
            _unitAttrLookup.Update(ref state);
            _dwellingAttrLookup.Update(ref state);
            _tauntBuffLookup.Update(ref state);
            _tauntedBuffLookup.Update(ref state);
            _lightShieldBuffLookup.Update(ref state);
            _lightShieldUnderDefendLookup.Update(ref state);
            var autoChooseTargetSystemConfig = SystemAPI.GetSingleton<SightSystemConfig>();
            var oocSystemConfig = SystemAPI.GetSingleton<OocSystemConfig>();
            // var config = SystemAPI.GetSingleton<StatSystemConfig>();
            var ecbSingleton = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>();

            var statRnd = SystemAPI.GetSingletonRW<StatRnd>();
            var rndValue = statRnd.ValueRW.Rnd.NextFloat();


            if (!(SystemAPI.HasSingleton<DebugTag>() && SystemAPI.TryGetSingleton(out StatDebug statDebug)))
            {
                statDebug = new StatDebug
                {
                    enabled = false
                };
            }

            new CheckStatChangeRequest
            {
                ECB = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged).AsParallelWriter(),
                RandomValue = rndValue,
                SightConfig = autoChooseTargetSystemConfig,
                OocConfig = oocSystemConfig,
                PlayerFaction = SystemAPI.GetSingleton<PlayerFactionData>().Value,

                // Debug
                StatDebug = statDebug,

                // Look up
                GeneralAttrLookup = _interactableAttrLookup,
                ObstacleTagLookup = _volumeObstacleTagLookup,
                StatLookup = _statDataLookup,
                TransformLookup = _localTransformLookup,
                TargetListLookup = _insightTargetLookup,
                ResourceAttrLookup = _resourceAttrLookup,
                RenewableResourceDataLookup = _renewableResourceDataLookup,
                InGarrisonLookup = _inGarrisonLookup,
                OocTagLookup = _oocTagLookup,
                CostListLookup = _costListLookup,
                BuildingAttrLookup = _buildingAttrLookup,
                InTeamTagLookup = _inTeamTagLookup,
                UnitAttrLookup = _unitAttrLookup,
                DwellingAttrLookup = _dwellingAttrLookup,


                DarkShieldTauntBuffLookup = _tauntBuffLookup,
                DarkShieldTauntedBuffLookup = _tauntedBuffLookup,
                LightShieldUnderDefendLookup = _lightShieldUnderDefendLookup,
                LightShieldBuffLookup = _lightShieldBuffLookup,
                
            }.Schedule();
        }

        // This job cannot schedule parallel
        [BurstCompile]
        public partial struct CheckStatChangeRequest : IJobEntity
        {
            public EntityCommandBuffer.ParallelWriter ECB;
            [ReadOnly] public float RandomValue;
            [ReadOnly] public FactionTag PlayerFaction;
            [ReadOnly] public SightSystemConfig SightConfig;
            [ReadOnly] public OocSystemConfig OocConfig;
            [ReadOnly] public StatDebug StatDebug;

            [NativeDisableParallelForRestriction] public BufferLookup<InsightTarget> TargetListLookup;
            [NativeDisableParallelForRestriction] public ComponentLookup<StatData> StatLookup;
            [NativeDisableParallelForRestriction] public ComponentLookup<OocTag> OocTagLookup;

            [ReadOnly] public ComponentLookup<GeneralAttr> GeneralAttrLookup;
            [ReadOnly] public ComponentLookup<VolumeObstacleTag> ObstacleTagLookup;
            [ReadOnly] public ComponentLookup<LocalTransform> TransformLookup;
            [ReadOnly] public ComponentLookup<ResourceAttr> ResourceAttrLookup;
            [ReadOnly] public ComponentLookup<RenewableData> RenewableResourceDataLookup;
            [ReadOnly] public ComponentLookup<InGarrison> InGarrisonLookup;
            [ReadOnly] public ComponentLookup<DwellingAttr> DwellingAttrLookup;
            [ReadOnly] public BufferLookup<CostList> CostListLookup; // For population release

            [ReadOnly] public ComponentLookup<BuildingAttr> BuildingAttrLookup;
            [ReadOnly] public ComponentLookup<InTeamTag> InTeamTagLookup;
            [ReadOnly] public ComponentLookup<UnitAttr> UnitAttrLookup;

            // Buff Lookup
            [ReadOnly] public ComponentLookup<DarkShieldTauntBuff> DarkShieldTauntBuffLookup;
            [ReadOnly] public ComponentLookup<DarkShieldTauntedBuff> DarkShieldTauntedBuffLookup;
            [ReadOnly] public ComponentLookup<LightShieldUnderDefend> LightShieldUnderDefendLookup;
            [ReadOnly] public ComponentLookup<LightShieldBuff> LightShieldBuffLookup;

            private void Execute([ChunkIndexInQuery] int index, in StatChangeRequest request, Entity entity)
            {
                // Destroy request
                ECB.DestroyEntity(index, entity);
                // Check if target is already dead
                if (!GeneralAttrLookup.TryGetComponent(request.Interactee, out var interacteeAttr)) return;
                ref var statInteractee = ref StatLookup.GetRefRW(request.Interactee).ValueRW;
                if (statInteractee.CurValue <= 0) return;

                var absAmount = request.AbsAmount;
                PriorApplyBuff(ref absAmount,  request,index);
                switch (request.Type)
                {
                    case StatChangeType.None:
                        break;
                    case StatChangeType.Heal:
                        statInteractee.CurValue = math.min(statInteractee.MaxValue,
                            statInteractee.CurValue + absAmount);
                        break;
                    case StatChangeType.Attack:
                    case StatChangeType.Harvest:
                        statInteractee.CurValue = math.max(0, statInteractee.CurValue - absAmount);
                        break;
                    case StatChangeType.UnNormalKill:
                    case StatChangeType.SimpleCleanUsedAsUpgrade:
                        statInteractee.CurValue = 0;
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }

                if (StatDebug.enabled)
                {
                    DebugCheck(request, interacteeAttr, ref statInteractee);
                }

                // Check interact type and do different jobs according to interact type 
                CheckInteractType(in request, in statInteractee, in interacteeAttr, index);

                // Remove Dead Entities, like units, resources, buildings
                if (statInteractee.CurValue <= 0)
                {
                    RemoveAndSendRequest(ref statInteractee, request.Interactee, index, in interacteeAttr);
                }

                // Only apply buff when statInteractee > 0 && statInteractor > 0 && not special stat change type
                if (statInteractee.CurValue <= 0 || request.Type is StatChangeType.UnNormalKill
                        or StatChangeType.SimpleCleanUsedAsUpgrade or StatChangeType.None) return;
                if (!StatLookup.TryGetComponent(request.Interactor, out var statInteractor)
                    || statInteractor.CurValue <= 0) return;

                LateApplyBuff(index, request, statInteractee);
            }

            private void PriorApplyBuff(ref int absAmount, in StatChangeRequest request,int index)
            {
                if (request.Type == StatChangeType.Attack)
                {
                    // Apply light shield damage reduction
                    if (LightShieldUnderDefendLookup.TryGetComponent(request.Interactee, out var underDefend)
                        && LightShieldUnderDefendLookup.IsComponentEnabled(request.Interactee))
                    {
                        if (LightShieldBuffLookup.TryGetComponent(underDefend.DefendBy, out var defend))
                        {
                            var selfScale = request.IsMagicDamage ? defend.SelfGetMagicDamageScale : defend.SelfGetPhysicalDamageScale;
                            var shieldScale = request.IsMagicDamage ? defend.ShieldGetMagicDamageScale : defend.ShieldGetPhysicalDamageScale;
                            absAmount = (int)(absAmount * selfScale);
                            var lightShieldPassDamage = ECB.CreateEntity(index);
                            ECB.AddComponent(index,lightShieldPassDamage, new StatChangeRequest
                            {
                                AbsAmount = (int)(absAmount * shieldScale),
                                Type = StatChangeType.Attack,
                                Interactee = underDefend.DefendBy,
                                Interactor = request.Interactor,
                                InteractorGeneralAttr = request.InteractorGeneralAttr,
                                IsMagicDamage = request.IsMagicDamage,
                            });
                        }
                    }
                }
            }

            private void LateApplyBuff(int index,in StatChangeRequest request,in StatData statInteractee)
            {
                // Apply dark shield reflect damage
                if (DarkShieldTauntedBuffLookup.HasComponent(request.Interactor) &&   // Only attackers that have taunted buff will get reflected damage
                    DarkShieldTauntedBuffLookup.IsComponentEnabled(request.Interactor) &&
                    DarkShieldTauntBuffLookup.TryGetComponent(request.Interactee, out var tauntBuff) &&
                    request is { Type: StatChangeType.Attack, AbsAmount: > 0 } &&
                    GeneralAttrLookup.TryGetComponent(request.Interactee, out var shieldUnit)) // Only apply to units
                {
                    var reflectDamage = ECB.CreateEntity(index);
                    ECB.AddComponent(index, reflectDamage, new StatChangeRequest
                    {
                        Type = StatChangeType.Attack,
                        AbsAmount = (int)(request.AbsAmount * tauntBuff.ReflectPhysicalDamageScale),
                        Interactee = request.Interactor,
                        Interactor = request.Interactee,
                        InteractorGeneralAttr = shieldUnit,
                    });
                    ECB.AddComponent<GameplayEntityTag>(index, reflectDamage);
                }
                
            }

            private void CheckInteractType(in StatChangeRequest request, in StatData statInteractee,
                in GeneralAttr interacteeAttr,
                int index)
            {
                switch (request.Type)
                {
                    // Raise attacker value in target list only when this hit will not kill the target
                    // Interactor stat must > 0 because it is checked before
                    case StatChangeType.UnNormalKill:
                    case StatChangeType.Attack:
                    {

                        // Update interactee targetList if it has
                        if (statInteractee.CurValue > 0
                            && TargetListLookup.TryGetBuffer(request.Interactee, out var targetsBuffer))
                        {
                            int i;

                            for (i = 0; i < targetsBuffer.Length; i++) // Look for interactor
                            {
                                var target = targetsBuffer[i];
                                if (target.Entity == request.Interactor)
                                    break;
                            }
                            // Target Not in sight but get attacked, should add it to target list
                            if (i == targetsBuffer.Length)
                            {
                                // ECB.AppendToBuffer(index, request.Interactee, );
                                targetsBuffer.Add(new InsightTarget
                                {
                                    Entity = request.Interactor,
                                    PriorityValue = 0f,
                                    DisValue = 0f,
                                    StatChangValue = 0f,
                                    InteractOverride = 0f,
                                    MemoryValue = 0f,
                                    TotalValue = 0f
                                });
                            }
                            else
                            {
                                var target = targetsBuffer[i];
                                var statChangeValue = CalStatChangeValue(request.AbsAmount, in SightConfig);
                                target.StatChangValue += statChangeValue;
                                targetsBuffer[i] = target;
                            }
                        }

                        // Update Ooc info(attack state or under attack state tag)
                        if (statInteractee.CurValue > 0 && request.Type != StatChangeType.UnNormalKill)
                            UpdateOocInfo(request, request.InteractorGeneralAttr, interacteeAttr);
                        StatUtils.GeneratePopNumberRequest(ref TransformLookup, request, request.InteractorGeneralAttr,
                            index,
                            ECB);
                        break;
                    }
                    case StatChangeType.Harvest:
                        var resourceAttr = ResourceAttrLookup[request.Interactee];
                        StatUtils.GenerateHarvestResourceRequest(request, request.InteractorGeneralAttr, resourceAttr,
                            index, ECB);
                        StatUtils.GeneratePopNumberRequest(ref TransformLookup, request, request.InteractorGeneralAttr,
                            index, ECB);
                        break;
                    case StatChangeType.Heal:
                        StatUtils.GeneratePopNumberRequest(ref TransformLookup, request, request.InteractorGeneralAttr,
                            index, ECB);
                        break;
                    case StatChangeType.None:
                    case StatChangeType.SimpleCleanUsedAsUpgrade:
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }


            /// <summary>
            /// This method should contain every post process of that dead entity,
            /// cause entity is destroyed and invalid after this system update
            /// </summary>
            /// <param name="statInteractee"></param>
            /// <param name="interacteeEntity"></param>
            /// <param name="index"></param>
            /// <param name="interacteeAttr"></param>
            private void RemoveAndSendRequest(ref StatData statInteractee, Entity interacteeEntity, int index,
                in GeneralAttr interacteeAttr)
            {
                switch (interacteeAttr.BaseTag)
                {
                    case BaseTag.Units:
                        StatUtils.GenerateGarrisonUnitDieRequest(interacteeEntity, index, interacteeAttr,
                            ref InGarrisonLookup, ECB);
                        StatUtils.GenerateReleasePopulationRequest(interacteeEntity, index, interacteeAttr,
                            ref CostListLookup, ECB);

                        if (InTeamTagLookup.TryGetComponent(interacteeEntity, out var inTeamTag))
                        {
                            StatUtils.GenerateRemoveFromTeamRequest(ECB, index, interacteeEntity, inTeamTag,
                                UnitAttrLookup);
                        }

                        KillUnit(interacteeEntity, index, interacteeAttr);
                        break;
                    case BaseTag.Buildings:
                        if (ObstacleTagLookup.HasComponent(interacteeEntity))
                        {
                            StatUtils.GenerateDestroyObstacleRequest(interacteeEntity, false, index, ECB);
                        }

                        var buildingAttr = BuildingAttrLookup[interacteeEntity];

                        if (buildingAttr.Type == BuildingType.Dwellings)
                        {
                            StatUtils.GenerateDwellingDestroyResourceChangeRequest(interacteeEntity, index,
                                interacteeAttr, DwellingAttrLookup[interacteeEntity], ECB);
                        }
                        var buildingPos = TransformLookup[interacteeEntity].Position;
                        AudioUtils.PlayAudioClip(AudioName.BuildingDestroyed,buildingPos,ECB, index);
                        
                        ECB.DestroyEntity(index, interacteeEntity);
                        break;
                    case BaseTag.Resources:
                        // Check if renewable resource
                        if (RenewableResourceDataLookup.TryGetComponent(interacteeEntity, out var renewableData))
                        {
                            var resourceAttr = ResourceAttrLookup[interacteeEntity];
                            renewableData.RegeneratingLeftTime = renewableData.RegenerationTimeSeconds;
                            var bias = (resourceAttr.AmountRange.upper - resourceAttr.AmountRange.lower) * RandomValue;
                            statInteractee.MaxValue = (int)(resourceAttr.AmountRange.lower + bias);
                            statInteractee.CurValue = statInteractee.MaxValue;
                            ECB.AddComponent<RegeneratingTag>(index, interacteeEntity);
                            ECB.SetComponent(index, interacteeEntity, renewableData);
                        }
                        else
                        {
                            if (ObstacleTagLookup.HasComponent(interacteeEntity))
                            {
                                StatUtils.GenerateDestroyObstacleRequest(interacteeEntity, true, index, ECB);
                            }

                            ECB.DestroyEntity(index, interacteeEntity);
                        }

                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }


            private void UpdateOocInfo(StatChangeRequest request, GeneralAttr interactorAttr,
                GeneralAttr interacteeAttr)
            {
                var interacteeSeconds = interacteeAttr.BaseTag == BaseTag.Buildings
                    ? OocConfig.BuildingOocSeconds
                    : OocConfig.UnitOocSeconds;
                var interactorSeconds = interactorAttr.BaseTag == BaseTag.Buildings
                    ? OocConfig.BuildingOocSeconds
                    : OocConfig.UnitOocSeconds;
                if (OocTagLookup.TryGetComponent(request.Interactee, out var _))
                {
                    ref var ooc = ref OocTagLookup.GetRefRW(request.Interactee).ValueRW;
                    ooc.Seconds = interacteeSeconds;
                    OocTagLookup.SetComponentEnabled(request.Interactee, true);
                }


                // Attacker is already dead
                if (!StatLookup.TryGetComponent(request.Interactor, out var interactorStat)
                    || interactorStat.CurValue <= 0
                    || !OocTagLookup.TryGetComponent(request.Interactor, out var _)) return;
                ref var oocInteractor = ref OocTagLookup.GetRefRW(request.Interactor).ValueRW;
                oocInteractor.Seconds = interactorSeconds;
                OocTagLookup.SetComponentEnabled(request.Interactor, true);
            }

            private void DebugCheck(in StatChangeRequest request, in GeneralAttr interacteeAttr,
                ref StatData statInteractee)
            {
                if(request.Type == StatChangeType.SimpleCleanUsedAsUpgrade)return;
                if (StatDebug.playerStatGeneralInfinite && interacteeAttr.FactionTag == PlayerFaction)
                    statInteractee.CurValue = statInteractee.MaxValue;
                if (StatDebug.playerStatGeneralZero && interacteeAttr.FactionTag == PlayerFaction)
                    statInteractee.CurValue = 0f;
                if (StatDebug.aiStatGeneralInfinite && interacteeAttr.FactionTag == ~PlayerFaction)
                    statInteractee.CurValue = statInteractee.MaxValue;
                if (StatDebug.aiStatGeneralZero && interacteeAttr.FactionTag == ~PlayerFaction)
                    statInteractee.CurValue = 0f;
                if (StatDebug.playerCrystalStatInfinite && interacteeAttr.FactionTag == PlayerFaction &&
                    interacteeAttr.BaseTag == BaseTag.Buildings)
                {
                    var buildingAttr = BuildingAttrLookup[request.Interactee];
                    if (buildingAttr is { SubTypeIndex: (int)OrnamentType.Crystal, Type: BuildingType.Ornaments }
                        or { SubTypeIndex: (int)OrnamentType.Beacon, Type: BuildingType.Ornaments })
                    {
                        statInteractee.CurValue = statInteractee.MaxValue;
                    }
                }

                if (StatDebug.playerCrystalStatZero && interacteeAttr.FactionTag == PlayerFaction &&
                    interacteeAttr.BaseTag == BaseTag.Buildings)
                {
                    var buildingAttr = BuildingAttrLookup[request.Interactee];
                    if (buildingAttr is { SubTypeIndex: (int)OrnamentType.Crystal, Type: BuildingType.Ornaments }
                        or { SubTypeIndex: (int)OrnamentType.Beacon, Type: BuildingType.Ornaments })
                    {
                        statInteractee.CurValue = 0f;
                    }
                }

                if (StatDebug.aiCrystalStatInfinite && interacteeAttr.FactionTag == ~PlayerFaction &&
                    interacteeAttr.BaseTag == BaseTag.Buildings)
                {
                    var buildingAttr = BuildingAttrLookup[request.Interactee];
                    if (buildingAttr is { SubTypeIndex: (int)OrnamentType.Crystal, Type: BuildingType.Ornaments }
                        or { SubTypeIndex: (int)OrnamentType.Beacon, Type: BuildingType.Ornaments })
                    {
                        statInteractee.CurValue = statInteractee.MaxValue;
                    }
                }

                if (StatDebug.aiCrystalStatZero && interacteeAttr.FactionTag == ~PlayerFaction &&
                    interacteeAttr.BaseTag == BaseTag.Buildings)
                {
                    var buildingAttr = BuildingAttrLookup[request.Interactee];
                    if (buildingAttr is { SubTypeIndex: (int)OrnamentType.Crystal, Type: BuildingType.Ornaments }
                        or { SubTypeIndex: (int)OrnamentType.Beacon, Type: BuildingType.Ornaments })
                    {
                        statInteractee.CurValue = 0f;
                    }
                }

                if (StatDebug.playerUnitStatInfinite && interacteeAttr.FactionTag == PlayerFaction &&
                    interacteeAttr.BaseTag == BaseTag.Units)
                {
                    statInteractee.CurValue = statInteractee.MaxValue;
                }

                if (StatDebug.playerUnitStatZero && interacteeAttr.FactionTag == PlayerFaction &&
                    interacteeAttr.BaseTag == BaseTag.Units)
                {
                    statInteractee.CurValue = 0f;
                }

                if (StatDebug.aiUnitStatInfinite && interacteeAttr.FactionTag == ~PlayerFaction &&
                    interacteeAttr.BaseTag == BaseTag.Units)
                {
                    statInteractee.CurValue = statInteractee.MaxValue;
                }

                if (StatDebug.aiUnitStatZero && interacteeAttr.FactionTag == ~PlayerFaction &&
                    interacteeAttr.BaseTag == BaseTag.Units)
                {
                    statInteractee.CurValue = 0f;
                }

                if (StatDebug.resourceStatInfinite && interacteeAttr.BaseTag == BaseTag.Resources)
                    statInteractee.CurValue = statInteractee.MaxValue;
                if (StatDebug.resourceStatZero && interacteeAttr.BaseTag == BaseTag.Resources)
                    statInteractee.CurValue = 0f;
            }

            private void KillUnit(Entity unit, int index, GeneralAttr interacteeAttr)
            {
                ECB.AddComponent<UnitDeadTag>(index, unit);
                AudioUtils.PlayAudioClip(
                    interacteeAttr.FactionTag == FactionTag.Ally ? AudioName.LightDead : AudioName.DarkDead,
                    TransformLookup[unit].Position,
                    ECB, index);
                ECB.RemoveComponent<GeneralAttr>(index, unit);
                ECB.RemoveComponent<UnitAttr>(index, unit);
                ECB.RemoveComponent<StatData>(index, unit);
                ECB.RemoveComponent<MovableData>(index, unit);
                ECB.RemoveComponent<Selected>(index, unit);
            }
            
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private static float CalStatChangeValue(float requestAmount, in SightSystemConfig config)
            {
                return requestAmount * config.StatValueChangeMultiplier;
            }
        }
    }
}