using System;
using SparFlame.Components.General;
using SparFlame.Components.SubGameplay;
using SparFlame.Core.Utils;
using Unity.Entities;

namespace SparFlame.Database
{
    public class GeneralBuildingAttributesAuthoring : GeneralDataItemAuthoring
    {
        protected class Baker : GeneralDataItemBaker<GeneralBuildingAttributesAuthoring>
        {
            public override void Bake(GeneralBuildingAttributesAuthoring authoring)
            {
                if (authoring.globalIdx == 0) return;
            
                var item = DatabaseManager.BuildingDatabaseSo.GetItemById(authoring.globalIdx);
                var entity = GetEntity(TransformUsageFlags.Dynamic);
                // var entity = GetEntity(TransformUsageFlags.Dynamic);
                
                BakeGeneralDataItem(entity, item);

                AddComponent(entity, new BuildingAttr
                {
                    SubTypeIndex = item.GetSubtypeIndex(),
                    Type = item.type,
                    ConstructTimeHours = item.constructTimeHours
                });
                
                var buffer = AddBuffer<CostList>(entity);
                foreach (var cost in item.costs)
                {
                    buffer.Add(new CostList
                    {
                        Amount = cost.amount,
                        Type = cost.type
                    });
                }
                if(!item.isPortalWall)
                    BakeVolumeObstacleAttr(item, entity);
                BakeGarrisonAttr(item, entity);
                BakeGenerateAttr(item, entity);
                BakeConjureAttr(item, entity);
                BakeDwellingAttr(item, entity);
                BakeOrnamentAttr(item, entity);
            }


            private void BakeGarrisonAttr(BuildingDataItem item, Entity entity)
            {
                if (item.IsGarrisonEnable())
                {
                    AddComponent(entity, new GarrisonAttr
                    {
                        MaxGarrisonCount = item.maxGarrisonCount,
                        MoveOutPositionBias = item.outPositionBias,
                    });
                    var positionBias = AddBuffer<GarrisonPositions>(entity);
                    foreach (var posBias in item.positionBias)
                    {
                        positionBias.Add(new GarrisonPositions{PositionBias = posBias});
                    }
                    AddBuffer<GarrisonTypeData>(entity);
                    AddBuffer<GarrisonEntity>(entity);
                    var buffer = AddBuffer<AllowGarrisonUnit>(entity);
                    foreach (var data in item.garrisonUnits)
                    {
                        buffer.Add(new AllowGarrisonUnit
                        {
                            UnitType = data.unitType,
                            SubTypeIndex = data.subTypeIndex
                        });
                    }

                    if (item.type == BuildingType.Fortifications)
                    {
                        AddComponent<BuildingGarrisonBuff>(entity);
                        SetComponentEnabled<BuildingGarrisonBuff>(entity, false);
                    }
                        
                }
            }

            private void BakeGenerateAttr(BuildingDataItem item, Entity entity)
            {
                if (item is not GeneratorData generatorData) return;
                switch (generatorData.generatorType)
                {
                    case GeneratorType.PlantGenerator:
                        AddComponent(entity, new GenerateAttr
                        {
                            GenerateResourceType = generatorData.generateResourceType,
                            GenerateSpeedHoursPerUnit = generatorData.generateSpeedHoursPerUnit,
                            MinCultivatorsRequireToGenerate = 0
                        });
                        break;
                    case GeneratorType.ResourceMine:
                        AddComponent(entity, new GenerateAttr
                        {
                            GenerateResourceType = generatorData.generateResourceType,
                            GenerateSpeedHoursPerUnit = generatorData.generateSpeedHoursPerUnit,
                            MinCultivatorsRequireToGenerate = generatorData.minWorkersCount < 1 ? 1 : generatorData.minWorkersCount
                        });
                        break;
                    default:
                        BurstSafe.UnexpectedEnum(generatorData.generatorType);
                        break;
                }
            }

            private void BakeConjureAttr(BuildingDataItem item, Entity entity)
            {
                if (item is not ConjuringShrineData conjuringData) return;
                AddComponent(entity, new ConjureAttr
                {
                    ConjuringType = conjuringData.conjureUnitType,
                    ConjurePositionBias = conjuringData.conjurePositionBias,
                });
                AddBuffer<ConjuringData>(entity);
            }

            private void BakeDwellingAttr(BuildingDataItem item, Entity entity)
            {
                if (item is not CapacityBuildingsData data) return;
                AddComponent(entity, new CapacityBuildingAttr
                {
                    ResourceType = data.storageResourceType,
                    StorageAmount = data.amount
                });
            }

            private void BakeOrnamentAttr(BuildingDataItem item, Entity entity)
            {
                if (item is not OrnamentData ornamentData) return;
                if (ornamentData.hasBuff)
                {
                    
                }
                if (ornamentData.ornamentType is OrnamentType.Crystal)
                {
                    AddComponent(entity, new CrystalDef());
                }

                if (ornamentData.ornamentType == OrnamentType.RetreatPortal)
                {
                    AddComponent<RetreatPortalTag>(entity);
                }
            }
        }
    }
}