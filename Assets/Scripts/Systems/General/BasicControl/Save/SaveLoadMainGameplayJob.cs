using System;
using SparFlame.Components.General;
using SparFlame.Components.MainGameplay;
using SparFlame.Components.SubGameplay;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Transforms;

namespace SparFlame.Systems.General.BasicControl
{
    [Serializable]
    public struct SeCityGarrisonEntity : IBufferElementData
    {
        public long tmpId;
    }

    [Serializable]
    public struct SeArmyGroupUnit : IBufferElementData
    {
        public long tmpId;
        public int globalId;
    }

    [Serializable]
    public struct SeIsMoving : IComponentData
    {
        public bool value;
    }

    [Serializable]
    public struct SeCalculationEnable : IComponentData
    {
        public bool value;
    }

    [Serializable]
    public struct SeArmyGroupInGarrison : IComponentData
    {
        public long tmpId;
    }

    [BurstCompile]
    public partial struct SaveArmyGroupMainDataJob : IJobEntity
    {
        // Army group component lookup
        [ReadOnly] public ComponentLookup<ArmyGroupMovingTag> ArmyGroupMovingTagLookup;
        [ReadOnly] public ComponentLookup<ArmyGroupCalculateEnable> ArmyGroupCalculateEnableLookup;
        [ReadOnly] public ComponentLookup<ArmyGroupInGarrison> ArmyGroupInGarrisonLookup;
        [ReadOnly] public ComponentLookup<CityAttr> CityAttrLookup;

        public EntityCommandBuffer.ParallelWriter ECB;

        private void Execute([ChunkIndexInQuery] int index, in MainGameplayGeneralAttr generalAttr,
            in LocalTransform transform, in ArmyGroupAttr armyGroupAttr,
            in ArmyGroupMovableData armyGroupMovableData,
            in DynamicBuffer<ArmyGroupMovingTarget> armyGroupMovingTargets,
            in NavAgentComponent navAgentComponent, in ArmyGroupCalculatePathData armyGroupCalculatePathData,
            in PathVisualizeData pathVisualizeData, in DynamicBuffer<WaypointBuffer> waypointBuffer,
            in DynamicBuffer<ArmyGroupFinalWayPoint> armyGroupFinalWayPoints,
            in DynamicBuffer<ArmyGroupUnit> armyGroupUnits,
            in DynamicBuffer<ArmyGroupUnitTypeData> unitTypeDatas,
            in ArmyGroupStateData stateData,
            in LastPassingByCity lastPassingByCity,
            in ArmyGroupStatData armyGroupStatData,
            in HpRegenerateTimer hpRegenerateTimer,
            Entity selfEntity)
        {
            var saveEntity = ECB.CreateEntity(index);
            ECB.AddComponent(index, saveEntity, new SeTransform
            {
                position = transform.Position,
                rotation = transform.Rotation,
                scale = transform.Scale
            });
            // Save army group general data
            ECB.AddComponent(index, saveEntity, generalAttr);
            ECB.AddComponent(index, saveEntity, armyGroupAttr);
            ECB.AddComponent(index, saveEntity, armyGroupStatData);
            // Save army group movable data
            ECB.AddComponent(index, saveEntity, armyGroupMovableData);
            ECB.AddComponent(index, saveEntity, new SeIsMoving
            {
                value = ArmyGroupMovingTagLookup.IsComponentEnabled(selfEntity)
            });
            ECB.AddBuffer<ArmyGroupMovingTarget>(index, saveEntity);
            foreach (var armyGroupMovingTarget in armyGroupMovingTargets)
            {
                ECB.AppendToBuffer(index, saveEntity, armyGroupMovingTarget);
            }

            // Save army group navigation data

            ECB.AddComponent(index, saveEntity, navAgentComponent);
            ECB.AddComponent(index, saveEntity, armyGroupCalculatePathData);
            ECB.AddComponent(index, saveEntity, new SeCalculationEnable
            {
                value = ArmyGroupCalculateEnableLookup.IsComponentEnabled(selfEntity)
            });
            ECB.AddComponent(index, saveEntity, pathVisualizeData);

            ECB.AddBuffer<WaypointBuffer>(index, saveEntity);
            foreach (var waypoint in waypointBuffer)
            {
                ECB.AppendToBuffer(index, saveEntity, waypoint);
            }

            ECB.AddBuffer<ArmyGroupFinalWayPoint>(index, saveEntity);
            foreach (var armyGroupFinalWayPoint in armyGroupFinalWayPoints)
            {
                ECB.AppendToBuffer(index, saveEntity, armyGroupFinalWayPoint);
            }

            // Save army group state data
            ECB.AddComponent(index, saveEntity, stateData);

            // Save army group last passing by data
            ECB.AddComponent(index, saveEntity, new SeLastPassingByCity
            {
                cityGlobalIndex = CityAttrLookup[lastPassingByCity.City].globalId
            });

            // save army group units data
            ECB.AddBuffer<SeArmyGroupUnit>(index, saveEntity);
            foreach (var armyGroupUnit in armyGroupUnits)
            {
                ECB.AppendToBuffer(index, saveEntity, new SeArmyGroupUnit
                {
                    globalId = armyGroupUnit.GlobalId,
                    tmpId = armyGroupUnit.SaveTmpId,
                });
            }

            ECB.AddBuffer<ArmyGroupUnitTypeData>(index, saveEntity);
            foreach (var armyGroupUnitTypeData in unitTypeDatas)
            {
                ECB.AppendToBuffer(index, saveEntity, armyGroupUnitTypeData);
            }

            var hasGarrisonCity = ArmyGroupInGarrisonLookup.TryGetComponent(selfEntity, out var inGarrison);
            // Save army group garrison data
            if (hasGarrisonCity)
            {
                ECB.AddComponent(index, saveEntity, new SeArmyGroupInGarrison
                {
                    tmpId = SaveUtilities.GetTmpIdForSaving(inGarrison.City),
                });
            }
            ECB.AddComponent(index, saveEntity, hpRegenerateTimer);

        }
    }

    [BurstCompile]
    public partial struct SaveCityMainDataJob : IJobEntity
    {
        public EntityCommandBuffer.ParallelWriter ECB;
        [ReadOnly] public ComponentLookup<ArmyGroupAttr> ArmyGroupAttrLookup;

        private void Execute([ChunkIndexInQuery] int index, in MainGameplayGeneralAttr generalAttr,
            in LocalTransform transform, in CityAttr cityAttr,
            in DynamicBuffer<CityGarrisonEntity> cityGarrisonEntities,
            in DynamicBuffer<CityResourceEntry> cityResourceEntries,
            in DynamicBuffer<CityTask> cityTasks,
            in HpRegenerateTimer hpRegenerateTimer,
            Entity selfEntity)
        {
            var saveEntity = ECB.CreateEntity(index);
            ECB.AddComponent(index, saveEntity, new SeTransform
            {
                position = transform.Position,
                rotation = transform.Rotation,
                scale = transform.Scale
            });
            ECB.AddComponent(index, saveEntity, cityAttr);


            ECB.AddBuffer<CityResourceEntry>(index, saveEntity);
            foreach (var cityResourceEntry in cityResourceEntries)
            {
                ECB.AppendToBuffer(index, saveEntity, cityResourceEntry);
            }

            ECB.AddBuffer<CityTask>(index, saveEntity);
            foreach (var cityTask in cityTasks)
            {
                ECB.AppendToBuffer(index, saveEntity, cityTask);
            }

            // Check if it needs to save garrison data
            if (cityGarrisonEntities.Length > 0)
            {
                ECB.AddComponent(index, saveEntity, new SeTmpId
                {
                    value = SaveUtilities.GetTmpIdForSaving(selfEntity)
                });
                ECB.AddBuffer<SeCityGarrisonEntity>(index, saveEntity);
                foreach (var cityGarrisonEntity in cityGarrisonEntities)
                {
                    ECB.AppendToBuffer(index, saveEntity, new SeCityGarrisonEntity
                    {
                        tmpId = ArmyGroupAttrLookup[cityGarrisonEntity.ArmyGroup].saveId
                    });
                }
            }
            ECB.AddComponent(index, saveEntity, hpRegenerateTimer);
        }
    }

    [BurstCompile]
    public partial struct LoadArmyGroupMainDataJob : IJobEntity
    {
        public EntityCommandBuffer.ParallelWriter ECB;
        [ReadOnly] public ArmyGroupConfig ArmyGroupConfig;
        [ReadOnly] public ComponentLookup<SeArmyGroupInGarrison> SeInGarrisonLookup;
        [ReadOnly] public ComponentLookup<SeLastPassingByCity> SeLastPassingByCityLookup;

        private void Execute([ChunkIndexInQuery] int index,
            in MainGameplayGeneralAttr generalAttr,
            in SeTransform transform, in ArmyGroupAttr armyGroupAttr,
            in ArmyGroupMovableData armyGroupMovableData,
            in SeIsMoving seIsMoving, in DynamicBuffer<ArmyGroupMovingTarget> movingTargets,
            in NavAgentComponent navAgentComponent, in ArmyGroupCalculatePathData armyGroupCalculatePathData,
            in SeCalculationEnable calculationEnable, in PathVisualizeData pathVisualizeData,
            in DynamicBuffer<WaypointBuffer> waypointBuffer, in DynamicBuffer<ArmyGroupFinalWayPoint> finalWayPoints,
            in ArmyGroupStateData armyGroupStateData,
            in DynamicBuffer<SeArmyGroupUnit> armyGroupUnits, in DynamicBuffer<ArmyGroupUnitTypeData> unitTypeDatas,
            in ArmyGroupStatData armyGroupStatData,
            in HpRegenerateTimer hpRegenerateTimer,
            Entity selfEntity)
        {
            ECB.DestroyEntity(index, selfEntity);
            var prefab = generalAttr.faction == FactionTag.Light
                ? ArmyGroupConfig.LightArmyGroupPrefab
                : ArmyGroupConfig.DarkArmyGroupPrefab;
            var armyGroup = ECB.Instantiate(index, prefab);
            ECB.AddComponent<MainGameplayEntityTag>(index, armyGroup);
            ECB.SetComponent(index, armyGroup, generalAttr);
            ECB.SetComponent(index, armyGroup, new LocalTransform
            {
                Position = transform.position,
                Rotation = transform.rotation,
                Scale = transform.scale
            });
            ECB.SetComponent(index, armyGroup, armyGroupAttr);
            ECB.SetComponent(index, armyGroup, armyGroupMovableData);
            ECB.SetComponent(index, armyGroup, armyGroupStatData);
            ECB.SetComponentEnabled<ArmyGroupMovingTag>(index, armyGroup, seIsMoving.value);
            foreach (var armyGroupMovingTarget in movingTargets)
            {
                ECB.AppendToBuffer(index, armyGroup, armyGroupMovingTarget);
            }

            ECB.SetComponent(index, armyGroup, navAgentComponent);
            ECB.SetComponent(index, armyGroup, armyGroupCalculatePathData);
            ECB.SetComponentEnabled<ArmyGroupCalculateEnable>(index, armyGroup, calculationEnable.value);
            ECB.SetComponent(index, armyGroup, pathVisualizeData);

            
            foreach (var waypoint in waypointBuffer)
            {
                ECB.AppendToBuffer(index, armyGroup, waypoint);
            }

            foreach (var finalWayPoint in finalWayPoints)
            {
                ECB.AppendToBuffer(index, armyGroup, finalWayPoint);
            }

            ECB.SetComponent(index, armyGroup, armyGroupStateData);
            foreach (var unitTypeData in unitTypeDatas)
            {
                ECB.AppendToBuffer(index, armyGroup, unitTypeData);
            }

            foreach (var armyGroupUnit in armyGroupUnits)
            {
                ECB.AppendToBuffer(index, armyGroup, new ArmyGroupUnit
                {
                    Unit = Entity.Null,
                    GlobalId = armyGroupUnit.globalId,
                    SaveTmpId = armyGroupUnit.tmpId
                });
            }

            var ifAddSeTmpId = false;
            if (SeInGarrisonLookup.TryGetComponent(selfEntity, out var seArmyGroupInGarrison) )
            {
                ifAddSeTmpId = true;
                ECB.AddComponent(index, armyGroup, new SeTmpId { value = armyGroupAttr.saveId });
                ECB.AddComponent(index, armyGroup, seArmyGroupInGarrison);
            }

            if (SeLastPassingByCityLookup.TryGetComponent(selfEntity, out var seLastPassingByCity))
            {
                ECB.AddComponent(index, armyGroup, seLastPassingByCity);
                if(!ifAddSeTmpId)ECB.AddComponent(index, armyGroup, new SeTmpId { value = armyGroupAttr.saveId });
            }
            
            ECB.SetComponent(index, armyGroup, hpRegenerateTimer);
        }
    }

    [BurstCompile]
    public partial struct LoadCityMainDataJob : IJobEntity
    {
        public EntityCommandBuffer.ParallelWriter ECB;
        [ReadOnly] public NativeHashMap<int, Entity> GlobalIdxToPrefabs;
        [ReadOnly] public ComponentLookup<SeTmpId> TmpIdLookup;
        [ReadOnly] public BufferLookup<SeCityGarrisonEntity> CityGarrisonEntitiesLookup;

        private void Execute([ChunkIndexInQuery] int index,
            in SeTransform transform, in CityAttr cityAttr,
            in DynamicBuffer<CityResourceEntry> cityResourceEntries,
            in DynamicBuffer<CityTask> cityTasks,
            in HpRegenerateTimer hpRegenerateTimer,
            Entity selfEntity)
        {
            ECB.DestroyEntity(index, selfEntity);
            var prefab = GlobalIdxToPrefabs[cityAttr.globalId];

            var city = ECB.Instantiate(index, prefab);
            ECB.AddComponent<MainGameplayEntityTag>(index, city);
            ECB.SetComponent(index, city, cityAttr);
            ECB.SetComponent(index, city, new LocalTransform
            {
                Position = transform.position,
                Rotation = transform.rotation,
                Scale = transform.scale
            });


            ECB.SetBuffer<CityResourceEntry>(index, city);
            foreach (var cityResourceEntry in cityResourceEntries)
            {
                ECB.AppendToBuffer(index, city, cityResourceEntry);
            }

            ECB.SetBuffer<CityTask>(index, city);
            foreach (var cityTask in cityTasks)
            {
                ECB.AppendToBuffer(index, city, cityTask);
            }

            if (CityGarrisonEntitiesLookup.TryGetBuffer(selfEntity, out var cityGarrisonEntities))
            {
                ECB.AddBuffer<SeCityGarrisonEntity>(index, city);
                foreach (var cityGarrisonEntity in cityGarrisonEntities)
                {
                    ECB.AppendToBuffer(index, city, cityGarrisonEntity);
                }

                ECB.AddComponent(index, city, TmpIdLookup[selfEntity]);
            }
            
            ECB.SetComponent(index, city, hpRegenerateTimer);
        }
    }

    [BurstCompile]
    [WithAll(typeof(SeTmpId))]
    public partial struct MainGameplayReplaceTmpIdJob : IJobEntity
    {
        public EntityCommandBuffer.ParallelWriter ECB;
        [ReadOnly] public NativeHashMap<long, Entity> TmpIdxToInstances;
        [ReadOnly] public NativeHashMap<int, Entity> CityIdToEntities;
        [ReadOnly] public ComponentLookup<SeArmyGroupInGarrison> SeInGarrisonLookup;
        [ReadOnly] public BufferLookup<SeCityGarrisonEntity> SeGarrisonEntitiesLookup;
        [ReadOnly] public ComponentLookup<SeLastPassingByCity> SeLastPassingByCityLookup;

        private void Execute([ChunkIndexInQuery] int index, Entity selfEntity)
        {
            ECB.RemoveComponent<SeTmpId>(index, selfEntity);
            if (SeInGarrisonLookup.TryGetComponent(selfEntity, out var inGarrison))
            {
                ECB.AddComponent(index, selfEntity, new ArmyGroupInGarrison
                {
                    City = TmpIdxToInstances[inGarrison.tmpId]
                });
                ECB.RemoveComponent<SeArmyGroupInGarrison>(index, selfEntity);
            }

            if (SeGarrisonEntitiesLookup.TryGetBuffer(selfEntity, out var garrisonEntities))
            {
                foreach (var seCityGarrisonEntity in garrisonEntities)
                {
                    ECB.AppendToBuffer(index, selfEntity, new CityGarrisonEntity
                    {
                        ArmyGroup = TmpIdxToInstances[seCityGarrisonEntity.tmpId]
                    });
                }

                ECB.RemoveComponent<SeCityGarrisonEntity>(index, selfEntity);
            }

            if (SeLastPassingByCityLookup.TryGetComponent(selfEntity, out var seLastPassingByCity))
            {
                ECB.SetComponent(index, selfEntity, new LastPassingByCity
                {
                    City = CityIdToEntities[seLastPassingByCity.cityGlobalIndex]
                });
                ECB.RemoveComponent<SeLastPassingByCity>(index, selfEntity);
            }
        }
    }
}