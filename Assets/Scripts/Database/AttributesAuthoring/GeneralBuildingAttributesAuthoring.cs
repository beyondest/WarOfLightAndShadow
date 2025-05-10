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
        protected class Baker : GeneralDataItemBaker<GeneralBuildingAttributesAuthoring>
        {
            public override void Bake(GeneralBuildingAttributesAuthoring authoring)
            {
                if(authoring.globalIdx == 0)return;
                var item = DatabaseManager.BuildingDatabaseSo.GetItemById(authoring.globalIdx);
                
                var entity = GetEntity(TransformUsageFlags.Dynamic);
                BakeGeneralDataItem(entity, item);

                AddComponent(entity, new BuildingAttr
                {
                    SubTypeIndex = item.GetSubtypeIndex(),
                    Type = item.type,
                });

                var buffer = AddBuffer<CostList>(entity);
                foreach (var cost in item.costs)
                {
                    buffer.Add(new CostList
                    {
                        Amount = cost.amount,
                        Type = cost.costResourceType
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
                }
            }

            private void BakeGenerateAttr(BuildingDataItem item, Entity entity)
            {
                if (item is not GeneratorData generatorData) return;
                AddComponent(entity,new GenerateAttr
                {
                    GenerateResourceType = generatorData.generateResourceType,
                    GenerateInitialSpeed = generatorData.initGenerateSpeed,
                    MaxGenerateSpeed = generatorData.maxGenerateSpeed,
                    CurGenerateSpeed = generatorData.initGenerateSpeed,
                    MinCultivatorsRequireToGenerate = generatorData.minCultivatorCounts
                });
                AddComponent<GenerateData>(entity);
            }

            private void BakeConjureAttr(BuildingDataItem item, Entity entity)
            {
                if(item is not ConjuringShrineData conjuringData) return;
                AddComponent(entity, new ConjureAttr
                {
                    ConjuringType = conjuringData.conjureUnitType,
                    ConjurePositionBias = conjuringData.conjurePositionBias,
                });
                AddBuffer<ConjuringData>(entity);
            }

            private void BakeDwellingAttr(BuildingDataItem item, Entity entity)
            {
                if(item is not DwellingData data) return;
                AddComponent(entity, new DwellingAttr
                {
                    ResourceType = data.dwellingResourceType,
                    Amount = data.dwellingAmount
                });
                AddComponent<DwellingGeneratePopulationTag>(entity);
            }

            private void BakeOrnamentAttr(BuildingDataItem item, Entity entity)
            {
                if(item is not OrnamentData ornamentData) return;
                if (ornamentData.hasBuff)
                {
                    AddComponent(entity, new StaticBuffAttr
                    {
                        Type = ornamentData.ornamentBuffType,
                        RangeSq = ornamentData.buffRange * ornamentData.buffRange,
                        LastSeconds = ornamentData.buffLastTimeSeconds
                    });
                }
                if (ornamentData.GetSubtypeIndex() == (int)OrnamentType.Crystal)
                {
                    AddComponent(entity, new CoreCrystalTag
                    {
                        Faction = item.factionTag
                    });
                }
            }
        }
    }
}

