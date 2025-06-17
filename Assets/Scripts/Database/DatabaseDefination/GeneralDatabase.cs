using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using Sirenix.OdinInspector;
using SparFlame.Database;
using SparFlame.GamePlaySystem.Building;
using SparFlame.GamePlaySystem.General;
using SparFlame.GamePlaySystem.Interact;
using Unity.Physics.Authoring;
using UnityEngine;
using UnityEngine.AddressableAssets;
// ReSharper disable RedundantJumpStatement

#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.AddressableAssets;
#endif


namespace GamePlaySystem.Database
{
    [Serializable]
    public class GeneralDataItem
    {
        #region General

        [VerticalGroup("General"), Tooltip("gameplayName"), HorizontalGroup("General/0"), TableColumnWidth(200, false)]
        public string gameplayName;

        [VerticalGroup("General"), Tooltip("global single id"), HorizontalGroup("General/1")]
        public int id;

        [VerticalGroup("General"), PreviewField,HorizontalGroup("General/2"), HideLabel] 
        public AssetReferenceSprite sprite2D;

        [VerticalGroup("General"), PreviewField,HorizontalGroup("General/2"),HideLabel]
        public GameObject prefab;

        [VerticalGroup("General"), HideLabel, Tooltip("Description"), TextArea(3, 10), HorizontalGroup("General/3")]
        public string description;

        [VerticalGroup("EnumValues"), HideLabel, Tooltip("base Tag"), TableColumnWidth(100,false)]
        public BaseTag baseTag;

        [VerticalGroup("EnumValues"), HideLabel, Tooltip("Faction Tag")]
        public FactionTag factionTag;

        #endregion

        #region Stat

        [VerticalGroup("Gameplay"), HorizontalGroup("Gameplay/Stat0"),TableColumnWidth(200,false)]
        public int stat;

        [VerticalGroup("Gameplay"), HorizontalGroup("Gameplay/Stat1"), ShowIf(nameof(IsStatUpGradable))]
        public int statPerLevel;

        #endregion

        #region Exp


        [ShowIf(nameof(IsUpgradable)), VerticalGroup("EnumValues")]
        public Tier curTier = Tier.Tier1;

        [ShowIf(nameof(IsUpgradable)), VerticalGroup("EnumValues"), ReadOnly]
        public Tier maxTier;

        [ShowIf(nameof(IsStatUpGradable)), VerticalGroup("Gameplay"),
         HorizontalGroup("Gameplay/Exp0")]
        public int expMaxValue;

        [ShowIf(nameof(IsStatUpGradable)), VerticalGroup("Gameplay"),
         HorizontalGroup("Gameplay/Exp1")]
        public int expGainPerLevel;

        [ShowIf(nameof(IsStatUpGradable)), VerticalGroup("Gameplay"),
         HorizontalGroup("Gameplay/Exp2")]
        public int maxLevel;
        
        #endregion

     

        #region InteractAbility

        [ShowIf(nameof(IsAttackable)), VerticalGroup("InteractAbility"), FoldoutGroup("InteractAbility/Attack"),
         HorizontalGroup("InteractAbility/Attack/0"), TableColumnWidth(250,false),HideLabel,LabelText("Amount")]
        public int attackAmount;

        [ShowIf(nameof(IsAttackUpGradable)), VerticalGroup("InteractAbility"),
         FoldoutGroup("InteractAbility/Attack"),
         HorizontalGroup("InteractAbility/Attack/0"), HideLabel,LabelText("PerLevel")]
        public int attackAmountPerLevel;

        [ShowIf(nameof(IsAttackable)), VerticalGroup("InteractAbility"), FoldoutGroup("InteractAbility/Attack"),
         HorizontalGroup("InteractAbility/Attack/1"), HideLabel,LabelText("Speed")]
        public float attackSpeed;

        [ShowIf(nameof(IsAttackUpGradable)), VerticalGroup("InteractAbility"), FoldoutGroup("InteractAbility/Attack"),
         HorizontalGroup("InteractAbility/Attack/1"),HideLabel, LabelText("PerLevel")]
        public float attackSpeedPerLevel;

        [ShowIf(nameof(IsAttackable)), VerticalGroup("InteractAbility"), FoldoutGroup("InteractAbility/Attack"),
         HorizontalGroup("InteractAbility/Attack/2"), HideLabel, LabelText("Range")]
        public float attackRange;
        [ShowIf(nameof(IsAttackUpGradable)), VerticalGroup("InteractAbility"), FoldoutGroup("InteractAbility/Attack"),
         HorizontalGroup("InteractAbility/Attack/2"),HideLabel, LabelText("PerLevel")]
        public float attackRangePerLevel;

        [ShowIf(nameof(IsAttackable)), VerticalGroup("InteractAbility"), FoldoutGroup("InteractAbility/Attack"),
         HorizontalGroup("InteractAbility/Attack/3"), HideLabel, LabelText("Targets")]
        public int attackTargets;
        [ShowIf(nameof(IsAttackUpGradable)), VerticalGroup("InteractAbility"), FoldoutGroup("InteractAbility/Attack"),
         HorizontalGroup("InteractAbility/Attack/3"),HideLabel, LabelText("PerLevel")]
        public int attackTargetsPerLevel;

        [ShowIf(nameof(IsHealable)), VerticalGroup("InteractAbility"), FoldoutGroup("InteractAbility/Heal"),
         HorizontalGroup("InteractAbility/Heal/0"), HideLabel, LabelText("Amount")]
        public int healAmount;
        [ShowIf(nameof(IsHealUpGradable)), VerticalGroup("InteractAbility"), FoldoutGroup("InteractAbility/Heal"),
         HorizontalGroup("InteractAbility/Heal/0"),HideLabel, LabelText("PerLevel")]
        public int healAmountPerLevel;

        [ShowIf(nameof(IsHealable)), VerticalGroup("InteractAbility"), FoldoutGroup("InteractAbility/Heal"),
         HorizontalGroup("InteractAbility/Heal/1"), HideLabel, LabelText("Speed")]
        public float healSpeed;
        [ShowIf(nameof(IsHealUpGradable)), VerticalGroup("InteractAbility"), FoldoutGroup("InteractAbility/Heal"),
         HorizontalGroup("InteractAbility/Heal/1"),HideLabel, LabelText("PerLevel")]
        public float healSpeedPerLevel;

        [ShowIf(nameof(IsHealable)), VerticalGroup("InteractAbility"), FoldoutGroup("InteractAbility/Heal"),
         HorizontalGroup("InteractAbility/Heal/2"), HideLabel, LabelText("Range")]
        public float healRange;
        [ShowIf(nameof(IsHealUpGradable)), VerticalGroup("InteractAbility"), FoldoutGroup("InteractAbility/Heal"),
         HorizontalGroup("InteractAbility/Heal/2"),HideLabel, LabelText("PerLevel")]
        public float healRangePerLevel;

        [ShowIf(nameof(IsHealable)), VerticalGroup("InteractAbility"), FoldoutGroup("InteractAbility/Heal"),
         HorizontalGroup("InteractAbility/Heal/3"), HideLabel, LabelText("Targets")]
        public int healTargets;
        [ShowIf(nameof(IsHealUpGradable)), VerticalGroup("InteractAbility"), FoldoutGroup("InteractAbility/Heal"),
         HorizontalGroup("InteractAbility/Heal/3"),HideLabel, LabelText("PerLevel")]
        public int healTargetsPerLevel;

        [ShowIf(nameof(IsHarvestable)), VerticalGroup("InteractAbility"), FoldoutGroup("InteractAbility/Harvest"),
         HorizontalGroup("InteractAbility/Harvest/0"), HideLabel, LabelText("Amount")]
        public int harvestAmount;
        [ShowIf(nameof(IsHarvestUpGradable)), VerticalGroup("InteractAbility"), FoldoutGroup("InteractAbility/Harvest"),
         HorizontalGroup("InteractAbility/Harvest/0"),HideLabel, LabelText("PerLevel")]
        public int harvestAmountPerLevel;

        [ShowIf(nameof(IsHarvestable)), VerticalGroup("InteractAbility"), FoldoutGroup("InteractAbility/Harvest"),
         HorizontalGroup("InteractAbility/Harvest/1"), HideLabel, LabelText("Speed")]
        public float harvestSpeed;
        [ShowIf(nameof(IsHarvestUpGradable)), VerticalGroup("InteractAbility"), FoldoutGroup("InteractAbility/Harvest"),
         HorizontalGroup("InteractAbility/Harvest/1"),HideLabel, LabelText("PerLevel")]
        public float harvestSpeedPerLevel;

        [ShowIf(nameof(IsHarvestable)), VerticalGroup("InteractAbility"), FoldoutGroup("InteractAbility/Harvest"),
         HorizontalGroup("InteractAbility/Harvest/2"), HideLabel, LabelText("Range")]
        public float harvestRange;
        [ShowIf(nameof(IsHarvestUpGradable)), VerticalGroup("InteractAbility"), FoldoutGroup("InteractAbility/Harvest"),
         HorizontalGroup("InteractAbility/Harvest/2"),HideLabel, LabelText("PerLevel")]
        public float harvestRangePerLevel;

        [ShowIf(nameof(IsHarvestable)), VerticalGroup("InteractAbility"), FoldoutGroup("InteractAbility/Harvest"),
         HorizontalGroup("InteractAbility/Harvest/3"), HideLabel, LabelText("Targets")]
        public int harvestTargets;
        [ShowIf(nameof(IsHarvestUpGradable)), VerticalGroup("InteractAbility"), FoldoutGroup("InteractAbility/Harvest"),
         HorizontalGroup("InteractAbility/Harvest/3"),HideLabel, LabelText("PerLevel")]
        public int harvestTargetsPerLevel;

        #endregion

    

        #region Additional
        
        [ShowIf(nameof(IsUpgradable)), VerticalGroup("Additional"), HorizontalGroup("Additional/Exp"),]
        [CanBeNull]
        public GameObject nextTierPrefab;
        
        [VerticalGroup("Additional"),
         HorizontalGroup("Additional/Sight1"),
         Tooltip(
             "This value determines the attack/heal/harvest value of this object, the higher " +
             "the value, the first to be attacked/healed/harvested")]
        public int sightPriority;

        [ShowIf(nameof(HasSight)), FoldoutGroup("Additional/Sight", Expanded = false),
         HorizontalGroup("Additional/Sight2"),
         OnValueChanged(nameof(OnSightPrefabChanged)), HideLabel]
        public GameObject sightPrefab;

        [FoldoutGroup("Additional/Sight", Expanded = false)]
        [ShowIf(nameof(HasSight))]
        [HorizontalGroup("Additional/Sight3"), LabelText("Range"), ReadOnly,HideLabel]
        public float sightRange;

        [VerticalGroup("Additional"), HideLabel, LabelText("ExtraConfig"),TableColumnWidth(200, false)] public bool enableAdditionalConfig;

        #endregion

        #region Public Interface

        public virtual int GetGeneralTypeIndex()
        {
            throw new ArgumentException(
                "This method should be overridden in derived classes to return the correct type index.");
        }

        public virtual int GetSubtypeIndex()
        {
            throw new ArgumentException(
                "This method should be overridden in derived classes to return the correct type index.");
        }

        public bool HasSight()
        {
            return IsAttackable() || IsHealable() || IsHarvestable();
        }

        public virtual bool IsAttackable()
        {
            return false;
        }

        public bool IsAttackUpGradable()
        {
            return IsAttackable() && baseTag == BaseTag.Units && IsUpgradable();
        }

        public virtual bool IsHealable()
        {
            return false;
        }
        public bool IsHealUpGradable()
        {
            return IsHealable() && baseTag == BaseTag.Units && IsUpgradable();
        }

        public virtual bool IsHarvestable()
        {
            return false;
        }
        
        public bool IsHarvestUpGradable()
        {
            return IsHarvestable() && baseTag == BaseTag.Units && IsUpgradable();
        }

        public bool IsStatUpGradable()
        {
            return baseTag == BaseTag.Units && IsUpgradable();
        }

        public bool IsUpgradable()
        {
            return baseTag is BaseTag.Buildings or BaseTag.Units;
        }

        #endregion

        #region Automatic methods

        [OnInspectorInit]
        protected virtual void InitDefaults()
        {
            if (string.IsNullOrEmpty(gameplayName))
            {
                gameplayName = "New " + GetType().Name;
            }
        }

        private void OnSightPrefabChanged()
        {
            var authoring = sightPrefab.GetComponent<PhysicsShapeAuthoring>();
            sightRange = authoring.GetCylinderRadius();
        }

        #endregion
    }

    public abstract class GeneralDatabase<TDataItem> : ScriptableObject where TDataItem : GeneralDataItem
    {
        public int idStart;


        public abstract List<TDataItem> Items { get; }

        public TDataItem GetItemById(int id)
        {
            var trueId = id - idStart;
            if (trueId < 0 || Items.Count <= trueId)
            {
                // Debug.Log($"true id : {trueId}, Items count : {Items.Count}, idStart : {idStart}");
                throw new ArgumentException($"{typeof(TDataItem).Name} with id {id} does not exist");
            }

            return Items[id - idStart];
        }

        private bool _shouldCheckExp;
        private int _preLength;


        [Button("Reassign all id and ReBake")]
        private void ReassignAllIDs()
        {
            if (Items.Count > 30)
                throw new ArgumentException(
                    $"Max count is 30, item count is {Items.Count}, please split the database.");

            for (int i = 0; i < Items.Count; i++)
            {
                Items[i].id = idStart + i;

                GameObject go = Items[i].prefab;
#if UNITY_EDITOR
                MonoBehaviour authoring = null;
                if (Items[i] is UnitDataItem)
                    authoring = go.GetComponent<GeneralUnitAttributesAuthoring>();
                if (Items[i] is BuildingDataItem)
                    authoring = go.GetComponent<GeneralBuildingAttributesAuthoring>();
                if (Items[i] is ResourceDataItem)
                    authoring = go.GetComponent<GeneralResourceAttributesAuthoring>();

                if (authoring)
                {
                    var so = new SerializedObject(authoring);
                    so.FindProperty("globalIdx").intValue = Items[i].id;
                    so.ApplyModifiedProperties();

                    EditorUtility.SetDirty(go);
                    UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(go.scene);
                }
#endif
            }
        }


        [Button("Check Exp settings Valid")]
        private void CheckExpSettings()
        {
            for (var i = 0; i < Items.Count; i++)
            {
                var item = Items[i];
                if (!item.IsUpgradable()) continue;
                if (item.curTier != Tier.Tier1)
                {
                    if (item.maxTier == default)
                        Debug.LogError(
                            $"{item.gameplayName}/{item.id} has wrong place, because its tier not match sequence");
                    // Only check the tier1 of all objects or will duplicate
                    continue;
                }

                var j = i;
                var preGo = Items[i].prefab;
                while (Items[j].nextTierPrefab)
                {
                    if ((int)Items[j].curTier != 3 + j - i || Items[j].prefab != preGo)
                    {
                        Debug.LogError(
                            "Database Upgrade settings wrong, all tier prefabs of same object should in sequence in database\n" +
                            $"{Items[j].gameplayName}/{Items[j].id} is in wrong position\n"
                        );
                        return;
                    }

                    preGo = Items[j].nextTierPrefab;
                    j++;
                }

                for (int k = i; k <= j; k++)
                {
                    Items[k].maxTier = (Tier)(j - i + 3);
                }
            }

            _shouldCheckExp = false;
            if (_shouldCheckExp) return;
            return;
        }

#if UNITY_EDITOR
        [Button("Auto assign Addressable Sprite")]
        private void AutoAssignSpritesFromAddressables()
        {
            foreach (var item in Items)
            {
                if (!item.prefab)
                {
                    Debug.LogWarning($"empty prefab in id {item.id}");
                    continue;
                }

                var key = item.prefab.name;
                var settings = AddressableAssetSettingsDefaultObject.Settings;
                var entry = settings.groups
                    .SelectMany(g => g.entries)
                    .FirstOrDefault(e => e.address == key);

                if (entry == null)
                {
                    Debug.LogWarning($"Not find Addressables  key：{key}");
                    continue;
                }

                item.sprite2D = new AssetReferenceSprite(entry.guid);
            }

            EditorUtility.SetDirty(this);
            AssetDatabase.SaveAssets();
        }
#endif
    }
}