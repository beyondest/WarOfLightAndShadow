using SparFlame.Components.General;
using SparFlame.Components.MainGameplay;
using SparFlame.Components.SubGameplay;
using SparFlame.Core.Structs;
using SparFlame.Core.Utils;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Rendering;
using Unity.Transforms;

namespace SparFlame.Systems.MainGameplay.EnemyAI
{
    [BurstCompile]
    [WithAll(typeof(AITag))]
    public partial struct EnemyCityStateMachineJob : IJobEntity
    {
        public EntityCommandBuffer.ParallelWriter ECB;

        // Cur info
        [ReadOnly] public float CurTotalHours;

        // Config
        [ReadOnly] public ArmyGroupThreatenCalculationConfig CalConfig;
        [ReadOnly] public FormationConfig FormationConfig;
        [ReadOnly] public DynamicBuffer<VeryRadicalPossibility> VeryRadicalPossibilityBuffer;

        // Component lookup
        [ReadOnly] public ComponentLookup<FocusOnPlayerTag> FocusOnPlayerLookup;
        [ReadOnly] public ComponentLookup<ArmyGroupAttr> ArmyGroupAttrLookup;
        [ReadOnly] public ComponentLookup<SubGameplayGeneralAttr> SubGameplayGeneralLookup;
        [ReadOnly] public BufferLookup<EnemyArmyGroupCompositionData> CompositionLookup;
        [ReadOnly] public ComponentLookup<UnitAttr> UnitAttrLookup;
        [ReadOnly] public ComponentLookup<ExpData> ExpLookup;
        [ReadOnly] public ComponentLookup<ArmyGroupThreatenData> ThreatenDataLookup;
        [ReadOnly] public ComponentLookup<ArmyGroupStatData> ArmyGroupStatDataLookup;
        [ReadOnly] public ComponentLookup<ArmyGroupInGarrison> ArmyGroupInGarrisonLookup;

        [ReadOnly] public NativeParallelHashMap<IntPair, float> CityDistanceMap;

        private void Execute([ChunkIndexInQuery] int index,
            in MainGameplayGeneralAttr generalAttr,
            in CityAttr selfCityAttr,
            in DynamicBuffer<InvadeTarget> invadeTargets,
            in DynamicBuffer<AttackArmyGroupPrefab> attackPrefabs,
            in DynamicBuffer<DefendArmyGroupPrefab> defendPrefabs,
            ref DynamicBuffer<AttackArmyGroup> attackArmyGroups,
            ref DynamicBuffer<DefendArmyGroup> defendArmyGroups,
            ref DynamicBuffer<ExtraArmyGroup> extraArmyGroups,
            ref CityAIData aiData,
            ref DynamicBuffer<ArmyGroupConjureStack> conjureStack,
            ref DynamicBuffer<InvadingArmyGroup> invadingArmyGroups,
            Entity selfEntity)
        {
            var isFocusOnPlayer = FocusOnPlayerLookup.IsComponentEnabled(selfEntity);

            // First conjure army group if queue is not empty. If do conjure this frame, do AI logic next frame
            if(CheckConjureStackAndSpawnArmyGroup(index, generalAttr, ref aiData, ref conjureStack, selfEntity))
                return;

            // Remove dead invading army groups
            RemoveInvalidArmyGroupsFromBuffer(ref invadingArmyGroups, ref attackArmyGroups, ref defendArmyGroups,
                ref extraArmyGroups, selfEntity);

            // If this city is invading or has no army groups, should conjure army groups
            var shouldConjureArmyGroups =
                invadingArmyGroups.Length > 0 || attackArmyGroups.IsEmpty && defendArmyGroups.IsEmpty
                                                                          && extraArmyGroups.IsEmpty;

            // Not focus on player city is always conservative 
            var realStrategy = isFocusOnPlayer ? aiData.Strategy : EnemyCityStrategy.Conservative;

            // Check apply very radical strategy logic
            if (realStrategy == EnemyCityStrategy.VeryRadical && !shouldConjureArmyGroups)
            {
                if (VeryRadicalLogic(index, selfCityAttr, selfEntity, invadeTargets, attackArmyGroups, extraArmyGroups,
                        defendArmyGroups, ref aiData,
                        ref invadingArmyGroups)) return;
            }

            // Conjure stack not empty, do current conjure job
            if (conjureStack.Length > 0) return;

            // Conjure stack is empty, check should conjure which army group first. 
            if (realStrategy is EnemyCityStrategy.Conservative or EnemyCityStrategy.VeryConservative)
            {
                ConservativeLogic(index, selfCityAttr.globalId, selfEntity, ref aiData, invadeTargets, attackPrefabs,
                    defendPrefabs,
                    attackArmyGroups, defendArmyGroups,
                    ref conjureStack, ref invadingArmyGroups, ref shouldConjureArmyGroups,
                    realStrategy);
            }
            else
            {
                RadicalLogic(index, selfCityAttr.globalId, selfEntity, ref aiData, invadeTargets, attackPrefabs,
                    defendPrefabs,
                    attackArmyGroups,
                    defendArmyGroups, ref conjureStack, ref invadingArmyGroups, ref shouldConjureArmyGroups);
            }
        }

        private void ConservativeLogic(
            int index,
            int selfCityId,
            Entity selfEntity,
            ref CityAIData aiData,
            in DynamicBuffer<InvadeTarget> targets,
            in DynamicBuffer<AttackArmyGroupPrefab> attackPrefabs,
            in DynamicBuffer<DefendArmyGroupPrefab> defendPrefabs,
            in DynamicBuffer<AttackArmyGroup> attackArmyGroups,
            in DynamicBuffer<DefendArmyGroup> defendArmyGroups,
            ref DynamicBuffer<ArmyGroupConjureStack> conjureStack,
            ref DynamicBuffer<InvadingArmyGroup> invadingArmyGroups,
            ref bool shouldConjureArmyGroups,
            EnemyCityStrategy strategy
        )
        {
            // If army group buffer is not full, conjure army group first. First push attack army group so as to conjure defend first
            if (attackArmyGroups.Length < attackPrefabs.Length)
            {
                shouldConjureArmyGroups = true;
                foreach (var attackPrefab in attackPrefabs)
                {
                    var alreadyHas = false;
                    var name = ArmyGroupAttrLookup[attackPrefab.ArmyGroupPrefab].gameplayName;
                    foreach (var armyGroup in attackArmyGroups)
                    {
                        if (ArmyGroupAttrLookup[armyGroup.ArmyGroup].gameplayName == name)
                        {
                            alreadyHas = true;
                            break;
                        }
                    }

                    if (!alreadyHas)
                    {
                        conjureStack.Add(new ArmyGroupConjureStack
                        {
                            ArmyGroupPrefab = attackPrefab.ArmyGroupPrefab,
                            Duty = EnemyArmyGroupDuty.Attack,
                            NeedHours = attackPrefab.NeedHours
                        });
                    }
                }
            }

            if (defendArmyGroups.Length < defendPrefabs.Length)
            {
                shouldConjureArmyGroups = true;
                foreach (var defendPrefab in defendPrefabs)
                {
                    var alreadyHas = false;
                    var name = ArmyGroupAttrLookup[defendPrefab.ArmyGroupPrefab].gameplayName;
                    foreach (var armyGroup in defendArmyGroups)
                    {
                        if (ArmyGroupAttrLookup[armyGroup.ArmyGroup].gameplayName == name)
                        {
                            alreadyHas = true;
                            break;
                        }
                    }

                    if (!alreadyHas)
                    {
                        conjureStack.Add(new ArmyGroupConjureStack
                        {
                            ArmyGroupPrefab = defendPrefab.ArmyGroupPrefab,
                            Duty = EnemyArmyGroupDuty.Defend,
                            NeedHours = defendPrefab.NeedHours
                        });
                    }
                }
            }

            if (shouldConjureArmyGroups) aiData.StartConjuringTotalHours = CurTotalHours;

            // Not full buffer, do nothing for conservative logic; Very conservative always stay
            if (shouldConjureArmyGroups || strategy == EnemyCityStrategy.VeryConservative) return;

            // Check if attack army group is full hp, if not , stay, else, go attack
            foreach (var armyGroup in attackArmyGroups)
            {
                var statData = ArmyGroupStatDataLookup[armyGroup.ArmyGroup];
                if (statData.totalCurrentHp < statData.totalMaxHp) return;
            }

            var readyToInvadeArmyGroups = new NativeList<Entity>(Allocator.Temp);
            foreach (var armyGroup in attackArmyGroups)
            {
                readyToInvadeArmyGroups.Add(armyGroup.ArmyGroup);
            }

            EnemyAIUtils.FindNearestCityToInvade(targets, CityDistanceMap, selfCityId, out var best, out _);
            if(best != Entity.Null)
                InvadeCity(index, selfEntity, ref invadingArmyGroups, best, readyToInvadeArmyGroups);
        }

        private void RadicalLogic(
            int index,
            int selfCityId,
            Entity selfEntity,
            ref CityAIData aiData,
            in DynamicBuffer<InvadeTarget> targets,
            in DynamicBuffer<AttackArmyGroupPrefab> attackPrefabs,
            in DynamicBuffer<DefendArmyGroupPrefab> defendPrefabs,
            in DynamicBuffer<AttackArmyGroup> attackArmyGroups,
            in DynamicBuffer<DefendArmyGroup> defendArmyGroups,
            ref DynamicBuffer<ArmyGroupConjureStack> conjureStack,
            ref DynamicBuffer<InvadingArmyGroup> invadingArmyGroups,
            ref bool shouldConjureArmyGroups
        )
        {
            // If attack buffer is not full, do conjure job 
            if (attackArmyGroups.Length < attackPrefabs.Length)
            {
                shouldConjureArmyGroups = true;
                foreach (var attackPrefab in attackPrefabs)
                {
                    var alreadyHas = false;
                    var name = ArmyGroupAttrLookup[attackPrefab.ArmyGroupPrefab].gameplayName;
                    foreach (var armyGroup in attackArmyGroups)
                    {
                        if (ArmyGroupAttrLookup[armyGroup.ArmyGroup].gameplayName == name)
                        {
                            alreadyHas = true;
                            break;
                        }
                    }

                    if (!alreadyHas)
                    {
                        conjureStack.Add(new ArmyGroupConjureStack
                        {
                            ArmyGroupPrefab = attackPrefab.ArmyGroupPrefab,
                            Duty = EnemyArmyGroupDuty.Attack,
                            NeedHours = attackPrefab.NeedHours
                        });
                    }
                }
            }


            // Not full attack buffer, do conjure job
            if (shouldConjureArmyGroups)
            {
                aiData.StartConjuringTotalHours = CurTotalHours;
                return;
            }

            // Full attack buffer, invade and check should conjure defend units

            var shouldInvade = invadingArmyGroups.Length == 0;
            // Check if attack army group is full hp, if not , stay, else, go attack
            foreach (var armyGroup in attackArmyGroups)
            {
                var statData = ArmyGroupStatDataLookup[armyGroup.ArmyGroup];
                if (statData.totalCurrentHp < statData.totalMaxHp)
                {
                    shouldInvade = false;
                    break;
                }
            }

            // If not should invade, check and conjure defend army groups
            if (!shouldInvade)
            {
                if (defendArmyGroups.Length < defendPrefabs.Length)
                {
                    shouldConjureArmyGroups = true;
                    aiData.StartConjuringTotalHours = CurTotalHours;
                    foreach (var defendPrefab in defendPrefabs)
                    {
                        var alreadyHas = false;
                        var name = ArmyGroupAttrLookup[defendPrefab.ArmyGroupPrefab].gameplayName;
                        foreach (var armyGroup in defendArmyGroups)
                        {
                            if (ArmyGroupAttrLookup[armyGroup.ArmyGroup].gameplayName == name)
                            {
                                alreadyHas = true;
                                break;
                            }
                        }

                        if (!alreadyHas)
                        {
                            conjureStack.Add(new ArmyGroupConjureStack
                            {
                                ArmyGroupPrefab = defendPrefab.ArmyGroupPrefab,
                                Duty = EnemyArmyGroupDuty.Defend,
                                NeedHours = defendPrefab.NeedHours
                            });
                        }
                    }
                }
            }
            else
            {
                var readyToInvadeArmyGroups = new NativeList<Entity>(Allocator.Temp);
                foreach (var armyGroup in attackArmyGroups)
                {
                    readyToInvadeArmyGroups.Add(armyGroup.ArmyGroup);
                }

                EnemyAIUtils.FindNearestCityToInvade(targets, CityDistanceMap, selfCityId, out var best, out _);
                if(best != Entity.Null)
                    InvadeCity(index, selfEntity, ref invadingArmyGroups, best, readyToInvadeArmyGroups);
            }
        }

        private bool VeryRadicalLogic(int index,
            CityAttr cityAttr,
            Entity selfEntity,
            in DynamicBuffer<InvadeTarget> invadeTargets,
            in DynamicBuffer<AttackArmyGroup> attackArmyGroups,
            in DynamicBuffer<ExtraArmyGroup> extraArmyGroups,
            in DynamicBuffer<DefendArmyGroup> defendArmyGroups,
            ref CityAIData aiData,
            ref DynamicBuffer<InvadingArmyGroup> invadingArmyGroups)
        {
            var allArmyGroupThreatenValue = 0f;
            var allArmyGroups = new NativeList<Entity>(Allocator.Temp);
            foreach (var armyGroup in attackArmyGroups)
            {
                allArmyGroups.Add(armyGroup.ArmyGroup);
                var threatenData = ThreatenDataLookup[armyGroup.ArmyGroup];
                allArmyGroupThreatenValue += threatenData.totalThreatenValue;
            }

            foreach (var armyGroup in extraArmyGroups)
            {
                allArmyGroups.Add(armyGroup.ArmyGroup);
                var threatenData = ThreatenDataLookup[armyGroup.ArmyGroup];
                allArmyGroupThreatenValue += threatenData.totalThreatenValue;
            }

            foreach (var armyGroup in defendArmyGroups)
            {
                allArmyGroups.Add(armyGroup.ArmyGroup);
                var threatenData = ThreatenDataLookup[armyGroup.ArmyGroup];
                allArmyGroupThreatenValue += threatenData.totalThreatenValue;
            }

            // Use random chance and self total threaten value to decide whether to invade
            var shouldInvade = false;
            foreach (var possibility in VeryRadicalPossibilityBuffer)
            {
                if (allArmyGroupThreatenValue >= possibility.minSelfTotalThreaten &&
                    allArmyGroupThreatenValue <= possibility.maxSelfTotalThreaten)
                {
                    var pick = aiData.Rnd.NextFloat(0f, 1f);
                    if (pick < possibility.invadeChance)
                    {
                        shouldInvade = true;
                        break;
                    }
                }
            }

            if (shouldInvade)
            {
                EnemyAIUtils.FindNearestCityToInvade(invadeTargets, CityDistanceMap,
                    cityAttr.globalId, out var nearestCity, out _);
                if (nearestCity != Entity.Null)
                {
                    InvadeCity(index, selfEntity, ref invadingArmyGroups, nearestCity, allArmyGroups);
                    return true;
                }
            }
            return false;
        }

        private void RemoveInvalidArmyGroupsFromBuffer(ref DynamicBuffer<InvadingArmyGroup> invadingArmyGroups,
            ref DynamicBuffer<AttackArmyGroup> attackArmyGroups, ref DynamicBuffer<DefendArmyGroup> defendArmyGroups,
            ref DynamicBuffer<ExtraArmyGroup> extraArmyGroups, Entity selfEntity)
        {
            for (var i = invadingArmyGroups.Length - 1; i >= 0; i--)
            {
                var armyGroup = invadingArmyGroups[i];
                if (!ArmyGroupAttrLookup.HasComponent(armyGroup.ArmyGroup))
                {
                    invadingArmyGroups.RemoveAt(i);
                    continue;
                }

                // Invading army group win and garrison to a new city, remove
                if (ArmyGroupInGarrisonLookup.TryGetComponent(armyGroup.ArmyGroup, out var inGarrison)
                    && inGarrison.City != selfEntity)
                {
                    invadingArmyGroups.RemoveAt(i);
                }
            }

            for (var i = attackArmyGroups.Length - 1; i >= 0; i--)
            {
                var armyGroup = attackArmyGroups[i];
                if (!ArmyGroupAttrLookup.HasComponent(armyGroup.ArmyGroup))
                {
                    attackArmyGroups.RemoveAt(i);
                    continue;
                }

                if (ArmyGroupInGarrisonLookup.TryGetComponent(armyGroup.ArmyGroup, out var inGarrison)
                    && inGarrison.City != selfEntity)
                {
                    attackArmyGroups.RemoveAt(i);
                }
            }

            for (var i = defendArmyGroups.Length - 1; i >= 0; i--)
            {
                var armyGroup = defendArmyGroups[i];
                if (!ArmyGroupAttrLookup.HasComponent(armyGroup.ArmyGroup))
                {
                    defendArmyGroups.RemoveAt(i);
                    continue;
                }

                if (ArmyGroupInGarrisonLookup.TryGetComponent(armyGroup.ArmyGroup, out var inGarrison)
                    && inGarrison.City != selfEntity)
                {
                    defendArmyGroups.RemoveAt(i);
                }
            }

            for (var i = extraArmyGroups.Length - 1; i >= 0; i--)
            {
                var armyGroup = extraArmyGroups[i];
                if (!ArmyGroupAttrLookup.HasComponent(armyGroup.ArmyGroup))
                    extraArmyGroups.RemoveAt(i);
                if (ArmyGroupInGarrisonLookup.TryGetComponent(armyGroup.ArmyGroup, out var inGarrison)
                    && inGarrison.City != selfEntity)
                {
                    extraArmyGroups.RemoveAt(i);
                }
            }
        }

        private bool CheckConjureStackAndSpawnArmyGroup(int index, in MainGameplayGeneralAttr generalAttr,
            ref CityAIData aiData,
            ref DynamicBuffer<ArmyGroupConjureStack> stack,
            Entity selfEntity)
        {
            var deltaHours = CurTotalHours - aiData.StartConjuringTotalHours;
            var doConjureThisFrame = false;
            for (var i = stack.Length - 1; i >= 0; i--)
            {
                var e = stack[i];
                if (deltaHours < e.NeedHours) break;
                doConjureThisFrame = true;
                // Spawn army group and add units to it
                stack.RemoveAt(i);
                deltaHours -= e.NeedHours;
                aiData.StartConjuringTotalHours += e.NeedHours;

                var armyGroup = ECB.Instantiate(index, e.ArmyGroupPrefab);
                ECB.AddComponent<MainGameplayEntityTag>(index, armyGroup);
                var armyGroupAttr = ArmyGroupAttrLookup[e.ArmyGroupPrefab];
                armyGroupAttr.createTimeInTotalHours = CurTotalHours;
                ECB.SetComponent(index, armyGroup, armyGroupAttr);
                // Garrison into this city
                var armyGroupGarrisonRequest = ECB.CreateEntity(index);
                ECB.AddComponent(index, armyGroupGarrisonRequest, new ArmyGroupGarrisonRequest
                {
                    ArmyGroup = armyGroup,
                    City = selfEntity,
                    IfGarrisonIn = true
                });
                ECB.AddComponent<MainGameplayEntityTag>(index, armyGroupGarrisonRequest);
                ECB.AddComponent(index, armyGroup, new EnemyArmyGroupBelongsToCity
                {
                    City = selfEntity,
                });

                ECB.AddComponent<EnemyArmyGroupShouldSaveTag>(index, armyGroup);

                var compositionDatas = CompositionLookup[e.ArmyGroupPrefab];
                // Spawn each type units
                var totalUnitCount = 0;
                foreach (var data in compositionDatas)
                {
                    totalUnitCount += data.Count;
                }

                var positions = new NativeList<float3>(Allocator.Temp);
                ArmyGroupUtils.GetSquareFormationPositions(FormationConfig.squareSpacing, float3.zero, totalUnitCount,
                    positions);
                
                // Prepare for loop, calculate army group threaten value, 
                // formation positions, unit type data
                var totalThreatenValue = 0f;
                var positionIndex = 0;
                var mainUnitType = UnitType.Magic;
                var mostUnitCount = 0;
                // var totalStatData = new ArmyGroupStatData();
                // Instantiate unit, assign datas, calculate total threaten value
                foreach (var t in compositionDatas)
                {
                    var data = t;
                    var unitAttr = UnitAttrLookup[data.UnitPrefab];
                    var expData = ExpLookup[data.UnitPrefab];
                    // var statData = StatDataLookup[data.UnitPrefab];
                    // totalStatData.totalCurrentHp += statData.curValue;
                    // totalStatData.totalMaxHp += statData.maxValue;
                    var subGameGeneralAttr = SubGameplayGeneralLookup[data.UnitPrefab];
                    subGameGeneralAttr.SubFaction = generalAttr.subFaction;

                    var baseValue = unitAttr.Type == UnitType.Magic
                        ? CalConfig.magicUnitBaseThreatenValue
                        : CalConfig.nonMagicUnitBaseThreatenValue;
                    var levelAddValue = EnemyAIUtils.EvaluateLevelAddThreatenValue(data.Level,
                        CalConfig.levelCoefficientA, CalConfig.levelCoefficientB,
                        CalConfig.levelCoefficientA);
                    totalThreatenValue += (baseValue + levelAddValue) * data.Count;
                    if (data.Count > mostUnitCount)
                    {
                        mainUnitType = unitAttr.Type;
                        mostUnitCount = data.Count;
                    }

                    while (data.Count-- > 0)
                    {
                        var unit = ECB.Instantiate(index, data.UnitPrefab);
                        ECB.SetComponent(index, unit, subGameGeneralAttr);
                        expData.curLevel = data.Level;
                        ECB.SetComponent(index, unit, expData);
                        var position = positions[positionIndex++];
                        ECB.SetComponent(index, unit, new LocalTransform
                        {
                            Position = position,
                            Rotation = quaternion.identity,
                            Scale = 1f
                        });
                        ECB.AddComponent<DisableRendering>(index,unit);

                        var addToArmyGroupRequest = ECB.CreateEntity(index);
                        ECB.AddComponent(index, addToArmyGroupRequest, new AddToArmyGroupRequest
                        {
                            ArmyGroup = armyGroup,
                            Type = AddToArmyGroupType.OnlySpecifiedUnit,
                            Unit = unit
                        });
                    }
                }
                // ECB.SetComponent(index, armyGroup, totalStatData);

                ECB.SetComponent(index, armyGroup, new ArmyGroupThreatenData
                {
                    totalThreatenValue = totalThreatenValue,
                    mainUnitType = mainUnitType
                });
                switch (e.Duty)
                {
                    case EnemyArmyGroupDuty.Attack:
                        ECB.AppendToBuffer(index, selfEntity, new AttackArmyGroup
                        {
                            ArmyGroup = armyGroup,
                        });
                        break;
                    case EnemyArmyGroupDuty.Defend:
                        ECB.AppendToBuffer(index, selfEntity, new DefendArmyGroup
                        {
                            ArmyGroup = armyGroup,
                        });
                        break;
                    default:
                        BurstSafe.UnexpectedEnum(e.Duty);
                        break;
                }
            }

            return doConjureThisFrame;
        }

        private void InvadeCity(int index, Entity selfEntity, ref DynamicBuffer<InvadingArmyGroup> invadingArmyGroups,
            Entity nearestCity,
            NativeList<Entity> readyToInvadeArmyGroups)
        {
            foreach (var armyGroup in readyToInvadeArmyGroups)
            {
                invadingArmyGroups.Add(new InvadingArmyGroup
                {
                    ArmyGroup = armyGroup,
                });
                var hintRequest = ECB.CreateEntity(index);
                ECB.AddComponent(index, hintRequest, new HintRequest
                {
                    Name = HintName.EnemyIsGoingToInvade
                });
                ECB.AddComponent<MainGameplayEntityTag>(index, hintRequest);
                ECB.SetComponent(index, armyGroup, new ArmyGroupCommandData
                {
                    TargetCity = nearestCity,
                });
                ECB.SetComponentEnabled<ArmyGroupCommandUpdate>(index, armyGroup, true);

                var garrisonOutRequest = ECB.CreateEntity(index);
                ECB.AddComponent<MainGameplayEntityTag>(index, garrisonOutRequest);
                ECB.AddComponent(index, garrisonOutRequest, new ArmyGroupGarrisonRequest
                {
                    ArmyGroup = armyGroup,
                    City = selfEntity,
                    IfGarrisonIn = false
                });
            }
        }
    }
}