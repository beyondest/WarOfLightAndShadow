using System;
using SparFlame.GamePlaySystem.Building;
using SparFlame.GamePlaySystem.Conjure;
using SparFlame.GamePlaySystem.Garrison;
using SparFlame.GamePlaySystem.General;
using SparFlame.GamePlaySystem.Generate;
using SparFlame.GamePlaySystem.Interact;
using SparFlame.GamePlaySystem.Resource;
using Unity.Entities;

namespace SparFlame.Database
{
    public class GeneralBuildingAttributesAuthoring : GeneralDataItemAuthoring
    {
        public bool ifInBuildingPack;

        protected class Baker : GeneralDataItemBaker<GeneralBuildingAttributesAuthoring>
        {
            public override void Bake(GeneralBuildingAttributesAuthoring authoring)
            {
                if (authoring.globalIdx == 0) return;
                var item = DatabaseManager.BuildingDatabaseSo.GetItemById(authoring.globalIdx);
                var entity = GetEntity(authoring.ifInBuildingPack
                    ? TransformUsageFlags.WorldSpace
                    : TransformUsageFlags.Dynamic);
                // var entity = GetEntity(TransformUsageFlags.Dynamic);
                
                BakeGeneralDataItem(entity, item);

                AddComponent(entity, new BuildingAttr
                {
                    SubTypeIndex = item.GetSubtypeIndex(),
                    Type = item.type,
                    ConstructTime = item.constructTime
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
                AddComponent<GenerateData>(entity);
                switch (generatorData.generatorType)
                {
                    case GeneratorType.PlantGenerator:
                        AddComponent(entity, new PlantGenerateAttr
                        {
                            GenerateResourceType = generatorData.generateResourceType,
                            GenerateSpeed = generatorData.generateSpeed
                        });
                        break;
                    case GeneratorType.ResourceMine:
                        AddComponent(entity, new ResourceMineGenerateAttr
                        {
                            GenerateResourceType = generatorData.generateResourceType,
                            CurGenerateSpeed = generatorData.generateSpeed,
                            MinCultivatorsRequireToGenerate = generatorData.minWorkersCount < 1 ? 1 : generatorData.minWorkersCount
                        });
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
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
                if (item is not DwellingData data) return;
                AddComponent(entity, new DwellingAttr
                {
                    ResourceType = data.dwellingResourceType,
                    Amount = data.dwellingAmount
                });
                AddComponent<DwellingGeneratePopulationTag>(entity);
                SetComponentEnabled<DwellingGeneratePopulationTag>(entity,true);
            }

            private void BakeOrnamentAttr(BuildingDataItem item, Entity entity)
            {
                if (item is not OrnamentData ornamentData) return;
                if (ornamentData.hasBuff)
                {
                    
                }
                if (ornamentData.ornamentType is OrnamentType.Crystal or OrnamentType.Beacon)
                {
                    AddComponent(entity, new CrystalDef
                    {
                        Faction = item.factionTag
                    });
           
                }
                if (item.factionTag == FactionTag.Ally && ornamentData.ornamentType == OrnamentType.Crystal)
                {
                    AddComponent<LightSingleCrystalTag>(entity);
                }
            }
        }
    }
}