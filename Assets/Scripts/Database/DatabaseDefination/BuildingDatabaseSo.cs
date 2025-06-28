using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using GamePlaySystem.Database;
using Sirenix.OdinInspector;
using SparFlame.Components.General;
using SparFlame.Components.SubGameplay;
using Unity.Mathematics;
using UnityEngine;

// ReSharper disable RedundantJumpStatement

namespace SparFlame.Database
{
    [CreateAssetMenu(fileName = "BuildingDatabase", menuName = "GameData/BuildingDatabase", order = 0)]
    public class BuildingDatabaseSo : GeneralDatabase<BuildingDataItem>
    {
        [TableList(ShowIndexLabels = true, AlwaysExpanded = false), TableColumnWidth(50, Resizable = true),
         SerializeReference, HideLabel, ListDrawerSettings(DraggableItems = true)]
        private List<BuildingDataItem> items;

        [SerializeField, ValueDropdown(nameof(GetTypeOptions))]
        private string selectedTypeName;

        private IEnumerable<string> GetTypeOptions()
        {
            return GetAllTypes().Select(t => t.FullName);
        }

        private IEnumerable<Type> GetAllTypes()
        {
            return Assembly.GetAssembly(typeof(BuildingDataItem))
                .GetTypes()
                .Where(t => t.IsSubclassOf(typeof(BuildingDataItem)) && !t.IsAbstract);
        }

        [Button("Add Building")]
        private void AddSelectedBuilding()
        {
            if (string.IsNullOrEmpty(selectedTypeName))
            {
                Debug.LogWarning("No type selected.");
                return;
            }

            var type = Type.GetType(selectedTypeName);
            if (type == null)
            {
                Debug.LogError($"Type not found: {selectedTypeName}");
                return;
            }

            if (Activator.CreateInstance(type) is BuildingDataItem instance)
            {
                items.Add(instance);
            }
        }


        public override List<BuildingDataItem> Items => items;

        private bool _shouldCheckValid;


        [Button]
        private void CheckBuildingConfigValid()
        {
            foreach (var item in items)
            {
                if (item.HasSight())
                {
                    if (item.IsAttackable() && !Mathf.Approximately(item.attackRange, item.sightRange)
                        || item.IsHealable() && !Mathf.Approximately(item.healRange, item.sightRange)
                        || item.IsHarvestable() && !Mathf.Approximately(item.harvestRange, item.sightRange))
                    {
                        item.attackRange = item.harvestRange = item.healRange = item.sightRange;
                    }
                }
            }

            _shouldCheckValid = false;
            if (_shouldCheckValid) return;
        }

        [Button("Copy Light Data to Dark")]
        private void CopyLightDataToDark()
        {
            var dict = new Dictionary<(BuildingType, int, Tier, int), BuildingDataItem>();
            foreach (var item in items)
            {
                if (item.factionTag == FactionTag.Light)
                    if(!dict.TryAdd((item.type, item.GetSubtypeIndex(), item.curTier, item.GetSubSubTypeIndex()), item))
                        Debug.LogError($"{item.gameplayName} / {item.id} : Duplicate key found");
            }

            foreach (var item in items)
            {
                if (item.factionTag == FactionTag.Dark)
                {
                    if (dict.TryGetValue((item.type, item.GetSubtypeIndex(), item.curTier, item.GetSubSubTypeIndex()), out var lightItem))
                    {
                        item.stat = lightItem.stat;
                        item.statPerLevel = lightItem.statPerLevel;
                        item.expMaxValue = lightItem.expMaxValue;
                        item.expGainPerLevel = lightItem.expGainPerLevel;
                        item.maxLevel = lightItem.maxLevel;

                        item.attackAmount = lightItem.attackAmount;
                        item.attackAmountPerLevel = lightItem.attackAmountPerLevel;
                        item.attackSpeed = lightItem.attackSpeed;
                        item.attackSpeedPerLevel = lightItem.attackSpeedPerLevel;
                        item.attackRange = lightItem.attackRange;
                        item.attackRangePerLevel = lightItem.attackRangePerLevel;
                        item.attackTargets = lightItem.attackTargets;
                        item.attackTargetsPerLevel = lightItem.attackTargetsPerLevel;

                        item.healAmount = lightItem.healAmount;
                        item.healAmountPerLevel = lightItem.healAmountPerLevel;
                        item.healSpeed = lightItem.healSpeed;
                        item.healSpeedPerLevel = lightItem.healSpeedPerLevel;
                        item.healRange = lightItem.healRange;
                        item.healRangePerLevel = lightItem.healRangePerLevel;
                        item.healTargets = lightItem.healTargets;
                        item.healTargetsPerLevel = lightItem.healTargetsPerLevel;

                        item.harvestAmount = lightItem.harvestAmount;
                        item.harvestAmountPerLevel = lightItem.harvestAmountPerLevel;
                        item.harvestSpeed = lightItem.harvestSpeed;
                        item.harvestSpeedPerLevel = lightItem.harvestSpeedPerLevel;
                        item.harvestRange = lightItem.harvestRange;
                        item.harvestRangePerLevel = lightItem.harvestRangePerLevel;
                        item.harvestTargets = lightItem.harvestTargets;
                        item.harvestTargetsPerLevel = lightItem.harvestTargetsPerLevel;

                        item.costs = new List<CostResourceTypeAmountPair>();
                        foreach (var cost in lightItem.costs)
                        {
                            var costCopy = cost;
                            if (ResourceUtils.GetCorrespondingResource(cost.type, out var correspondingResourceType))
                                costCopy.type = correspondingResourceType;
                            item.costs.Add(costCopy);
                        }
                    }
                    else
                    {
                        Debug.Log($"No light unit found for {item.type} {item.GetSubtypeIndex()} {item.curTier}");
                    }
                }
            }
#if UNITY_EDITOR
            UnityEditor.EditorUtility.SetDirty(this);
            Debug.Log("Copy light data to dark done");
#endif
        }
        // 要统一设置的目标成本列表（你可以在 Inspector 中直接配置）
        [BoxGroup("Tools"), LabelText("Target cost"), SerializeField]
        private List<CostResourceTypeAmountPair> targetCosts;

        [BoxGroup("Tools"), Button("Change all cost")]
        private void ApplyCostsToAllUnits()
        {
            if (targetCosts == null)
            {
                Debug.LogWarning("Target cost is null, please set change to target first！");
                return;
            }

            foreach (var item in items)
            {
                // 创建一个新列表副本，防止多个引用共享一个列表实例
                item.costs = new List<CostResourceTypeAmountPair>(targetCosts);
            }
#if UNITY_EDITOR
            UnityEditor.EditorUtility.SetDirty(this);
            Debug.Log("All unit costs changed");
#endif
        }
    }
    

    [Serializable]
    public class BuildingDataItem : GeneralDataItem
    {
        [VerticalGroup("EnumValues"), HideLabel, Tooltip("building type")]
        public BuildingType type;
        [VerticalGroup("Cost"), HorizontalGroup("Cost/0"), ListDrawerSettings(DraggableItems = true),
         TableColumnWidth(250, false), TableList(AlwaysExpanded = true)]
        public List<CostResourceTypeAmountPair> costs;

        [VerticalGroup("Cost"), HorizontalGroup("Cost/1")]
        public float constructTime = 10f;


        [ShowIf(nameof(IsGarrisonEnable)), FoldoutGroup("Gameplay/Garrison"), HorizontalGroup("Gameplay/Garrison/1"),
         Tooltip("This count should grow with tier, using this to avoid player put all units in a weak tower")]
        public int maxGarrisonCount;

        [ShowIf(nameof(IsGarrisonEnable)), FoldoutGroup("Gameplay/Garrison"), HorizontalGroup("Gameplay/Garrison/2")]
        public float3 outPositionBias;

        [ShowIf(nameof(IsGarrisonEnable)), FoldoutGroup("Gameplay/Garrison"), HorizontalGroup("Gameplay/Garrison/3"),
         ListDrawerSettings(DraggableItems = true)]
        public List<GarrisonUnitData> garrisonUnits;

        public override int GetGeneralTypeIndex()
        {
            return (int)type;
        }

        protected override void InitDefaults()
        {
            base.InitDefaults();
            if (baseTag == default)
            {
                baseTag = BaseTag.Buildings;
            }

            if (factionTag == default)
            {
                factionTag = FactionTag.Light;
            }
        }

        public bool IsGarrisonEnable()
        {
            return type switch
            {
                BuildingType.Fortifications => (FortificationType)GetSubtypeIndex() == FortificationType.Tower || (FortificationType)GetSubtypeIndex()== FortificationType.BigTower,
                BuildingType.Generators when GetSubtypeIndex() == (int)GeneratorType.ResourceMine => true,
                BuildingType.Generators when GetSubtypeIndex() == (int)GeneratorType.PlantGenerator => false,
                BuildingType.ConjuringShrines or BuildingType.Dwellings or BuildingType.Ornaments => false,
                _ => throw new ArgumentOutOfRangeException()
            };
        }

        public virtual int GetSubSubTypeIndex()
        {
            return 0;
        }

        [Serializable]
        public struct GarrisonUnitData
        {
            public UnitType unitType;
            public int subTypeIndex;
        }
    }

    [Serializable]
    public class FortificationData : BuildingDataItem
    {
        [VerticalGroup("EnumValues"), HideLabel, Tooltip("Fortification type")]
        public FortificationType fortificationType;

        public override bool IsAttackable() => fortificationType is FortificationType.Tower or FortificationType.BigTower;

        public override int GetSubtypeIndex() => (int)fortificationType;

        protected override void InitDefaults()
        {
            base.InitDefaults();
            if (type == default)
                type = BuildingType.Fortifications;
        }

    }

    [Serializable]
    public class GeneratorData : BuildingDataItem
    {
        [VerticalGroup("EnumValues"), HideLabel, Tooltip("generator type")]
        public GeneratorType generatorType;

        [VerticalGroup("EnumValues"), HideLabel, ShowIf(nameof(IsResourceMine))]
        public ResourceMineType resourceMineType;
        [VerticalGroup("EnumValues"), HideLabel, ShowIf(nameof(IsPlantGenerator))]
        public PlantGeneratorType plantGeneratorType;
        
        [FoldoutGroup("Gameplay/Generator"), HorizontalGroup("Gameplay/Generator/0"), HideLabel,
         Tooltip("generate resource type")]
        public ResourceType generateResourceType;

        [ShowIf(nameof(IsPlantGenerator)), FoldoutGroup("Gameplay/Generator"), HorizontalGroup("Gameplay/Generator/2"),
         Tooltip("All the cultivator generate speed bonus multiply this initial speed to " +
                 "calculate the cur speed, not the cur speed")]
        public float generateSpeed;

        [ShowIf(nameof(IsResourceMine)), FoldoutGroup("Gameplay/Generator"), HorizontalGroup("Gameplay/Generator/1")]
        public int minWorkersCount = 1;


        public override int GetSubtypeIndex() => (int)generatorType;

        protected override void InitDefaults()
        {
            base.InitDefaults();
            if (type == default)
                type = BuildingType.Generators;
        }

        private bool IsPlantGenerator() => generatorType == GeneratorType.PlantGenerator;
        private bool IsResourceMine() => generatorType == GeneratorType.ResourceMine;
        public override int GetSubSubTypeIndex() => IsResourceMine() ? (int)resourceMineType : (int)plantGeneratorType;

    }

    [Serializable]
    public class ConjuringShrineData : BuildingDataItem
    {
        [VerticalGroup("EnumValues"), HideLabel, Tooltip("conjuring shrine type"),
         OnValueChanged(nameof(SetConjureUnitType))]
        public ConjuringShrineType conjuringShrineType = ConjuringShrineType.AegisShrine;

        [FoldoutGroup("Gameplay/ConjuringShrine"), HorizontalGroup("Gameplay/ConjuringShrine/1"), HideLabel,
         ReadOnly, Tooltip("Conjure unit type")]
        public UnitType conjureUnitType = UnitType.Shield;

        [FoldoutGroup("Gameplay/ConjuringShrine"), HorizontalGroup("Gameplay/ConjuringShrine/2"), HideLabel,
        LabelText("Pos")]
        public float3 conjurePositionBias;


        public override int GetSubtypeIndex() => (int)conjuringShrineType;

        private void SetConjureUnitType()
        {
            conjureUnitType = (UnitType)conjuringShrineType;
        }

        protected override void InitDefaults()
        {
            base.InitDefaults();
            if (type == default)
                type = BuildingType.ConjuringShrines;
        }
    }

    [Serializable]
    public class DwellingData : BuildingDataItem
    {
        [VerticalGroup("EnumValues"), HideLabel, Tooltip("Dwelling type")]
        public DwellingType dwellingType;

        [FoldoutGroup("Gameplay/Dwelling"), HorizontalGroup("Gameplay/Dwelling/1"), HideLabel]
        public ResourceType dwellingResourceType = ResourceType.SoulPact;

        [FoldoutGroup("Gameplay/Dwelling"), HorizontalGroup("Gameplay/Dwelling/2")]
        public int dwellingAmount;

        public override int GetSubtypeIndex() => (int)dwellingType;

        protected override void InitDefaults()
        {
            base.InitDefaults();
            if (type == default)
                type = BuildingType.Dwellings;
        }
    }

    [Serializable]
    public class OrnamentData : BuildingDataItem
    {
        [VerticalGroup("EnumValues"), HideLabel, Tooltip("Ornament type")]
        public OrnamentType ornamentType;

        [FoldoutGroup("Gameplay/Ornament"), HorizontalGroup("Gameplay/Ornament/0")]
        public bool hasBuff;

        [ShowIf(nameof(hasBuff)), FoldoutGroup("Gameplay/Ornament"), HorizontalGroup("Gameplay/Ornament/1"), HideLabel]
        public BuffType ornamentBuffType;

        [ShowIf(nameof(hasBuff)), FoldoutGroup("Gameplay/Ornament"), HorizontalGroup("Gameplay/Ornament/2")]
        public float buffRange;

        [ShowIf(nameof(hasBuff)), FoldoutGroup("Gameplay/Ornament"), HorizontalGroup("Gameplay/Ornament/3")]
        public float buffLastTimeSeconds;


        public override int GetSubtypeIndex() => (int)ornamentType;
        public override bool IsAttackable() => ornamentType is OrnamentType.Crystal or OrnamentType.Beacon;

        protected override void InitDefaults()
        {
            base.InitDefaults();
            if (type == default)
                type = BuildingType.Ornaments;
        }
    }
}