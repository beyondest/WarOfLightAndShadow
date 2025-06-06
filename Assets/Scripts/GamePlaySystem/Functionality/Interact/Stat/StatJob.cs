using System;
using System.Runtime.CompilerServices;
using SparFlame.BootStrapper;
using SparFlame.GamePlaySystem.Building;
using SparFlame.GamePlaySystem.Garrison;
using SparFlame.GamePlaySystem.General;
using SparFlame.GamePlaySystem.Movement;
using SparFlame.GamePlaySystem.Ooc;
using SparFlame.GamePlaySystem.Resource;
using SparFlame.GamePlaySystem.Units;
using SparFlame.GamePlaySystem.UnitSelection;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace SparFlame.GamePlaySystem.Interact
{
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
            [NativeDisableParallelForRestriction] public ComponentLookup<UnitAttr> UnitAttrLookup;


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

     

            private void Execute([ChunkIndexInQuery] int index, in StatChangeRequest request, Entity entity)
            {
                // Destroy request
                ECB.DestroyEntity(index, entity);
                // Check if target is already dead
                if (!GeneralAttrLookup.TryGetComponent(request.Interactee, out var interacteeAttr)) return;
                ref var statInteractee = ref StatLookup.GetRefRW(request.Interactee).ValueRW;
                if (statInteractee.CurValue <= 0) return;

                var absAmount = request.AbsAmount;
                switch (request.Type)
                {
                    case StatChangeType.None:
                        break;
                    case StatChangeType.Heal:
                        if(statInteractee.CurValue >= statInteractee.MaxValue + statInteractee.Bonus)return;
                        statInteractee.CurValue = math.min(statInteractee.MaxValue + statInteractee.Bonus,
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
                CheckInteractType(in request, in statInteractee, in interacteeAttr, index,absAmount);

                // Remove Dead Entities, like units, resources, buildings
                if (statInteractee.CurValue <= 0)
                {
                    RemoveAndSendRequest(ref statInteractee, request.Interactee, index, in interacteeAttr);
                }


            }

            

       

            private void CheckInteractType(in StatChangeRequest request, in StatData statInteractee,
                in GeneralAttr interacteeAttr,
                int index, int absAmount)
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
                                var statChangeValue = CalStatChangeValue(absAmount, in SightConfig);
                                target.StatChangValue += statChangeValue;
                                targetsBuffer[i] = target;
                            }
                        }

                        // Update Ooc info(attack state or under attack state tag)
                        if (statInteractee.CurValue > 0 && request.Type != StatChangeType.UnNormalKill)
                            UpdateOocInfo(request, request.InteractorGeneralAttr, interacteeAttr);
                        StatUtils.GeneratePopNumberRequest(ref TransformLookup, request, request.InteractorGeneralAttr,
                            index,
                            ECB, absAmount);
                        break;
                    }
                    case StatChangeType.Harvest:
                        var resourceAttr = ResourceAttrLookup[request.Interactee];
                        StatUtils.GenerateHarvestResourceRequest(request, request.InteractorGeneralAttr, resourceAttr,
                            index, ECB,absAmount);
                        StatUtils.GeneratePopNumberRequest(ref TransformLookup, request, request.InteractorGeneralAttr,
                            index, ECB,absAmount);
                        break;
                    case StatChangeType.Heal:
                        StatUtils.GeneratePopNumberRequest(ref TransformLookup, request, request.InteractorGeneralAttr,
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
                                UnitAttrLookup[interacteeEntity]);
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
                            statInteractee.Bonus = 0;
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
                    statInteractee.CurValue = statInteractee.MaxValue + statInteractee.Bonus;
                if (StatDebug.playerStatGeneralZero && interacteeAttr.FactionTag == PlayerFaction)
                    statInteractee.CurValue = 0f;
                if (StatDebug.aiStatGeneralInfinite && interacteeAttr.FactionTag == ~PlayerFaction)
                    statInteractee.CurValue = statInteractee.MaxValue + statInteractee.Bonus;
                if (StatDebug.aiStatGeneralZero && interacteeAttr.FactionTag == ~PlayerFaction)
                    statInteractee.CurValue = 0f;
                if (StatDebug.playerCrystalStatInfinite && interacteeAttr.FactionTag == PlayerFaction &&
                    interacteeAttr.BaseTag == BaseTag.Buildings)
                {
                    var buildingAttr = BuildingAttrLookup[request.Interactee];
                    if (buildingAttr is { SubTypeIndex: (int)OrnamentType.Crystal, Type: BuildingType.Ornaments }
                        or { SubTypeIndex: (int)OrnamentType.Beacon, Type: BuildingType.Ornaments })
                    {
                        statInteractee.CurValue = statInteractee.MaxValue + statInteractee.Bonus;
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
                        statInteractee.CurValue = statInteractee.MaxValue+ statInteractee.Bonus;
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
                    statInteractee.CurValue = statInteractee.MaxValue+ statInteractee.Bonus;
                }

                if (StatDebug.playerUnitStatZero && interacteeAttr.FactionTag == PlayerFaction &&
                    interacteeAttr.BaseTag == BaseTag.Units)
                {
                    statInteractee.CurValue = 0f;
                }

                if (StatDebug.aiUnitStatInfinite && interacteeAttr.FactionTag == ~PlayerFaction &&
                    interacteeAttr.BaseTag == BaseTag.Units)
                {
                    statInteractee.CurValue = statInteractee.MaxValue+ statInteractee.Bonus;
                }

                if (StatDebug.aiUnitStatZero && interacteeAttr.FactionTag == ~PlayerFaction &&
                    interacteeAttr.BaseTag == BaseTag.Units)
                {
                    statInteractee.CurValue = 0f;
                }

                if (StatDebug.resourceStatInfinite && interacteeAttr.BaseTag == BaseTag.Resources)
                    statInteractee.CurValue = statInteractee.MaxValue + statInteractee.Bonus;
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