using SparFlame.Components.General;
using SparFlame.Components.MainGameplay;
using SparFlame.Components.SubGameplay;
using SparFlame.Core.Structs;
using SparFlame.Core.Utils;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace SparFlame.Systems.MainGameplay.EnemyAI
{
    [BurstCompile]
    [WithAll(typeof(AITag))]
    public partial struct EnemyCityStateMachineJob : IJobEntity
    {
        public EntityCommandBuffer.ParallelWriter ECB;
        [ReadOnly] public NativeHashMap<int, ExpStaticConfig> ExpDatabase;
        [ReadOnly] public EnemyAIMainGameplayDebug Debug;

        // Cur info
        [ReadOnly] public float CurTotalHours;

        // Config
        [ReadOnly] public ArmyGroupThreatenCalculationConfig CalConfig;
        [ReadOnly] public FormationConfig FormationConfig;
        [ReadOnly] public DynamicBuffer<VeryRadicalPossibilityConfig> VeryRadicalPossibilityBuffer;
        // Component lookup

        [ReadOnly] public ComponentLookup<GlobalSingleId> SingleIdLookup;
        [ReadOnly] public ComponentLookup<ArmyGroupAttr> ArmyGroupAttrLookup;
        [ReadOnly] public ComponentLookup<SubGameplayGeneralAttr> SubGameplayGeneralLookup;
        [ReadOnly] public BufferLookup<EnemyArmyGroupCompositionData> CompositionLookup;


        [ReadOnly] public ComponentLookup<UnitAttr> UnitAttrLookup;
        [ReadOnly] public ComponentLookup<AttackAbility> AttackLookup;
        [ReadOnly] public ComponentLookup<HealAbility> HealLookup;
        [ReadOnly] public ComponentLookup<HarvestAbility> HarvestLookup;
        [ReadOnly] public ComponentLookup<PrefabId> PrefabIdLookup;
        [NativeDisableParallelForRestriction] public UnitUpgradeAspect.Lookup UnitUpgradeAspectLookup;


        [ReadOnly] public ComponentLookup<ArmyGroupThreatenData> ThreatenDataLookup;
        [ReadOnly] public ComponentLookup<ArmyGroupStatData> ArmyGroupStatDataLookup;
        [ReadOnly] public ComponentLookup<ArmyGroupInGarrison> ArmyGroupInGarrisonLookup;

        [ReadOnly] public NativeParallelHashMap<IntPair, float> CityDistanceMap;

        private void Execute([ChunkIndexInQuery] int index,
            in MainGameplayGeneralAttr generalAttr,
            in PrefabId selfPrefabId,
            in DynamicBuffer<InvadeTarget> invadeTargets,
            in DynamicBuffer<AttackArmyGroupPrefab> attackPrefabs,
            in DynamicBuffer<DefendArmyGroupPrefab> defendPrefabs,
            ref DynamicBuffer<AttackArmyGroup> attackArmyGroups,
            ref DynamicBuffer<DefendArmyGroup> defendArmyGroups,
            ref DynamicBuffer<ExtraArmyGroup> extraArmyGroups,
            ref CityAIData cityAIData,
            ref Rnd rnd,
            ref DynamicBuffer<ArmyGroupConjureStack> conjureStack,
            ref DynamicBuffer<InvadingArmyGroup> invadingArmyGroups,
            Entity selfEntity)
        {
            var isFocusOnPlayer = cityAIData.IsFocusOnPlayer;

            // First conjure army group if queue is not empty. If city do conjure this frame, do AI logic next frame
            if (CheckConjureStackAndSpawnArmyGroup(index, generalAttr, ref cityAIData, ref conjureStack,
                    in attackPrefabs, in defendPrefabs, selfEntity))
                return;

            // Remove dead invading army groups, assign single id after army group single id is created
            RefreshArmyGroupBuffer(ref attackArmyGroups, selfEntity);
            RefreshArmyGroupBuffer(ref defendArmyGroups, selfEntity);
            RefreshArmyGroupBuffer(ref extraArmyGroups, selfEntity);
            RefreshArmyGroupBuffer(ref invadingArmyGroups, selfEntity);

            // If this city is invading or has no army groups, should conjure army groups
            var shouldConjureArmyGroups =
                invadingArmyGroups.Length > 0 || attackArmyGroups.IsEmpty && defendArmyGroups.IsEmpty
                                                                          && extraArmyGroups.IsEmpty;

            // Not focus on player city is always conservative 
            var realStrategy = isFocusOnPlayer ? cityAIData.Strategy : EnemyCityStrategy.Conservative;

            // Check apply very radical strategy logic
            if (realStrategy == EnemyCityStrategy.VeryRadical && !shouldConjureArmyGroups)
            {
                if (VeryRadicalLogic(index, selfPrefabId, invadeTargets, attackArmyGroups,
                        extraArmyGroups,
                        defendArmyGroups, ref cityAIData,ref rnd,
                        ref invadingArmyGroups)) return;
            }

            // Conjure stack not empty, do current conjure job
            if (conjureStack.Length > 0) return;

            // Conjure stack is empty, check should conjure which army group first. 
            if (realStrategy is EnemyCityStrategy.Conservative or EnemyCityStrategy.VeryConservative)
            {
                ConservativeLogic(index, selfPrefabId.value, ref cityAIData, invadeTargets, attackPrefabs,
                    defendPrefabs,
                    attackArmyGroups, defendArmyGroups,
                    ref conjureStack, ref invadingArmyGroups, ref shouldConjureArmyGroups,
                    realStrategy);
            }
            else
            {
                RadicalLogic(index, selfPrefabId.value, ref cityAIData, invadeTargets, attackPrefabs,
                    defendPrefabs,
                    attackArmyGroups,
                    defendArmyGroups, ref conjureStack, ref invadingArmyGroups, ref shouldConjureArmyGroups);
            }
        }

        private void ConservativeLogic(
            int index,
            int selfCityId,
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
            // If army group buffer is not full, conjure army group first. First push attack army group to conjure defend first
            if (attackArmyGroups.Length < attackPrefabs.Length)
            {
                shouldConjureArmyGroups = true;
                foreach (var attackPrefab in attackPrefabs)
                {
                    if (!HasThisArmyGroupPrefabInBuffer(attackPrefab, attackArmyGroups))
                    {
                        conjureStack.Add(new ArmyGroupConjureStack
                        {
                            PrefabId = attackPrefab.PrefabId,
                            Duty = EnemyArmyGroupDuty.Attack,
                            NeedHours = attackPrefab.NeedHours,
                        });
                    }
                }
            }

            if (defendArmyGroups.Length < defendPrefabs.Length)
            {
                shouldConjureArmyGroups = true;
                foreach (var defendPrefab in defendPrefabs)
                {
                    if (!HasThisArmyGroupPrefabInBuffer(defendPrefab, defendArmyGroups))
                    {
                        conjureStack.Add(new ArmyGroupConjureStack
                        {
                            PrefabId = defendPrefab.PrefabId,
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
                if (statData.totalCurrentHp < statData.totalMaxHp) return; // Attack army group is attacked before leaving the city, then stay until full hp
            }

            var readyToInvadeArmyGroups = new NativeList<Entity>(Allocator.Temp);
            foreach (var armyGroup in attackArmyGroups)
            {
                readyToInvadeArmyGroups.Add(armyGroup.ArmyGroup);
            }

            EnemyAIUtils.FindNearestCityToInvade(targets, CityDistanceMap, selfCityId, out var best, out _);
            if (best != Entity.Null)
                InvadeCity(index, ref invadingArmyGroups, best, readyToInvadeArmyGroups);
        }

        private void RadicalLogic(
            int index,
            int selfCityId,
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
                    if (!HasThisArmyGroupPrefabInBuffer(attackPrefab, attackArmyGroups))
                    {
                        conjureStack.Add(new ArmyGroupConjureStack
                        {
                            PrefabId = attackPrefab.PrefabId,
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
            if (!shouldInvade && defendArmyGroups.Length < defendPrefabs.Length)
            {
                shouldConjureArmyGroups = true;
                aiData.StartConjuringTotalHours = CurTotalHours;
                foreach (var defendPrefab in defendPrefabs)
                {
                    if (!HasThisArmyGroupPrefabInBuffer(defendPrefab, defendArmyGroups))
                    {
                        conjureStack.Add(new ArmyGroupConjureStack
                        {
                            PrefabId = defendPrefab.PrefabId,
                            Duty = EnemyArmyGroupDuty.Defend,
                            NeedHours = defendPrefab.NeedHours
                        });
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
                if (best != Entity.Null)
                    InvadeCity(index, ref invadingArmyGroups, best, readyToInvadeArmyGroups);
            }
        }

        private bool VeryRadicalLogic(int index,
            PrefabId prefabId,
            in DynamicBuffer<InvadeTarget> invadeTargets,
            in DynamicBuffer<AttackArmyGroup> attackArmyGroups,
            in DynamicBuffer<ExtraArmyGroup> extraArmyGroups,
            in DynamicBuffer<DefendArmyGroup> defendArmyGroups,
            ref CityAIData aiData,
            ref Rnd rnd,
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
                    var pick = rnd.value.NextFloat(0f, 1f);
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
                    prefabId.value, out var nearestCity, out _);
                if (nearestCity != Entity.Null)
                {
                    InvadeCity(index, ref invadingArmyGroups, nearestCity, allArmyGroups);
                    return true;
                }
            }

            return false;
        }

        private void RefreshArmyGroupBuffer<T>(ref DynamicBuffer<T> armyGroups, Entity selfEntity)
            where T : unmanaged, IBufferElementData, ICityArmyGroupElement
        {
            for (var i = armyGroups.Length - 1; i >= 0; i--)
            {
                var armyGroup = armyGroups[i];

                // Enemy army group either dead after invading or garrison into player city 
                if (!ArmyGroupAttrLookup.HasComponent(armyGroup.ArmyGroup) ||
                    ArmyGroupInGarrisonLookup.TryGetComponent(armyGroup.ArmyGroup, out var inGarrison)
                    && inGarrison.City != selfEntity)
                {
                    armyGroups.RemoveAt(i);
                    continue;
                }

                var singleId = SingleIdLookup[armyGroup.ArmyGroup];
                if (armyGroup.SingleId != 0 || singleId.value == 0) continue;
                armyGroup.SingleId = singleId.value;
                armyGroups[i] = armyGroup;
            }
        }

        private bool CheckConjureStackAndSpawnArmyGroup(int index, in MainGameplayGeneralAttr generalAttr,
            ref CityAIData aiData,
            ref DynamicBuffer<ArmyGroupConjureStack> stack,
            in DynamicBuffer<AttackArmyGroupPrefab> attackPrefabs,
            in DynamicBuffer<DefendArmyGroupPrefab> defendPrefabs,
            Entity selfEntity)
        {
            var deltaHours = CurTotalHours - aiData.StartConjuringTotalHours;
            var doConjureThisFrame = false;
            for (var i = stack.Length - 1; i >= 0; i--)
            {
                var e = stack[i];
                var needHours = Debug.enabled ? e.NeedHours * Debug.armyGroupConjureTimeScale : e.NeedHours;
                if (deltaHours < needHours) break;
                doConjureThisFrame = true;
                // Spawn army group and add units to it
                stack.RemoveAt(i);
                deltaHours -= needHours;
                aiData.StartConjuringTotalHours += needHours;
                if (!FindArmyGroupPrefab(e.PrefabId, attackPrefabs, out var prefab))
                    FindArmyGroupPrefab(e.PrefabId, defendPrefabs, out prefab);
                var armyGroup = ECB.Instantiate(index, prefab);
                ECB.AddComponent<MainGameplayEntityTag>(index, armyGroup);
                var armyGroupAttr = ArmyGroupAttrLookup[prefab];
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
                    SingleId = SingleIdLookup[selfEntity].value
                });


                var compositionDatas = CompositionLookup[prefab];
                // Spawn each type units
                var totalUnitCount = 0;
                foreach (var data in compositionDatas)
                {
                    var count = data.Count;
                    if (Debug.enabled) count *= Debug.unitCountScale;
                    totalUnitCount += count;
                }

                ECB.AddComponent(index, armyGroup, new EnemyArmyGroupShouldSaveTag
                {
                    TotalUnitCount = totalUnitCount,
                });

                var positions = new NativeList<float3>(Allocator.Temp);
                ArmyGroupUtils.GetSquareFormationPositions(FormationConfig.squareSpacing, float3.zero,
                    totalUnitCount,
                    positions);

                // Prepare for loop, calculate army group threaten value, 
                // formation positions, unit type data
                var totalThreatenValue = 0f;
                var positionIndex = 0;
                var mainUnitType = UnitType.Magic;
                var mostUnitCount = 0;
                // var totalStatData = new ArmyGroupStatData();
                // Spawn fake unit, assign datas, calculate total threaten value
                foreach (var t in compositionDatas)
                {
                    var spawnCount = t.Count;
                    if (Debug.enabled) spawnCount *= Debug.unitCountScale;
                    var unitAttr = UnitAttrLookup[t.UnitPrefab];
                    var baseValue = unitAttr.Type == UnitType.Magic
                        ? CalConfig.magicUnitBaseThreatenValue
                        : CalConfig.nonMagicUnitBaseThreatenValue;
                    var levelAddValue = EnemyAIUtils.EvaluateLevelAddThreatenValue(t.Level,
                        CalConfig.levelCoefficientA, CalConfig.levelCoefficientB,
                        CalConfig.levelCoefficientA);
                    totalThreatenValue += (baseValue + levelAddValue) * spawnCount;
                    if (spawnCount > mostUnitCount)
                    {
                        mainUnitType = unitAttr.Type;
                        mostUnitCount = spawnCount;
                    }

                    while (spawnCount-- > 0)
                    {
                        SpawnFakeUnit(index, t.UnitPrefab, t.Level, armyGroup, positions[positionIndex],
                            generalAttr);
                        positionIndex++;
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

        private void InvadeCity(int index,
            ref DynamicBuffer<InvadingArmyGroup> invadingArmyGroups,
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
            }
        }

        private void SpawnFakeUnit(int index, Entity prefab, int level, Entity armyGroup,
            float3 position, MainGameplayGeneralAttr cityGeneralAttr)
        {
            var fakeUnit = ECB.CreateEntity(index);

            var prefabId = PrefabIdLookup[prefab];


            // General components
            ECB.AddComponent<GlobalSingleId>(index, fakeUnit);
            ECB.AddComponent<AssignGlobalSingleIDRequest>(index, fakeUnit);
            ECB.AddComponent<AssignRandomRequest>(index, fakeUnit);
            ECB.AddComponent(index,fakeUnit, new Rnd{value = new Random()});
            ECB.AddComponent<NeedSaveTag>(index, fakeUnit);
            ECB.SetComponentEnabled<NeedSaveTag>(index, fakeUnit, false);
            ECB.AddComponent(index, fakeUnit, prefabId);
            var subGameplayGeneralAttr = SubGameplayGeneralLookup[prefab];
            subGameplayGeneralAttr.SubFaction = cityGeneralAttr.subFaction;
            ECB.AddComponent<MainGameplayEntityTag>(index, fakeUnit);
            ECB.AddComponent(index, fakeUnit, subGameplayGeneralAttr);
            ECB.AddComponent(index, fakeUnit, UnitAttrLookup[prefab]);
            var localTrans = new LocalTransform
            {
                Position = position,
                Rotation = quaternion.identity,
                Scale = 1f
            };
            ECB.AddComponent(index, fakeUnit,localTrans);
            ECB.AddComponent(index, fakeUnit, new FormationTransform
            {
                Transform = localTrans
            });

            var upgradeAspect = UnitUpgradeAspectLookup[prefab];


            var statData = new StatData();
            var expData = new ExpData();
            var movableData = new MovableData();
            if (AttackLookup.HasComponent(prefab))
            {
                var attackAbility = AttackLookup[prefab];
                upgradeAspect.SetLevelDataWhenThisIsPrefab(level, ExpDatabase, ref statData,
                    ref movableData, ref expData, ref attackAbility);
                ECB.AddComponent(index, fakeUnit, attackAbility);
            }
            else if (HealLookup.HasComponent(prefab))
            {
                var healAbility = HealLookup[prefab];
                upgradeAspect.SetLevelDataWhenThisIsPrefab(level, ExpDatabase, ref statData,
                    ref movableData, ref expData, ref healAbility);
                ECB.AddComponent(index, fakeUnit, healAbility);
            }
            else if (HarvestLookup.HasComponent(prefab))
            {
                var harvestAbility = HarvestLookup[prefab];
                upgradeAspect.SetLevelDataWhenThisIsPrefab(level, ExpDatabase, ref statData,
                    ref movableData, ref expData, ref harvestAbility);
                ECB.AddComponent(index, fakeUnit, harvestAbility);
            }

            ECB.AddComponent(index, fakeUnit, expData);
            ECB.AddComponent(index, fakeUnit, statData);
            ECB.AddComponent(index, fakeUnit, movableData);

            ECB.AddComponent(index, fakeUnit, new FakeUnitNeedAddToArmyGroupAfterAssignSingleId { ArmyGroup = armyGroup });
        }

        private static bool FindArmyGroupPrefab<T>(int prefabId, DynamicBuffer<T> prefabs, out Entity prefab)
            where T : unmanaged, IBufferElementData, ICityArmyGroupPrefabElement
        {
            prefab = Entity.Null;
            foreach (var t in prefabs)
            {
                if (t.PrefabId != prefabId) continue;
                prefab = t.Prefab;
                return true;
            }

            return false;
        }

        private bool HasThisArmyGroupPrefabInBuffer<TArmyGroupPrefab, TArmyGroup>(TArmyGroupPrefab prefab,
            DynamicBuffer<TArmyGroup> armyGroups)
            where TArmyGroupPrefab : unmanaged, IBufferElementData, ICityArmyGroupPrefabElement
            where TArmyGroup : unmanaged, IBufferElementData, ICityArmyGroupElement
        {
            foreach (var armyGroup in armyGroups)
            {
                if (PrefabIdLookup[armyGroup.ArmyGroup].value
                    == prefab.PrefabId) return true;
            }

            return false;
        }
    }
}