using System;
using SparFlame.Components.General;
using SparFlame.Components.MainGameplay;
using SparFlame.Components.SubGameplay;
using SparFlame.Core.Utils;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Physics;
using Unity.Transforms;

namespace SparFlame.Systems.General.BasicControl
{
    [Serializable]
    public struct SeInverseMass : IComponentData
    {
        public float value;
    }

    [Serializable]
    public struct SeInGarrison : IComponentData
    {
        public long buildingSingleId;
        public bool inBuilding;
        public float priorMass;
    }


    [Serializable]
    public struct SeGarrisonEntity : IBufferElementData
    {
        public long unitTmpId;
        public int id;
    }

    [Serializable]
    public struct SeSingleId : IComponentData
    {
        public long value;
    }

    [Serializable]
    public struct SeInArmyGroup : IComponentData
    {
        public long armyGroupSaveId;
    }

    [Serializable]
    public struct SeLastPassingByCity : IComponentData
    {
        public int cityGlobalIndex;
    }

    [Serializable]
    public struct SeConjuringData : IBufferElementData
    {
        public int unitGlobalId;
        public int targetAmount;
        public int conjuredAmount;
        public float remainingTimeHours;
        public float accumulatedHours;
    }

    [BurstCompile]
    [WithNone(typeof(InArmyGroup))]
    public partial struct SaveSubGameplayJob : IJobEntity
    {
        public EntityCommandBuffer.ParallelWriter ECB;

        [ReadOnly] public BufferLookup<GarrisonEntity> GarrisonEntitiesLookup;
        [ReadOnly] public BufferLookup<GarrisonTypeData> GarrisonTypeDataLookup;
        [ReadOnly] public BufferLookup<ConjuringData> ConjuringDataLookup;

        [ReadOnly] public ComponentLookup<InGarrison> InGarrisonLookup;
        [ReadOnly] public ComponentLookup<PhysicsMass> PhysicsMassLookup;
        [ReadOnly] public ComponentLookup<ConstructingTimer> ConstructingTimerLookup;
        [ReadOnly] public ComponentLookup<CityTaskUniqueId> CityTaskUniqueIdLookup;
        [ReadOnly] public ComponentLookup<GlobalSingleId> GlobalSingleIdLookup;

        private void Execute([ChunkIndexInQuery] int index, in SubGameplayGeneralAttr generalAttr,
            in LocalTransform transform,
            in StatData statData, in ExpData expData, Entity selfEntity)
        {
            var saveEntity = ECB.CreateEntity(index);
            ECB.AddComponent(index, saveEntity, new SeTransform
            {
                position = transform.Position,
                rotation = transform.Rotation,
                scale = transform.Scale,
            });
            ECB.AddComponent(index, saveEntity, new SePrefabId { value = generalAttr.PrefabID });
            ECB.AddComponent(index, saveEntity, statData);
            if (generalAttr.BaseTag == BaseTag.Units)
            {
                ECB.AddComponent(index, saveEntity, expData);
                if (InGarrisonLookup.TryGetComponent(selfEntity, out var inGarrison)) // Safety check
                {
                    var physicsMass = PhysicsMassLookup[selfEntity];
                    ECB.AddComponent(index, saveEntity, new SeInverseMass { value = physicsMass.InverseMass });
                    ECB.AddComponent(index, saveEntity, new SeInGarrison
                    {
                        buildingSingleId = GlobalSingleIdLookup[inGarrison.BuildingEntity].value,
                        inBuilding = inGarrison.InBuilding,
                        priorMass = inGarrison.PriorMass,
                    });
                    ECB.AddComponent(index, saveEntity,
                        new SeSingleId { value = GlobalSingleIdLookup[selfEntity].value });
                }
            }
            else if (generalAttr.BaseTag == BaseTag.Buildings)
            {
                // Save city task unique id
                if (CityTaskUniqueIdLookup.TryGetComponent(selfEntity, out var cityTaskUniqueId))
                {
                    ECB.AddComponent(index, saveEntity, cityTaskUniqueId);
                }

                // Save garrison data
                if (GarrisonEntitiesLookup.TryGetBuffer(selfEntity, out var garrisonEntities)
                    && garrisonEntities.Length > 0)
                {
                    var garrisonTypeDataBuffer = GarrisonTypeDataLookup[selfEntity];
                    ECB.AddBuffer<GarrisonTypeData>(index, saveEntity);
                    foreach (var typeData in garrisonTypeDataBuffer)
                    {
                        ECB.AppendToBuffer(index, saveEntity, typeData);
                    }

                    ECB.AddBuffer<SeGarrisonEntity>(index, saveEntity);
                    foreach (var garrisonEntity in garrisonEntities)
                    {
                        ECB.AppendToBuffer(index, saveEntity, new SeGarrisonEntity
                        {
                            unitTmpId = GlobalSingleIdLookup[garrisonEntity.Value].value,
                            id = garrisonEntity.Id
                        });
                    }

                    ECB.AddComponent(index, saveEntity,
                        new SeSingleId { value = GlobalSingleIdLookup[selfEntity].value });
                }

                // Save conjuring data
                if (ConjuringDataLookup.TryGetBuffer(selfEntity, out var conjuringDataBuffer) &&
                    conjuringDataBuffer.Length > 0)
                {
                    ECB.AddBuffer<SeConjuringData>(index, saveEntity);
                    foreach (var conjuringData in conjuringDataBuffer)
                    {
                        ECB.AppendToBuffer(index, saveEntity, new SeConjuringData
                        {
                            accumulatedHours = conjuringData.LastCheckTotalHours,
                            conjuredAmount = conjuringData.ConjuredAmount,
                            remainingTimeHours = conjuringData.ThisTaskRemainingTime,
                            targetAmount = conjuringData.TargetAmount,
                            unitGlobalId = conjuringData.UnitGlobalId
                        });
                    }
                }

                // Save constructing timer data
                if (ConstructingTimerLookup.TryGetComponent(selfEntity, out var constructingTimer))
                {
                    ECB.AddComponent(index, saveEntity, constructingTimer);
                }
            }
        }
    }

    [BurstCompile]
    public partial struct LoadArmyGroupSubDataJob : IJobEntity
    {
        public EntityCommandBuffer.ParallelWriter ECB;
        [ReadOnly] public NativeHashMap<int, Entity> GlobalIdxToPrefabs;
        [ReadOnly] public NativeHashMap<int, ExpStaticConfig> ExpDatabase;

        [ReadOnly] public ComponentLookup<MovableData> MovableDataLookup;
        [ReadOnly] public ComponentLookup<AttackAbility> AttackAbilityLookup;
        [ReadOnly] public ComponentLookup<HealAbility> HealAbilityLookup;
        [ReadOnly] public ComponentLookup<HarvestAbility> HarvestAbilityLookup;

        private void Execute([ChunkIndexInQuery] int index, in SeTransform seTransform, in SePrefabId sePrefabId,
            in SeSingleId seSingleId, in ExpData expData, in StatData statData,
            in SeInArmyGroup seInArmyGroup,
            Entity selfEntity)
        {
            ECB.DestroyEntity(index, selfEntity);
            var instance = ECB.Instantiate(index, GlobalIdxToPrefabs[sePrefabId.value]);
            ECB.AddComponent<SubGameplayEntityTag>(index, instance);
            ECB.AddComponent(index, instance, seSingleId);
            ECB.AddComponent(index, instance, seInArmyGroup);
            ECB.SetComponent(index, instance, new LocalTransform
            {
                Position = seTransform.position,
                Rotation = seTransform.rotation,
                Scale = seTransform.scale,
            });
            ECB.SetComponent(index, instance, statData);
            ECB.SetComponent(index, instance, expData);


            var expStaticConfig = ExpDatabase[sePrefabId.value];

            // Modify move speed based on exp level
            var movableData = MovableDataLookup[GlobalIdxToPrefabs[sePrefabId.value]];
            movableData.MoveSpeed += (expData.curLevel - 1) * expStaticConfig.MoveSpeedPerLevel;
            ECB.SetComponent(index, instance, movableData);

            // Modify abilities based on exp level
            if (AttackAbilityLookup.TryGetComponent(GlobalIdxToPrefabs[sePrefabId.value], out var attackAbility))
            {
                attackAbility.Amount += (expData.curLevel - 1) * expStaticConfig.AttackAmountPerLevel;
                attackAbility.Speed += (expData.curLevel - 1) * expStaticConfig.AttackSpeedPerLevel;
                attackAbility.Targets += (expData.curLevel - 1) * expStaticConfig.AttackTargetsPerLevel;
                attackAbility.Range += (expData.curLevel - 1) * expStaticConfig.AttackRangePerLevel;
                ECB.SetComponent(index, instance, attackAbility);
            }

            if (HealAbilityLookup.TryGetComponent(GlobalIdxToPrefabs[sePrefabId.value], out var healAbility))
            {
                healAbility.Amount += (expData.curLevel - 1) * expStaticConfig.HealAmountPerLevel;
                healAbility.Speed += (expData.curLevel - 1) * expStaticConfig.HealSpeedPerLevel;
                healAbility.Targets += (expData.curLevel - 1) * expStaticConfig.HealTargetsPerLevel;
                healAbility.Range += (expData.curLevel - 1) * expStaticConfig.HealRangePerLevel;
                ECB.SetComponent(index, instance, healAbility);
            }

            if (HarvestAbilityLookup.TryGetComponent(GlobalIdxToPrefabs[sePrefabId.value], out var harvestAbility))
            {
                harvestAbility.Amount += (expData.curLevel - 1) * expStaticConfig.HarvestAmountPerLevel;
                harvestAbility.Speed += (expData.curLevel - 1) * expStaticConfig.HarvestSpeedPerLevel;
                harvestAbility.Targets += (expData.curLevel - 1) * expStaticConfig.HarvestTargetsPerLevel;
                harvestAbility.Range += (expData.curLevel - 1) * expStaticConfig.HarvestRangePerLevel;
                ECB.SetComponent(index, instance, harvestAbility);
            }
        }
    }

    [BurstCompile]
    public partial struct LoadSubGameplayJob : IJobEntity
    {
        public EntityCommandBuffer.ParallelWriter ECB;

        // Component lookups
        [ReadOnly] public ComponentLookup<ExpData> ExpLookup;
        [ReadOnly] public ComponentLookup<SeInGarrison> InGarrisonLookup;
        [ReadOnly] public ComponentLookup<SeInverseMass> InverseMassLookup;
        [ReadOnly] public ComponentLookup<PhysicsMass> PhysicsMassLookup;
        [ReadOnly] public ComponentLookup<ConstructingTimer> ConstructingTimerLookup;
        [ReadOnly] public ComponentLookup<CityTaskUniqueId> CityTaskUniqueIdLookup;

        [ReadOnly] public ComponentLookup<MovableData> MovableDataLookup;
        [ReadOnly] public ComponentLookup<AttackAbility> AttackAbilityLookup;
        [ReadOnly] public ComponentLookup<HealAbility> HealAbilityLookup;
        [ReadOnly] public ComponentLookup<HarvestAbility> HarvestAbilityLookup;

        [ReadOnly] public ComponentLookup<SeSingleId> TmpIdLookup;

        // Buffer lookups
        [ReadOnly] public BufferLookup<SeGarrisonEntity> GarrisonEntitiesLookup;
        [ReadOnly] public BufferLookup<GarrisonTypeData> GarrisonTypeDataLookup;
        [ReadOnly] public BufferLookup<SeConjuringData> SeConjuringDataLookup;

        [ReadOnly] public NativeHashMap<int, Entity> GlobalIdxToPrefabs;
        [ReadOnly] public NativeHashMap<int, ExpStaticConfig> ExpDatabase;

        private void Execute([ChunkIndexInQuery] int index,
            in SePrefabId prefabId, in SeTransform transform, in StatData statData, Entity selfEntity)
        {
            ECB.DestroyEntity(index, selfEntity);

            // Create instance and set general data
            var instance = ECB.Instantiate(index, GlobalIdxToPrefabs[prefabId.value]);
            ECB.AddComponent<SubGameplayEntityTag>(index, instance);
            ECB.SetComponent(index, instance, new LocalTransform
            {
                Position = transform.position,
                Rotation = transform.rotation,
                Scale = transform.scale,
            });
            ECB.SetComponent(index, instance, statData);

            // Set exp data for unit. Building don't need to, because it doesn't gain exp 
            if (ExpLookup.TryGetComponent(selfEntity, out var expData))
            {
                ECB.SetComponent(index, instance, expData);
                var expStaticConfig = ExpDatabase[prefabId.value];

                // Modify move speed based on exp level
                var movableData = MovableDataLookup[GlobalIdxToPrefabs[prefabId.value]];
                movableData.MoveSpeed += (expData.curLevel - 1) * expStaticConfig.MoveSpeedPerLevel;
                ECB.SetComponent(index, instance, movableData);

                // Modify abilities based on exp level
                if (AttackAbilityLookup.TryGetComponent(GlobalIdxToPrefabs[prefabId.value], out var attackAbility))
                {
                    attackAbility.Amount += (expData.curLevel - 1) * expStaticConfig.AttackAmountPerLevel;
                    attackAbility.Speed += (expData.curLevel - 1) * expStaticConfig.AttackSpeedPerLevel;
                    attackAbility.Targets += (expData.curLevel - 1) * expStaticConfig.AttackTargetsPerLevel;
                    attackAbility.Range += (expData.curLevel - 1) * expStaticConfig.AttackRangePerLevel;
                    ECB.SetComponent(index, instance, attackAbility);
                }

                if (HealAbilityLookup.TryGetComponent(GlobalIdxToPrefabs[prefabId.value], out var healAbility))
                {
                    healAbility.Amount += (expData.curLevel - 1) * expStaticConfig.HealAmountPerLevel;
                    healAbility.Speed += (expData.curLevel - 1) * expStaticConfig.HealSpeedPerLevel;
                    healAbility.Targets += (expData.curLevel - 1) * expStaticConfig.HealTargetsPerLevel;
                    healAbility.Range += (expData.curLevel - 1) * expStaticConfig.HealRangePerLevel;
                    ECB.SetComponent(index, instance, healAbility);
                }

                if (HarvestAbilityLookup.TryGetComponent(GlobalIdxToPrefabs[prefabId.value], out var harvestAbility))
                {
                    harvestAbility.Amount += (expData.curLevel - 1) * expStaticConfig.HarvestAmountPerLevel;
                    harvestAbility.Speed += (expData.curLevel - 1) * expStaticConfig.HarvestSpeedPerLevel;
                    harvestAbility.Targets += (expData.curLevel - 1) * expStaticConfig.HarvestTargetsPerLevel;
                    harvestAbility.Range += (expData.curLevel - 1) * expStaticConfig.HarvestRangePerLevel;
                    ECB.SetComponent(index, instance, harvestAbility);
                }
            }

            // Set garrison unit data
            if (InGarrisonLookup.TryGetComponent(selfEntity, out var inGarrison))
            {
                ECB.AddComponent(index, instance, inGarrison);
                var physicsMass = PhysicsMassLookup[GlobalIdxToPrefabs[prefabId.value]];
                physicsMass.InverseMass = InverseMassLookup[selfEntity].value;
                ECB.SetComponent(index, instance, physicsMass);
                ECB.SetComponentEnabled<GarrisonStateTag>(index, instance, true);
                ECB.SetComponent(index, instance, new BasicStateData
                {
                    TargetEntity = Entity.Null,
                    CurState = InteractState.Garrison,
                    TargetState = InteractState.Idle
                });
            }

            // Set garrisoned building buffer
            if (GarrisonEntitiesLookup.TryGetBuffer(selfEntity, out var garrisonEntities))
            {
                ECB.AddBuffer<SeGarrisonEntity>(index, instance);
                foreach (var garrisonEntity in garrisonEntities)
                {
                    ECB.AppendToBuffer(index, instance, garrisonEntity);
                }

                foreach (var typeData in GarrisonTypeDataLookup[selfEntity])
                {
                    ECB.AppendToBuffer(index, instance, typeData);
                }
            }

            // Set conjuring data
            if (SeConjuringDataLookup.TryGetBuffer(selfEntity, out var seConjuringDatas))
            {
                ECB.AddComponent<ConjuringTag>(index, instance);
                foreach (var seConjuringData in seConjuringDatas)
                {
                    ECB.AppendToBuffer(index, instance, new ConjuringData
                    {
                        UnitGlobalId = seConjuringData.unitGlobalId,
                        TargetAmount = seConjuringData.targetAmount,
                        ConjuredAmount = seConjuringData.conjuredAmount,
                        ThisTaskRemainingTime = seConjuringData.remainingTimeHours,
                        LastCheckTotalHours = seConjuringData.accumulatedHours,
                        ConjuringEntity = GlobalIdxToPrefabs[seConjuringData.unitGlobalId]
                    });
                }
            }

            // Set constructing timer data
            if (ConstructingTimerLookup.TryGetComponent(selfEntity, out var constructingTimer))
            {
                ECB.AddComponent(index, instance, constructingTimer);
            }

            // Set city task unique id
            if (CityTaskUniqueIdLookup.TryGetComponent(selfEntity, out var cityTaskUniqueId))
            {
                ECB.AddComponent(index, instance, cityTaskUniqueId);
            }

            // Set tmp id
            if (TmpIdLookup.TryGetComponent(selfEntity, out var tmpId))
            {
                ECB.AddComponent(index, instance, tmpId);
            }
        }
    }

    [BurstCompile]
    [WithAll(typeof(SeSingleId))]
    public partial struct SubGameplayReplaceTmpIdJob : IJobEntity
    {
        public EntityCommandBuffer.ParallelWriter ECB;
        [ReadOnly] public NativeHashMap<long, Entity> TmpIdxToInstances;
        [ReadOnly] public ComponentLookup<SeInGarrison> SeInGarrisonLookup;
        [ReadOnly] public BufferLookup<SeGarrisonEntity> SeGarrisonEntitiesLookup;

        private void Execute([ChunkIndexInQuery] int index, Entity selfEntity)
        {
            ECB.RemoveComponent<SeSingleId>(index, selfEntity);
            if (SeInGarrisonLookup.TryGetComponent(selfEntity, out var inGarrison))
            {
                ECB.AddComponent(index, selfEntity, new InGarrison
                {
                    BuildingEntity = TmpIdxToInstances[inGarrison.buildingSingleId],
                    InBuilding = inGarrison.inBuilding,
                    PriorMass = inGarrison.priorMass,
                });
                ECB.RemoveComponent<SeInGarrison>(index, selfEntity);
            }

            if (SeGarrisonEntitiesLookup.TryGetBuffer(selfEntity, out var garrisonEntities))
            {
                foreach (var seGarrison in garrisonEntities)
                {
                    ECB.AppendToBuffer(index, selfEntity, new GarrisonEntity
                    {
                        Id = seGarrison.id,
                        Value = TmpIdxToInstances[seGarrison.unitTmpId]
                    });
                }

                ECB.RemoveComponent<SeGarrisonEntity>(index, selfEntity);
            }
        }
    }

    [BurstCompile]
    [WithAll(typeof(SeSingleId))]
    public partial struct ArmyGroupSubDataReplaceUnitTmpIdJob : IJobEntity
    {
        public EntityCommandBuffer.ParallelWriter ECB;
        [ReadOnly] public NativeHashMap<long, Entity> TmpIdxToInstances;

        private void Execute([ChunkIndexInQuery] int index, in SeInArmyGroup seInArmyGroup, in SeSingleId seSingleId,
            Entity selfEntity)
        {
            ECB.RemoveComponent<SeSingleId>(index, selfEntity);
            ECB.RemoveComponent<SeInArmyGroup>(index, selfEntity);
            ECB.AddComponent(index, selfEntity, new InArmyGroup
            {
                BelongsTo = TmpIdxToInstances[seInArmyGroup.armyGroupSaveId]
            });
        }
    }

    [BurstCompile]
    [WithAll(typeof(InSubGameTag))]
    public partial struct ArmyGroupSubDataReplaceArmyGroupTmpIdJob : IJobEntity
    {
        [ReadOnly] public NativeHashMap<long, Entity> TmpIdxToInstances;

        private void Execute(ref DynamicBuffer<ArmyGroupUnit> units)
        {
            for (var i = 0; i < units.Length; i++)
            {
                var unit = units[i];
                unit.Unit = TmpIdxToInstances[unit.SaveTmpId];
                units[i] = unit;
            }
        }
    }

    [BurstCompile]
    public partial struct ArmyGroupSetUnitsRelativePositionJob : IJobEntity
    {
        [ReadOnly] public ComponentLookup<ArmyGroupAttr> ArmyGroupAttrLookup;

        private void Execute(ref LocalTransform transform, in InArmyGroup inArmyGroup)
        {
            var position = transform.Position;
            var armyGroupAttr = ArmyGroupAttrLookup[inArmyGroup.BelongsTo];
            position = armyGroupAttr.loadingCenter + armyGroupAttr.loadingScale * position;
            transform.Position = position;
        }
    }
}