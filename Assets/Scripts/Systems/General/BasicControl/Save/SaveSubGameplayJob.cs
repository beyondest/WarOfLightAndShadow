using System;
using SparFlame.Components.General;
using SparFlame.Components.MainGameplay;
using SparFlame.Components.SubGameplay;
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
        public long buildingTmpId;
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
    public struct SeTmpId : IComponentData
    {
        public long value;
    }

    [Serializable]
    public struct SeInArmyGroup : IComponentData
    {
        public long armyGroupSaveId;
    }


    [BurstCompile]
    [WithNone(typeof(InArmyGroup))]
    public partial struct SaveSubGameplayJob : IJobEntity
    {
        public EntityCommandBuffer.ParallelWriter ECB;

        [ReadOnly] public BufferLookup<GarrisonEntity> GarrisonEntitiesLookup;
        [ReadOnly] public BufferLookup<GarrisonTypeData> GarrisonTypeDataLookup;
        [ReadOnly] public ComponentLookup<InGarrison> InGarrisonLookup;
        [ReadOnly] public ComponentLookup<PhysicsMass> PhysicsMassLookup;

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
            ECB.AddComponent(index, saveEntity, new SeGlobalId { value = generalAttr.ID });
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
                        buildingTmpId = SaveUtilities.GetTmpIdForSaving(inGarrison.BuildingEntity),
                        inBuilding = inGarrison.InBuilding,
                        priorMass = inGarrison.PriorMass,
                    });
                    ECB.AddComponent(index, saveEntity,
                        new SeTmpId { value = SaveUtilities.GetTmpIdForSaving(selfEntity) });
                }
            }
            else if (generalAttr.BaseTag == BaseTag.Buildings)
            {
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
                            unitTmpId = SaveUtilities.GetTmpIdForSaving(garrisonEntity.Value),
                            id = garrisonEntity.Id
                        });
                    }

                    ECB.AddComponent(index, saveEntity,
                        new SeTmpId { value = SaveUtilities.GetTmpIdForSaving(selfEntity) });
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

        private void Execute([ChunkIndexInQuery] int index, in SeTransform seTransform, in SeGlobalId seGlobalId,
            in SeTmpId seTmpId, in ExpData expData, in StatData statData,
            in SeInArmyGroup seInArmyGroup,
            Entity selfEntity)
        {
            ECB.DestroyEntity(index, selfEntity);
            var instance = ECB.Instantiate(index, GlobalIdxToPrefabs[seGlobalId.value]);
            ECB.AddComponent<SubGameplayEntityTag>(index, instance);
            ECB.AddComponent(index, instance, seTmpId);
            ECB.AddComponent(index, instance, seInArmyGroup);
            ECB.SetComponent(index, instance, new LocalTransform
            {
                Position = seTransform.position,
                Rotation = seTransform.rotation,
                Scale = seTransform.scale,
            });
            ECB.SetComponent(index, instance, statData);
            ECB.SetComponent(index, instance, expData);


            var expStaticConfig = ExpDatabase[seGlobalId.value];

            // Modify move speed based on exp level
            var movableData = MovableDataLookup[GlobalIdxToPrefabs[seGlobalId.value]];
            movableData.MoveSpeed += expData.curLevel * expStaticConfig.MoveSpeedPerLevel;
            ECB.SetComponent(index, instance, movableData);

            // Modify abilities based on exp level
            if (AttackAbilityLookup.TryGetComponent(GlobalIdxToPrefabs[seGlobalId.value], out var attackAbility))
            {
                attackAbility.Amount += expData.curLevel * expStaticConfig.AttackAmountPerLevel;
                attackAbility.Speed += expData.curLevel * expStaticConfig.AttackSpeedPerLevel;
                attackAbility.Targets += expData.curLevel * expStaticConfig.AttackTargetsPerLevel;
                attackAbility.Range += expData.curLevel * expStaticConfig.AttackRangePerLevel;
                ECB.SetComponent(index, instance, attackAbility);
            }

            if (HealAbilityLookup.TryGetComponent(GlobalIdxToPrefabs[seGlobalId.value], out var healAbility))
            {
                healAbility.Amount += expData.curLevel * expStaticConfig.HealAmountPerLevel;
                healAbility.Speed += expData.curLevel * expStaticConfig.HealSpeedPerLevel;
                healAbility.Targets += expData.curLevel * expStaticConfig.HealTargetsPerLevel;
                healAbility.Range += expData.curLevel * expStaticConfig.HealRangePerLevel;
                ECB.SetComponent(index, instance, healAbility);
            }

            if (HarvestAbilityLookup.TryGetComponent(GlobalIdxToPrefabs[seGlobalId.value], out var harvestAbility))
            {
                harvestAbility.Amount += expData.curLevel * expStaticConfig.HarvestAmountPerLevel;
                harvestAbility.Speed += expData.curLevel * expStaticConfig.HarvestSpeedPerLevel;
                harvestAbility.Targets += expData.curLevel * expStaticConfig.HarvestTargetsPerLevel;
                harvestAbility.Range += expData.curLevel * expStaticConfig.HarvestRangePerLevel;
                ECB.SetComponent(index, instance, harvestAbility);
            }
        }
    }

    [BurstCompile]
    public partial struct LoadSubGameplayJob : IJobEntity
    {
        public EntityCommandBuffer.ParallelWriter ECB;
        [ReadOnly] public ComponentLookup<ExpData> ExpLookup;
        [ReadOnly] public ComponentLookup<SeInGarrison> InGarrisonLookup;
        [ReadOnly] public ComponentLookup<SeInverseMass> InverseMassLookup;
        [ReadOnly] public ComponentLookup<PhysicsMass> PhysicsMassLookup;

        // Upgrade component lookups
        [ReadOnly] public ComponentLookup<MovableData> MovableDataLookup;
        [ReadOnly] public ComponentLookup<AttackAbility> AttackAbilityLookup;
        [ReadOnly] public ComponentLookup<HealAbility> HealAbilityLookup;
        [ReadOnly] public ComponentLookup<HarvestAbility> HarvestAbilityLookup;

        [ReadOnly] public ComponentLookup<SeTmpId> TmpIdLookup;

        [ReadOnly] public BufferLookup<SeGarrisonEntity> GarrisonEntitiesLookup;
        [ReadOnly] public BufferLookup<GarrisonTypeData> GarrisonTypeDataLookup;

        [ReadOnly] public NativeHashMap<int, Entity> GlobalIdxToPrefabs;
        [ReadOnly] public NativeHashMap<int, ExpStaticConfig> ExpDatabase;

        private void Execute([ChunkIndexInQuery] int index,
            in SeGlobalId globalId, in SeTransform transform, in StatData statData, Entity selfEntity)
        {
            ECB.DestroyEntity(index, selfEntity);

            // Create instance and set general data
            var instance = ECB.Instantiate(index, GlobalIdxToPrefabs[globalId.value]);
            ECB.AddComponent<SubGameplayEntityTag>(index, instance);
            ECB.SetComponent(index, instance, new LocalTransform
            {
                Position = transform.position,
                Rotation = transform.rotation,
                Scale = transform.scale,
            });
            ECB.SetComponent(index, instance, statData);

            // Set exp data for unit. Building don't need to, because it doesn't gain exp 
            if (ExpLookup.TryGetComponent(selfEntity, out var exp))
            {
                ECB.SetComponent(index, instance, exp);
                var expStaticConfig = ExpDatabase[globalId.value];

                // Modify move speed based on exp level
                var movableData = MovableDataLookup[GlobalIdxToPrefabs[globalId.value]];
                movableData.MoveSpeed += exp.curLevel * expStaticConfig.MoveSpeedPerLevel;
                ECB.SetComponent(index, instance, movableData);

                // Modify abilities based on exp level
                if (AttackAbilityLookup.TryGetComponent(GlobalIdxToPrefabs[globalId.value], out var attackAbility))
                {
                    attackAbility.Amount += exp.curLevel * expStaticConfig.AttackAmountPerLevel;
                    attackAbility.Speed += exp.curLevel * expStaticConfig.AttackSpeedPerLevel;
                    attackAbility.Targets += exp.curLevel * expStaticConfig.AttackTargetsPerLevel;
                    attackAbility.Range += exp.curLevel * expStaticConfig.AttackRangePerLevel;
                    ECB.SetComponent(index, instance, attackAbility);
                }

                if (HealAbilityLookup.TryGetComponent(GlobalIdxToPrefabs[globalId.value], out var healAbility))
                {
                    healAbility.Amount += exp.curLevel * expStaticConfig.HealAmountPerLevel;
                    healAbility.Speed += exp.curLevel * expStaticConfig.HealSpeedPerLevel;
                    healAbility.Targets += exp.curLevel * expStaticConfig.HealTargetsPerLevel;
                    healAbility.Range += exp.curLevel * expStaticConfig.HealRangePerLevel;
                    ECB.SetComponent(index, instance, healAbility);
                }

                if (HarvestAbilityLookup.TryGetComponent(GlobalIdxToPrefabs[globalId.value], out var harvestAbility))
                {
                    harvestAbility.Amount += exp.curLevel * expStaticConfig.HarvestAmountPerLevel;
                    harvestAbility.Speed += exp.curLevel * expStaticConfig.HarvestSpeedPerLevel;
                    harvestAbility.Targets += exp.curLevel * expStaticConfig.HarvestTargetsPerLevel;
                    harvestAbility.Range += exp.curLevel * expStaticConfig.HarvestRangePerLevel;
                    ECB.SetComponent(index, instance, harvestAbility);
                }
            }

            // Set garrison unit data
            if (InGarrisonLookup.TryGetComponent(selfEntity, out var inGarrison))
            {
                ECB.AddComponent(index, instance, inGarrison);
                var physicsMass = PhysicsMassLookup[GlobalIdxToPrefabs[globalId.value]];
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

            // Set tmp id
            if (TmpIdLookup.TryGetComponent(selfEntity, out var tmpId))
            {
                ECB.AddComponent(index, instance, tmpId);
            }
        }
    }

    [BurstCompile]
    [WithAll(typeof(SeTmpId))]
    public partial struct SubGameplayReplaceTmpIdJob : IJobEntity
    {
        public EntityCommandBuffer.ParallelWriter ECB;
        [ReadOnly] public NativeHashMap<long, Entity> TmpIdxToInstances;
        [ReadOnly] public ComponentLookup<SeInGarrison> SeInGarrisonLookup;
        [ReadOnly] public BufferLookup<SeGarrisonEntity> SeGarrisonEntitiesLookup;

        private void Execute([ChunkIndexInQuery] int index, Entity selfEntity)
        {
            ECB.RemoveComponent<SeTmpId>(index, selfEntity);
            if (SeInGarrisonLookup.TryGetComponent(selfEntity, out var inGarrison))
            {
                ECB.AddComponent(index, selfEntity, new InGarrison
                {
                    BuildingEntity = TmpIdxToInstances[inGarrison.buildingTmpId],
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
    [WithAll(typeof(SeTmpId))]
    public partial struct ArmyGroupSubDataReplaceUnitTmpIdJob : IJobEntity
    {
        public EntityCommandBuffer.ParallelWriter ECB;
        [ReadOnly] public NativeHashMap<long, Entity> TmpIdxToInstances;

        private void Execute([ChunkIndexInQuery] int index, in SeInArmyGroup seInArmyGroup, in SeTmpId seTmpId,
            Entity selfEntity)
        {
            ECB.RemoveComponent<SeTmpId>(index, selfEntity);
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
    


    /*[BurstCompile]
    [WithAll(typeof(InSubGameTag))]
    public partial struct SaveArmyGroupSubDataJob : IJobEntity
    {
        public EntityCommandBuffer.ParallelWriter ECB;
        [ReadOnly] public ComponentLookup<LocalTransform> TransformLookup;
        [ReadOnly] public ComponentLookup<SubGameplayGeneralAttr> GeneralAttrLookup;
        [ReadOnly] public ComponentLookup<StatData> StatDataLookup;
        [ReadOnly] public ComponentLookup<ExpData> ExpDataLookup;
        [ReadOnly] public ComponentLookup<InGarrison> InGarrisonLookup;
        [ReadOnly] public ComponentLookup<PhysicsMass> PhysicsMassLookup;

        private void Execute([ChunkIndexInQuery] int index, ref DynamicBuffer<ArmyGroupUnit> armyGroupUnits)
        {
            for (var i = 0; i < armyGroupUnits.Length; i++)
            {
                var armyGroupUnit = armyGroupUnits[i];
                var unit = armyGroupUnit.Unit;
                var unitTmpId = SaveUtilities.GetTmpIdForSaving(unit);
                armyGroupUnit.SaveTmpId = unitTmpId;
                armyGroupUnits[i] = armyGroupUnit;

                var saveEntity = ECB.CreateEntity(index);

                var transform = TransformLookup[unit];
                var generalAttr = GeneralAttrLookup[unit];
                var statData = StatDataLookup[unit];
                var expData = ExpDataLookup[unit];
                ECB.AddComponent(index, saveEntity, new SeTransform
                {
                    position = transform.Position,
                    rotation = transform.Rotation,
                    scale = transform.Scale,
                });
                ECB.AddComponent(index, saveEntity, new SeGlobalId { value = generalAttr.ID });
                ECB.AddComponent(index, saveEntity, statData);
                ECB.AddComponent(index, saveEntity, expData);
                if (InGarrisonLookup.TryGetComponent(unit, out var inGarrison))
                {
                    var physicsMass = PhysicsMassLookup[unit];
                    ECB.AddComponent(index, saveEntity, new SeInverseMass { value = physicsMass.InverseMass });
                    ECB.AddComponent(index, saveEntity, new SeInGarrison
                    {
                        buildingTmpId = SaveUtilities.GetTmpIdForSaving(inGarrison.BuildingEntity),
                        inBuilding = inGarrison.InBuilding,
                        priorMass = inGarrison.PriorMass,
                    });
                    ECB.AddComponent(index, saveEntity,
                        new SeTmpId { value = unitTmpId });
                }
            }
        }
    }*/
}