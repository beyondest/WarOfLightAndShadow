using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using Sirenix.OdinInspector;
using SparFlame.Database;
using SparFlame.GamePlaySystem.Building;
using SparFlame.GamePlaySystem.Interact;
using SparFlame.GamePlaySystem.General;
using Unity.Physics.Authoring;
using UnityEditor.AddressableAssets;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceLocations;
// ReSharper disable RedundantJumpStatement

#if UNITY_EDITOR
using UnityEditor;
#endif


namespace GamePlaySystem.Database
{
    [Serializable]
    public class GeneralDataItem
    {
        #region General

        [VerticalGroup("General"), Tooltip("gameplayName")]
        public string gameplayName;

        [VerticalGroup("General"), Tooltip("global single id")]
        public int id;

        [VerticalGroup("Asset"), PreviewField] public AssetReferenceSprite sprite2D;

        [VerticalGroup("Asset"), PreviewField, OnValueChanged(nameof(OnGamePrefabChanged))]
        public GameObject prefab;

        [VerticalGroup("General"), HideLabel, Tooltip("Description"),TextArea(3, 10)] public string description;

        [VerticalGroup("EnumValues"), HideLabel, Tooltip("base Tag")]
        public BaseTag baseTag;

        [VerticalGroup("EnumValues"), HideLabel, Tooltip("Faction Tag")]
        public FactionTag factionTag;

        #endregion

        #region Stat

        [VerticalGroup("Gameplay"), HorizontalGroup("Gameplay/Stat")]
        public int stat;

        #endregion

        #region Exp

        [VerticalGroup("Gameplay")] 
        public bool upgradable;

        
        [ShowIf(nameof(upgradable)), VerticalGroup("Gameplay"),FoldoutGroup("Gameplay/Exp"), HorizontalGroup("Gameplay/Exp/1"),
         Tooltip("curTier")]
        public Tier curTier = Tier.Tier1;

        [ShowIf(nameof(upgradable)), VerticalGroup("Gameplay"), FoldoutGroup("Gameplay/Exp"),HorizontalGroup("Gameplay/Exp/2"),
         Tooltip("maxTier"), ReadOnly]
        public Tier maxTier;

        [ShowIf(nameof(upgradable)), VerticalGroup("Gameplay"),FoldoutGroup("Gameplay/Exp"), HorizontalGroup("Gameplay/Exp/3"),]
        public int statMaxValue;

        [ShowIf(nameof(upgradable)), VerticalGroup("Gameplay"),FoldoutGroup("Gameplay/Exp"), HorizontalGroup("Gameplay/Exp/4"),] [CanBeNull]
        public GameObject nextTierPrefab;

        #endregion

        #region Sight

        [ FoldoutGroup("Gameplay/Sight", Expanded = false),
         HorizontalGroup("Gameplay/Sight/0"),
         Tooltip(
             "This value determines the attack/heal/harvest value of this object, the higher " +
             "the value, the first to be attacked/healed/harvested")]
        public int sightPriority;
        
        
        [ShowIf(nameof(HasSight)), FoldoutGroup("Gameplay/Sight",Expanded = false),
         HorizontalGroup("Gameplay/Sight/1"),
         OnValueChanged(nameof(OnSightPrefabChanged)), HideLabel]
        public GameObject sightPrefab;

        [FoldoutGroup("Gameplay/Sight", Expanded = false)]
        [ShowIf(nameof(HasSight))]
        [HorizontalGroup("Gameplay/Sight/2"), LabelText("Range"),ReadOnly]
        public float sightRange;

        [FoldoutGroup("Gameplay/Sight", Expanded = false)]
        [ShowIf(nameof(HasSight))]
        [HorizontalGroup("Gameplay/Sight/3"), LabelText("Belongs To"),ReadOnly]
        public PhysicsCategoryTags sightBelongsTo;

        [FoldoutGroup("Gameplay/Sight", Expanded = false)]
        [ShowIf(nameof(HasSight))]
        [HorizontalGroup("Gameplay/Sight/4"), LabelText("Collides With"),ReadOnly]
        public PhysicsCategoryTags sightCollidesWith;

        #endregion

        #region InteractAbility

        [ShowIf(nameof(IsAttackable)), VerticalGroup("InteractAbility"),FoldoutGroup("InteractAbility/Attack"), HorizontalGroup("InteractAbility/Attack/0")]
        public int attackAmount;

        [ShowIf(nameof(IsAttackable)), VerticalGroup("InteractAbility"),FoldoutGroup("InteractAbility/Attack"), HorizontalGroup("InteractAbility/Attack/1")]
        public float attackSpeed;

        [ShowIf(nameof(IsAttackable)), VerticalGroup("InteractAbility"),FoldoutGroup("InteractAbility/Attack"), HorizontalGroup("InteractAbility/Attack/2")]
        public float attackRange;

        [ShowIf(nameof(IsAttackable)), VerticalGroup("InteractAbility"),FoldoutGroup("InteractAbility/Attack"), HorizontalGroup("InteractAbility/Attack/3")]
        public int attackTargets;

        [ShowIf(nameof(IsHealable)), VerticalGroup("InteractAbility"),FoldoutGroup("InteractAbility/Heal"), HorizontalGroup("InteractAbility/Heal/0")]
        public int healAmount;

        [ShowIf(nameof(IsHealable)), VerticalGroup("InteractAbility"),FoldoutGroup("InteractAbility/Heal"), HorizontalGroup("InteractAbility/Heal/1")]
        public float healSpeed;

        [ShowIf(nameof(IsHealable)), VerticalGroup("InteractAbility"),FoldoutGroup("InteractAbility/Heal"), HorizontalGroup("InteractAbility/Heal/2")]
        public float healRange;

        [ShowIf(nameof(IsHealable)), VerticalGroup("InteractAbility"),FoldoutGroup("InteractAbility/Heal"), HorizontalGroup("InteractAbility/Heal/3")]
        public int healTargets;

        [ShowIf(nameof(IsHarvestable)), VerticalGroup("InteractAbility"),FoldoutGroup("InteractAbility/Harvest"), HorizontalGroup("InteractAbility/Harvest/0")]
        public int harvestAmount;

        [ShowIf(nameof(IsHarvestable)), VerticalGroup("InteractAbility"),FoldoutGroup("InteractAbility/Harvest"), HorizontalGroup("InteractAbility/Harvest/1")]
        public float harvestSpeed;

        [ShowIf(nameof(IsHarvestable)), VerticalGroup("InteractAbility"),FoldoutGroup("InteractAbility/Harvest"), HorizontalGroup("InteractAbility/Harvest/2")]
        public float harvestRange;

        [ShowIf(nameof(IsHarvestable)), VerticalGroup("InteractAbility"),FoldoutGroup("InteractAbility/Harvest"), HorizontalGroup("InteractAbility/Harvest/3")]
        public int harvestTargets;

        #endregion

        #region Physics

        [VerticalGroup("Physics"),Tooltip("Collide Belongs to"),ReadOnly]
        public PhysicsCategoryTags collideBelongsTo;

        [VerticalGroup("Physics"), Tooltip("Collide With"),ReadOnly]
        public PhysicsCategoryTags collideWith;

        #endregion

        #region Additional

        [VerticalGroup("Additional")] public bool enableAdditionalConfig;

        [ShowIf(nameof(enableAdditionalConfig)), FoldoutGroup("Additional/FogOfWar"),
         HorizontalGroup("Additional/FogOfWar/0")]
        public float fogSightRange = 30;
        
        [ShowIf(nameof(enableAdditionalConfig)), FoldoutGroup("Additional/FogOfWar"),
         HorizontalGroup("Additional/FogOfWar/1")]
        public float fogSightAngle = 360;
        
        [ShowIf(nameof(enableAdditionalConfig)), FoldoutGroup("Additional/FogOfWar"),
         HorizontalGroup("Additional/FogOfWar/2")]
        public float disappearAlphaThreshold = 0.1f;
        
        
        
        #endregion

        #region Public Interface

        public virtual int GetGeneralTypeIndex()
        {
            throw new NotImplementedException();
        }
        public virtual int GetSubtypeIndex()
        {
            throw new NotImplementedException();
        }
        public bool HasSight()
        {
            return IsAttackable() || IsHealable() || IsHarvestable();
        }

        public virtual bool IsAttackable()
        {
            return false;
        }

        public virtual bool IsHealable()
        {
            return false;
        }

        public virtual bool IsHarvestable()
        {
            return false;
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

        private void OnGamePrefabChanged()
        {
            var authoring = prefab.GetComponent<PhysicsShapeAuthoring>();
            collideBelongsTo = authoring.BelongsTo;
            collideWith = authoring.CollidesWith;
        }

        private void OnSightPrefabChanged()
        {
            var authoring = sightPrefab.GetComponent<PhysicsShapeAuthoring>();
            sightRange = authoring.GetCylinderRadius();
            sightBelongsTo = authoring.BelongsTo;
            sightCollidesWith = authoring.CollidesWith;
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
                throw new ArgumentException($"Max count is 30, item count is {Items.Count}, please split the database.");

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

                if (authoring != null)
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
                if (!item.upgradable) continue;
                if (item.curTier != Tier.Tier1)
                {
                    if(item.maxTier == default)Debug.LogError($"{item.gameplayName}/{item.id} has wrong place, because its tier not match sequence");
                    // Only check the tier1 of all objects or will duplicate
                    continue;
                }
                var j = i;
                var preGo = Items[i].prefab;
                while (Items[j].nextTierPrefab != null)
                {
                    if ((int)Items[j].curTier != 3 + j - i || Items[j].prefab != preGo)
                    {
                        Debug.LogError("Database Upgrade settings wrong, all tier prefabs of same object should in sequence in database\n" +
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
            if(_shouldCheckExp)return ;
            return ;
        }
        
#if UNITY_EDITOR
        [Button("Auto assign Addressable Sprite")]
        private void AutoAssignSpritesFromAddressables()
        {

            foreach (var item in Items)
            {
                if (item.prefab == null)
                {
                    Debug.LogWarning($"empty prefab in id {item.id}");
                    continue;
                }

                string key = item.prefab.name;
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