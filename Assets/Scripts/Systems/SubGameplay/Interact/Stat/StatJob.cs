using System;
using System.Runtime.CompilerServices;
using SparFlame.Components.ComponentUtils;
using SparFlame.Components.General;
using SparFlame.Components.SubGameplay;
using SparFlame.Systems.General.Audio;
using SparFlame.Systems.SubGameplay.Ooc;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace SparFlame.Systems.SubGameplay.Interact
{
        // This job cannot schedule parallel
        [BurstCompile]
        public partial struct CheckStatChangeRequest : IJobEntity
        {
            public EntityCommandBuffer.ParallelWriter ECB;
            [ReadOnly] public float RandomValue;
            [ReadOnly] public PlayerFactionData PlayerFactionData;
            [ReadOnly] public SightSystemConfig SightConfig;
            [ReadOnly] public OocSystemConfig OocConfig;
            [ReadOnly] public StatDebug StatDebug;

            [NativeDisableParallelForRestriction] public BufferLookup<InsightTarget> TargetListLookup;
            [NativeDisableParallelForRestriction] public ComponentLookup<StatData> StatLookup;
            [NativeDisableParallelForRestriction] public ComponentLookup<OocTag> OocTagLookup;
            [NativeDisableParallelForRestriction] public ComponentLookup<UnitAttr> UnitAttrLookup;


            [ReadOnly] public ComponentLookup<SubGameplayGeneralAttr> GeneralAttrLookup;
            [ReadOnly] public ComponentLookup<VolumeObstacleTag> ObstacleTagLookup;
            [ReadOnly] public ComponentLookup<LocalTransform> TransformLookup;
            [ReadOnly] public ComponentLookup<ResourceAttr> ResourceAttrLookup;
            [ReadOnly] public ComponentLookup<RenewableData> RenewableResourceDataLookup;
            [ReadOnly] public ComponentLookup<InGarrison> InGarrisonLookup;
            [ReadOnly] public ComponentLookup<DwellingAttr> DwellingAttrLookup;
            [ReadOnly] public BufferLookup<CostList> CostListLookup; // For population release

            [ReadOnly] public ComponentLookup<BuildingAttr> BuildingAttrLookup;
            [ReadOnly] public ComponentLookup<InTeamTag> InTeamTagLookup;
            [ReadOnly] public ComponentLookup<ExpData> ExpDataLookup;

     

            private void Execute([ChunkIndexInQuery] int index, in StatChangeRequest request, Entity entity)
            {
                // Destroy request
                ECB.DestroyEntity(index, entity);
                // Check if target is already dead
                if (!GeneralAttrLookup.TryGetComponent(request.Interactee, out var interacteeAttr)) return;
                ref var statInteractee = ref StatLookup.GetRefRW(request.Interactee).ValueRW;
                if (statInteractee.curValue <= 0) return;

                var absAmount = request.AbsAmount;
                switch (request.Type)
                {
                    case StatChangeType.None:
                        break;
                    case StatChangeType.Heal:
                        if(statInteractee.curValue >= statInteractee.maxValue + statInteractee.bonus)return;
                        statInteractee.curValue = math.min(statInteractee.maxValue + statInteractee.bonus,
                            statInteractee.curValue + absAmount);
                        break;
                    case StatChangeType.Attack:
                    case StatChangeType.Harvest:
                        statInteractee.curValue = math.max(0, statInteractee.curValue - absAmount);
                        break;
                    case StatChangeType.UnNormalKill:
                    case StatChangeType.SimpleCleanUsedAsUpgrade:
                        statInteractee.curValue = 0;
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }

                if (StatDebug.enabled)
                {
                    DebugCheck(request, interacteeAttr, ref statInteractee);
                }

                // Check interact type and do different jobs according to interact type 
                CheckInteractType(in request, in statInteractee, in interacteeAttr, index,absAmount);

                // Remove Dead Entities, like units, resources, buildings
                if (statInteractee.curValue <= 0)
                {
                    RemoveAndSendRequest(ref statInteractee, request, index, in interacteeAttr);
                }


            }

            

       

            private void CheckInteractType(in StatChangeRequest request, in StatData statInteractee,
                in SubGameplayGeneralAttr interacteeAttr,
                int index, int absAmount)
            {
                switch (request.Type)
                {
                    // Raise attacker value in target list only when this hit will not kill the target
                    // Interactor stat must > 0 because it is checked before
                    case StatChangeType.UnNormalKill:
                    case StatChangeType.Attack:
                    {
                        if (request.InteractorSubGameplayGeneralAttr.BaseTag == BaseTag.Units)
                        {

                            if (!ExpDataLookup.TryGetComponent(request.Interactee, out var expData))
                                expData.curLevel = 1;
                            var expGainRequest = ECB.CreateEntity(index);
                            ECB.AddComponent<SubGameplayEntityTag>(index, expGainRequest);
                            ECB.AddComponent(index, expGainRequest, new ExpGainRequest
                            {
                                Type = ExpGainType.AttackerGainByAttack,
                                GainEntity = request.Interactor,
                                Multiplier = expData.curLevel + ((int)expData.curTier - 3) * 10 + 1f
                            });
                        }

                        if (interacteeAttr.BaseTag == BaseTag.Units
                            && UnitAttrLookup.TryGetComponent(request.Interactee, out var interacteeUnitAttr)
                            && interacteeUnitAttr.Type == UnitType.Shield)
                        {
                            if (!ExpDataLookup.TryGetComponent(request.Interactor, out var expData))
                                expData.curLevel = 1;
                            var expGainRequest = ECB.CreateEntity(index);
                            ECB.AddComponent<SubGameplayEntityTag>(index, expGainRequest);
                            ECB.AddComponent(index, expGainRequest, new ExpGainRequest
                            {
                                Type = ExpGainType.ShieldGainByGetDamage,
                                GainEntity = request.Interactee,
                                Multiplier = expData.curLevel
                            });
                        }
                        // Update interactee targetList if it has
                        if (statInteractee.curValue > 0
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
                                var statChangeValue = CalStatChangeValue(absAmount, in SightConfig);
                                target.StatChangValue += statChangeValue;
                                targetsBuffer[i] = target;
                            }
                        }

                        // Update Ooc info(attack state or under attack state tag)
                        if (statInteractee.curValue > 0 && request.Type != StatChangeType.UnNormalKill)
                            UpdateOocInfo(request, request.InteractorSubGameplayGeneralAttr, interacteeAttr);
                        StatUtils.GeneratePopNumberRequest(ref TransformLookup, request, request.InteractorSubGameplayGeneralAttr,
                            index,
                            ECB, absAmount);
                        break;
                    }
                    case StatChangeType.Harvest:
                        if (request.InteractorSubGameplayGeneralAttr.BaseTag == BaseTag.Units)
                        {
                            var expGainRequest = ECB.CreateEntity(index);
                            ECB.AddComponent<SubGameplayEntityTag>(index, expGainRequest);
                            ECB.AddComponent(index, expGainRequest, new ExpGainRequest
                            {
                                Type = ExpGainType.HarvestGainByHarvest,
                                GainEntity = request.Interactor,
                                Multiplier = 1f
                            });
                        }
                        var resourceAttr = ResourceAttrLookup[request.Interactee];
                        StatUtils.GenerateHarvestResourceRequest(request, request.InteractorSubGameplayGeneralAttr, resourceAttr,
                            index, ECB,absAmount);
                        StatUtils.GeneratePopNumberRequest(ref TransformLookup, request, request.InteractorSubGameplayGeneralAttr,
                            index, ECB,absAmount);
                        break;
                    case StatChangeType.Heal:
                        if (request.InteractorSubGameplayGeneralAttr.BaseTag == BaseTag.Units)
                        {
                            var expGainRequest = ECB.CreateEntity(index);
                            ECB.AddComponent<SubGameplayEntityTag>(index, expGainRequest);
                            ECB.AddComponent(index, expGainRequest, new ExpGainRequest
                            {
                                Type = ExpGainType.HealerGainByHeal,
                                GainEntity = request.Interactor,
                                Multiplier = 1f
                            });
                        }
                        StatUtils.GeneratePopNumberRequest(ref TransformLookup, request, request.InteractorSubGameplayGeneralAttr,
                            index, ECB,absAmount);
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
            /// <param name="request"></param>
            /// <param name="index"></param>
            /// <param name="interacteeAttr"></param>
            private void RemoveAndSendRequest(ref StatData statInteractee, in StatChangeRequest request, int index,
                in SubGameplayGeneralAttr interacteeAttr)
            {
                switch (interacteeAttr.BaseTag)
                {
                    case BaseTag.Units:
                        StatUtils.GenerateGarrisonUnitDieRequest(request.Interactee, index, interacteeAttr,
                            ref InGarrisonLookup, ECB);
                        StatUtils.GenerateReleasePopulationRequest(request.Interactee, index, interacteeAttr,
                            ref CostListLookup, ECB);

                        if (InTeamTagLookup.TryGetComponent(request.Interactee, out var inTeamTag))
                        {
                            StatUtils.GenerateRemoveFromTeamRequest(ECB, index, request.Interactee, inTeamTag,
                                UnitAttrLookup[request.Interactee]);
                        }

                        KillUnit(request, index, interacteeAttr);
                        break;
                    case BaseTag.Buildings:
                        if (ObstacleTagLookup.HasComponent(request.Interactee))
                        {
                            StatUtils.GenerateDestroyObstacleRequest(request.Interactee, false, index, ECB);
                        }

                        var buildingAttr = BuildingAttrLookup[request.Interactee];

                        if (buildingAttr.Type == BuildingType.Dwellings)
                        {
                            StatUtils.GenerateDwellingDestroyResourceChangeRequest(request.Interactee, index,
                                interacteeAttr, DwellingAttrLookup[request.Interactee], ECB);
                        }
                        var buildingPos = TransformLookup[request.Interactee].Position;
                        AudioUtils.PlayAudioClip(AudioName.BuildingDestroyed,buildingPos,ECB, index);
                        
                        ECB.DestroyEntity(index, request.Interactee);
                        break;
                    case BaseTag.Resources:
                        // Check if renewable resource
                        if (RenewableResourceDataLookup.TryGetComponent(request.Interactee, out var renewableData))
                        {
                            var resourceAttr = ResourceAttrLookup[request.Interactee];
                            renewableData.RegeneratingLeftTime = renewableData.RegeneratingTimeHours;
                            var bias = (resourceAttr.AmountRange.upper - resourceAttr.AmountRange.lower) * RandomValue;
                            statInteractee.maxValue = (int)(resourceAttr.AmountRange.lower + bias);
                            statInteractee.curValue = statInteractee.maxValue;
                            statInteractee.bonus = 0;
                            ECB.AddComponent<RegeneratingTag>(index, request.Interactee);
                            ECB.SetComponent(index, request.Interactee, renewableData);
                        }
                        else
                        {
                            if (ObstacleTagLookup.HasComponent(request.Interactee))
                            {
                                StatUtils.GenerateDestroyObstacleRequest(request.Interactee, true, index, ECB);
                            }

                            ECB.DestroyEntity(index, request.Interactee);
                        }

                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }


            private void UpdateOocInfo(StatChangeRequest request, SubGameplayGeneralAttr interactorAttr,
                SubGameplayGeneralAttr interacteeAttr)
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
                    || interactorStat.curValue <= 0
                    || !OocTagLookup.TryGetComponent(request.Interactor, out var _)) return;
                ref var oocInteractor = ref OocTagLookup.GetRefRW(request.Interactor).ValueRW;
                oocInteractor.Seconds = interactorSeconds;
                OocTagLookup.SetComponentEnabled(request.Interactor, true);
            }

            private void DebugCheck(in StatChangeRequest request, in SubGameplayGeneralAttr interacteeAttr,
                ref StatData statInteractee)
            {
                if(request.Type == StatChangeType.SimpleCleanUsedAsUpgrade)return;
                var relationship = FactionUtils.GetRelationship(PlayerFactionData, interacteeAttr.Faction,
                    interacteeAttr.SubFaction);
                if (StatDebug.playerStatGeneralInfinite && relationship == Relationship.Player)
                    statInteractee.curValue = statInteractee.maxValue + statInteractee.bonus;
                if (StatDebug.playerStatGeneralZero &&relationship == Relationship.Player)
                    statInteractee.curValue = 0f;
                if (StatDebug.aiStatGeneralInfinite && relationship != Relationship.Player)
                    statInteractee.curValue = statInteractee.maxValue + statInteractee.bonus;
                if (StatDebug.aiStatGeneralZero &&  relationship != Relationship.Player)
                    statInteractee.curValue = 0f;
                if (StatDebug.playerCrystalStatInfinite &&  relationship == Relationship.Player &&
                    interacteeAttr.BaseTag == BaseTag.Buildings)
                {
                    var buildingAttr = BuildingAttrLookup[request.Interactee];
                    if (buildingAttr is { SubTypeIndex: (int)OrnamentType.Crystal, Type: BuildingType.Ornaments }
                        or { SubTypeIndex: (int)OrnamentType.Beacon, Type: BuildingType.Ornaments })
                    {
                        statInteractee.curValue = statInteractee.maxValue + statInteractee.bonus;
                    }
                }

                if (StatDebug.playerCrystalStatZero &&  relationship == Relationship.Player &&
                    interacteeAttr.BaseTag == BaseTag.Buildings)
                {
                    var buildingAttr = BuildingAttrLookup[request.Interactee];
                    if (buildingAttr is { SubTypeIndex: (int)OrnamentType.Crystal, Type: BuildingType.Ornaments }
                        or { SubTypeIndex: (int)OrnamentType.Beacon, Type: BuildingType.Ornaments })
                    {
                        statInteractee.curValue = 0f;
                    }
                }

                if (StatDebug.aiCrystalStatInfinite && relationship != Relationship.Player  &&
                    interacteeAttr.BaseTag == BaseTag.Buildings)
                {
                    var buildingAttr = BuildingAttrLookup[request.Interactee];
                    if (buildingAttr is { SubTypeIndex: (int)OrnamentType.Crystal, Type: BuildingType.Ornaments }
                        or { SubTypeIndex: (int)OrnamentType.Beacon, Type: BuildingType.Ornaments })
                    {
                        statInteractee.curValue = statInteractee.maxValue+ statInteractee.bonus;
                    }
                }

                if (StatDebug.aiCrystalStatZero && relationship != Relationship.Player  &&
                    interacteeAttr.BaseTag == BaseTag.Buildings)
                {
                    var buildingAttr = BuildingAttrLookup[request.Interactee];
                    if (buildingAttr is { SubTypeIndex: (int)OrnamentType.Crystal, Type: BuildingType.Ornaments }
                        or { SubTypeIndex: (int)OrnamentType.Beacon, Type: BuildingType.Ornaments })
                    {
                        statInteractee.curValue = 0f;
                    }
                }

                if (StatDebug.playerUnitStatInfinite && relationship == Relationship.Player  &&
                    interacteeAttr.BaseTag == BaseTag.Units)
                {
                    statInteractee.curValue = statInteractee.maxValue+ statInteractee.bonus;
                }

                if (StatDebug.playerUnitStatZero &&relationship == Relationship.Player  &&
                    interacteeAttr.BaseTag == BaseTag.Units)
                {
                    statInteractee.curValue = 0f;
                }

                if (StatDebug.aiUnitStatInfinite &&relationship != Relationship.Player  &&
                    interacteeAttr.BaseTag == BaseTag.Units)
                {
                    statInteractee.curValue = statInteractee.maxValue+ statInteractee.bonus;
                }

                if (StatDebug.aiUnitStatZero && relationship != Relationship.Player &&
                    interacteeAttr.BaseTag == BaseTag.Units)
                {
                    statInteractee.curValue = 0f;
                }

                if (StatDebug.resourceStatInfinite && interacteeAttr.BaseTag == BaseTag.Resources)
                    statInteractee.curValue = statInteractee.maxValue + statInteractee.bonus;
                if (StatDebug.resourceStatZero && interacteeAttr.BaseTag == BaseTag.Resources)
                    statInteractee.curValue = 0f;
            }

            private void KillUnit(in StatChangeRequest request, int index, SubGameplayGeneralAttr interacteeAttr)
            {
                if (request.Type == StatChangeType.SimpleCleanUsedAsUpgrade)
                {
                    ECB.DestroyEntity(index, request.Interactee);
                    return;
                }
                ECB.AddComponent<UnitDeadTag>(index, request.Interactee);
                AudioUtils.PlayAudioClip(
                    interacteeAttr.Faction == FactionTag.Light ? AudioName.LightDead : AudioName.DarkDead,
                    TransformLookup[request.Interactee].Position,
                    ECB, index);
                ECB.RemoveComponent<SubGameplayGeneralAttr>(index, request.Interactee);
                ECB.RemoveComponent<UnitAttr>(index, request.Interactee);
                ECB.RemoveComponent<StatData>(index, request.Interactee);
                ECB.RemoveComponent<MovableData>(index, request.Interactee);
                ECB.RemoveComponent<Selected>(index, request.Interactee);
            }
            
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private static float CalStatChangeValue(float requestAmount, in SightSystemConfig config)
            {
                return requestAmount * config.StatValueChangeMultiplier;
            }
        }
}