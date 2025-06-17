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
using UnityEngine;

namespace SparFlame.Database
{
    [CreateAssetMenu(fileName = "UnitDatabase", menuName = "GameData/UnitDatabase", order = 0)]
    public class UnitDatabaseSo : GeneralDatabase<UnitDataItem>
    {
        [SerializeReference, TableList(ShowIndexLabels = true), HideLabel, ListDrawerSettings(DraggableItems = true)]
        private List<UnitDataItem> items;


        [SerializeField, ValueDropdown(nameof(GetTypeOptions))]
        private string selectedTypeName;


        private IEnumerable<string> GetTypeOptions()
        {
            return GetAllUnitTypes().Select(t => t.FullName);
        }

        private IEnumerable<Type> GetAllUnitTypes()
        {
            return Assembly.GetAssembly(typeof(UnitDataItem))
                .GetTypes()
                .Where(t => t.IsSubclassOf(typeof(UnitDataItem)) && !t.IsAbstract);
        }

        [Button("Add Unit")]
        private void AddSelectedUnit()
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

            if (Activator.CreateInstance(type) is UnitDataItem instance)
            {
                items.Add(instance);
            }
        }

        [Button("Copy Light Data to Dark")]
        private void CopyLightDataToDark()
        {
            var dict = new Dictionary<(UnitType, int, Tier), UnitDataItem>();
            foreach (var item in items)
            {
                if (item.factionTag == FactionTag.Ally)
                    dict.Add((item.type, item.GetSubtypeIndex(), item.curTier), item);
            }

            foreach (var item in items)
            {
                if (item.factionTag == FactionTag.Enemy)
                {
                    if (dict.TryGetValue((item.type, item.GetSubtypeIndex(), item.curTier), out var lightItem))
                    {
                        item.moveSpeed = lightItem.moveSpeed;
                        item.moveSpeedPerLevel = lightItem.moveSpeedPerLevel;
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

                        item.conjureSpeedSecondPerUnit = lightItem.conjureSpeedSecondPerUnit;
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

        [BoxGroup("Tools"), Button("Change all units cost")]
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


        public override List<UnitDataItem> Items => items;
    }


    [Serializable]
    public class UnitDataItem : GeneralDataItem
    {
        [VerticalGroup("EnumValues"), HideLabel, Tooltip("unity type")]
        public UnitType type;

        #region Movement

        [VerticalGroup("Gameplay"), FoldoutGroup("Gameplay/MoveSpeed", expanded: true),
         HorizontalGroup("Gameplay/MoveSpeed/0"), HideLabel]
        public float moveSpeed;

        [VerticalGroup("Gameplay"), FoldoutGroup("Gameplay/MoveSpeed", expanded: true),
         HorizontalGroup("Gameplay/MoveSpeed/0"), HideLabel]
        public float moveSpeedPerLevel;


        [ShowIf(nameof(enableAdditionalConfig)), VerticalGroup("Additional"), HorizontalGroup("Additional/Movement"),
         Tooltip("how long interval will the nav system calculate the path for this unit")]
        public float movementCalculationInterval = 1.0f;

        #endregion

        [VerticalGroup("Cost"), HorizontalGroup("Cost/1"), ListDrawerSettings(DraggableItems = true),
         TableColumnWidth(200, false),
         TableList(AlwaysExpanded = true)]
        public List<CostResourceTypeAmountPair> costs;

        [VerticalGroup("Cost"), HorizontalGroup("Cost/2")]
        public float conjureSpeedSecondPerUnit;

        [ShowIf(nameof(HasLightGroupBuff)), FoldoutGroup("Additional/Buff"),
         AssetsOnly, Tooltip("Light shield and light cavalry will raise buff by aoe trigger")]
        public GameObject lightGroupAoeTrigger;

        public override int GetGeneralTypeIndex()
        {
            return (int)type;
        }

        protected override void InitDefaults()
        {
            base.InitDefaults();
            if (baseTag == default)
                baseTag = BaseTag.Units;
            if (factionTag == default)
                factionTag = FactionTag.Ally;
        }

        public bool HasLightGroupBuff()
        {
            return factionTag == FactionTag.Ally && type is UnitType.Shield or UnitType.Cavalry;
        }
    }

    [Serializable]
    public class ShieldData : UnitDataItem
    {
        [VerticalGroup("EnumValues"), HideLabel, Tooltip("shield type")]
        public ShieldType shieldType;

        public override int GetSubtypeIndex() => (int)shieldType;

        public override bool IsAttackable() => true;

        public override bool IsHarvestable() => false;

        public override bool IsHealable() => shieldType == ShieldType.Paladin;

        protected override void InitDefaults()
        {
            base.InitDefaults();
            if (type == default)
                type = UnitType.Shield;
        }
    }


    [Serializable]
    public class RangedData : UnitDataItem
    {
        [VerticalGroup("EnumValues"), HideLabel, Tooltip("range type")]
        public RangedType rangedType;

        public override int GetSubtypeIndex() => (int)rangedType;
        public override bool IsAttackable() => true;

        public override bool IsHarvestable() => false;

        public override bool IsHealable() => false;

        protected override void InitDefaults()
        {
            base.InitDefaults();
            if (type == default)
                type = UnitType.Ranged;
        }
    }

    [Serializable]
    public class MagicData : UnitDataItem
    {
        [VerticalGroup("EnumValues"), HideLabel, Tooltip("magic type")]
        public MagicType magicType;

        public override int GetSubtypeIndex() => (int)magicType;
        public override bool IsAttackable() => magicType != MagicType.Cleric;
        public override bool IsHarvestable() => false;
        public override bool IsHealable() => magicType is MagicType.Cleric or MagicType.Prophet;

        protected override void InitDefaults()
        {
            base.InitDefaults();
            if (type == default)
                type = UnitType.Magic;
        }
    }

    [Serializable]
    public class CavalryData : UnitDataItem
    {
        [VerticalGroup("EnumValues"), HideLabel, Tooltip("cavalry type")]
        public CavalryType cavalryType;

        public override int GetSubtypeIndex() => (int)cavalryType;

        public override bool IsAttackable() => true;
        public override bool IsHarvestable() => false;
        public override bool IsHealable() => false;

        protected override void InitDefaults()
        {
            base.InitDefaults();
            if (type == default)
                type = UnitType.Cavalry;
        }
    }

    [Serializable]
    public class WorkerData : UnitDataItem
    {
        [VerticalGroup("EnumValues"), HideLabel, Tooltip("worker type")]
        public WorkerType workerType;

        [ShowIf(nameof(IsAttuner)), FoldoutGroup("Gameplay/Attuner")]
        public float generateSpeedBonus;

        public override int GetSubtypeIndex() => (int)workerType;
        public override bool IsAttackable() => true;
        public override bool IsHarvestable() => true;
        public override bool IsHealable() => false;
        private bool IsAttuner() => workerType == WorkerType.Attuner;

        protected override void InitDefaults()
        {
            base.InitDefaults();
            if (type == default)
                type = UnitType.Worker;
        }
    }
}