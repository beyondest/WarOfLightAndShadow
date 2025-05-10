using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using GamePlaySystem.Database;
using Sirenix.OdinInspector;
using SparFlame.GamePlaySystem.General;
using SparFlame.GamePlaySystem.Interact;
using SparFlame.GamePlaySystem.Resource;
using SparFlame.GamePlaySystem.Units;
using Unity.Mathematics;
using UnityEngine;

// ReSharper disable RedundantJumpStatement

namespace SparFlame.GamePlaySystem.Building
{
    [CreateAssetMenu(fileName = "BuildingDatabase", menuName = "GameData/BuildingDatabase", order = 0)]
    public class BuildingDatabaseSo : GeneralDatabase<BuildingDataItem>
    {
        
        [ TableList(ShowIndexLabels = true,AlwaysExpanded = false),TableColumnWidth(50, Resizable = true),
         SerializeReference, HideLabel,ListDrawerSettings(DraggableItems = true) ]
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

        private void ShouldCheckValid()
        {
            _shouldCheckValid = true;
        }
        
        [Button]
        private void CheckBuildingConfigValid()
        {
            foreach (var item in items)
            {
                if (item.HasSight() )
                {
                    if (item.IsAttackable() && !Mathf.Approximately(item.attackRange, item.sightRange)
                        || item.IsHealable() && !Mathf.Approximately(item.healRange, item.sightRange)
                        || item.IsHarvestable() && !Mathf.Approximately(item.harvestRange, item.sightRange))
                    {
                        item.attackRange = item.harvestRange = item.healRange = item.sightRange;
                    }
                }

                if (item.type is BuildingType.ConjuringShrines or BuildingType.Generators)
                {
                    if (!item.upgradable)
                    {
                        Debug.LogError($"{item.gameplayName} / {item.id} : ConjuringShrine and Generators must be upgradable");
                        item.upgradable = true;
                    }
                }
            }
            _shouldCheckValid = false;
            if(_shouldCheckValid)return;
        }
    }

    [Serializable]
    public class BuildingDataItem : GeneralDataItem
    {
        [VerticalGroup("EnumValues"), HideLabel, Tooltip("building type")]
        public BuildingType type;
  
        [VerticalGroup("Gameplay"), HorizontalGroup("Gameplay/Cost"),ListDrawerSettings(DraggableItems = true)]
        public List<CostResourceTypeAmountPair> costs;

        [ShowIf(nameof(IsGarrisonEnable)), FoldoutGroup("Gameplay/Garrison"), HorizontalGroup("Gameplay/Garrison/1"),
        Tooltip("This count should grow with tier, using this to avoid player put all units in a weak tower")]
        public int maxGarrisonCount;
        
        [ShowIf(nameof(IsGarrisonEnable)), FoldoutGroup("Gameplay/Garrison"), HorizontalGroup("Gameplay/Garrison/2")]
        public float3 outPositionBias;
        
        [ShowIf(nameof(IsGarrisonEnable)),FoldoutGroup("Gameplay/Garrison"),HorizontalGroup("Gameplay/Garrison/3") ,ListDrawerSettings(DraggableItems = true)]
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
                factionTag = FactionTag.Ally;
            }
        }
        
        public bool IsGarrisonEnable()
        {
            return type switch
            {
                BuildingType.Fortifications => (FortificationType)GetSubtypeIndex() == FortificationType.Tower,
                BuildingType.Generators => true,
                BuildingType.ConjuringShrines or BuildingType.Dwellings or BuildingType.Ornaments => false,
                _ => throw new ArgumentOutOfRangeException()
            };
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

        public override bool IsAttackable() => fortificationType is FortificationType.Tower ;

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

        [ShowIf(nameof(IsBloom)),FoldoutGroup("Gameplay/Bloom"),HorizontalGroup("Gameplay/Bloom/0"),HideLabel, Tooltip("generate resource type")] 
        public ResourceType generateResourceType;
        [ShowIf(nameof(IsBloom)),FoldoutGroup("Gameplay/Bloom"),HorizontalGroup("Gameplay/Bloom/1")] 
        public int minCultivatorCounts;
        [ShowIf(nameof(IsBloom)),FoldoutGroup("Gameplay/Bloom"),HorizontalGroup("Gameplay/Bloom/2"),Tooltip("All the cultivator generate speed bonus multiply this initial speed to " +
             "calculate the cur speed, not the cur speed")] 
        public float initGenerateSpeed;
        [ShowIf(nameof(IsBloom)),FoldoutGroup("Gameplay/Bloom"),HorizontalGroup("Gameplay/Bloom/3")] 
        public float maxGenerateSpeed;

        [ShowIf(nameof(IsConvertor)), FoldoutGroup("Gameplay/Convertor"), HorizontalGroup("Gameplay/Convertor/0")]
        public ResourceType convertToType;
        public override int GetSubtypeIndex() => (int)generatorType;
        protected override void InitDefaults()
        {
            base.InitDefaults();
            if (type == default)
                type = BuildingType.Generators;
        }

        private bool IsBloom() => generatorType == GeneratorType.BloomSpire;
        private bool IsConvertor() => generatorType == GeneratorType.Converter;
    }

    [Serializable]
    public class ConjuringShrineData : BuildingDataItem
    {
        [VerticalGroup("EnumValues"), HideLabel, Tooltip("conjuring shrine type"),OnValueChanged(nameof(SetConjureUnitType))]
        public ConjuringShrineType conjuringShrineType = ConjuringShrineType.AegisShrine;
        
        [FoldoutGroup("Gameplay/ConjuringShrine"), HorizontalGroup("Gameplay/ConjuringShrine/1"), HideLabel,
        ReadOnly, Tooltip("Conjure unit type")]
        public UnitType conjureUnitType = UnitType.Shield;
        
        [FoldoutGroup("Gameplay/ConjuringShrine"), HorizontalGroup("Gameplay/ConjuringShrine/2")]
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
        
        [FoldoutGroup("Gameplay/Dwelling"), HorizontalGroup("Gameplay/Dwelling/1"),HideLabel]
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
        
        [ShowIf(nameof(hasBuff)),FoldoutGroup("Gameplay/Ornament"), HorizontalGroup("Gameplay/Ornament/1"),HideLabel]
        public BuffType ornamentBuffType;

        [ShowIf(nameof(hasBuff)), FoldoutGroup("Gameplay/Ornament"), HorizontalGroup("Gameplay/Ornament/2")]
        public float buffRange;

        [ShowIf(nameof(hasBuff)), FoldoutGroup("Gameplay/Ornament"), HorizontalGroup("Gameplay/Ornament/3")]
        public float buffLastTimeSeconds;


       
        
        public override int GetSubtypeIndex() => (int)ornamentType;
        public override bool IsAttackable() => ornamentType == OrnamentType.Crystal;
        protected override void InitDefaults()
        {
            base.InitDefaults();
            if (type == default)
                type = BuildingType.Ornaments;
        }
    }
}