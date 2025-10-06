using System;
using System.Collections.Generic;
using SparFlame.Components.General;
using SparFlame.Components.MainGameplay;
using SparFlame.Components.SubGameplay;
using SparFlame.Core.Utils;
using SparFlame.Systems.General.BasicControl;
using SparFlame.UI.General;
using SparFlame.UI.MainGameplay;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;
using UnityEngine.UI;

namespace SparFlame.UI.SubGameplay.StaticWindows
{
    public class ArmyGroupSlotWindow : MultiSlotWindowUtils.MultiSlotsWindow<ArmyGroupSlot>
    {
        [SerializeField] private List<ArmyGroupSortTypeIcon> sortIcons;

        [SerializeField] private Image sortImage;

        [SerializeField] private Image tierFilterButtonImage;

        [SerializeField] private List<Image> unitTypeFilterSelectedImages;

        [SerializeField] private GameObject otherSubGameplayStaticPanel;

        [SerializeField] private GameObject accurateSelectionPanel;

        [SerializeField] private GameObject enterAccurateSelectionButton;
        [SerializeField] private bool initNoUnitTypeSelected = true;
        public static ArmyGroupSlotWindow Instance;

        public event Action OnEcsRemoveSelectedUnitsFromTheirArmyGroup;
        public event Action<bool, Tier, List<UnitType>> OnEcsSelectAllUnitsWithoutArmyGroupAndGarrisoned;
        public event Action<Entity, bool> OnEcsSelectArmyGroupUnits;
        public event Action<Entity> OnEcsSprintArmyGroupUnits;
        public event Action<Entity> OnEcsHoldSwitchArmyGroup;
        public event Action<Entity> OnEcsCastSkill; 

        public Action<Entity, AddToArmyGroupType> OnEcsAddToArmyGroup;

        public event Action OnEcsCheckSelected;

        public event Action OnEcsUpdateArmyGroupAvgData;
        public bool HasSelectedUnitAlreadyInArmyGroup { get; set; }


        public void SwitchSelectionMode(bool enter)
        {
            enterAccurateSelectionButton.SetActive(!enter);
            using var query =
                World.DefaultGameObjectInjectionWorld.EntityManager.CreateEntityQuery(typeof(UnitSelectionFilter));
            var data = query.GetSingletonRW<UnitSelectionFilter>();
            data.ValueRW.UnitTypeFilterEnabled = enter;
            if (!enter)
            {
                data.ValueRW.TierFilterEnabled = false;
            }

            foreach (var slot in SlotComponents)
            {
                slot.SetAddButton(enter);
            }

            accurateSelectionPanel.SetActive(enter);
            otherSubGameplayStaticPanel.SetActive(!enter);
        }


        public void UpDateCandidates(List<ArmyGroupSlotInfo> armyGroupSlotInfos, bool isInPlayerCity)
        {
            // enterAccurateSelectionButton.SetActive(!_isInSelectionMode && isInPlayerCity);
            switch (_currentArmyGroupSortType)
            {
                case ArmyGroupSortType.ByCreateTimeAscending:
                    armyGroupSlotInfos.Sort((a, b) => a.CreateTimeTotalHours.CompareTo(b.CreateTimeTotalHours));
                    break;
                case ArmyGroupSortType.ByCreateTimeDescending:
                    armyGroupSlotInfos.Sort((a, b) => b.CreateTimeTotalHours.CompareTo(a.CreateTimeTotalHours));
                    break;
                case ArmyGroupSortType.ByCurrentUnitCountDescending:
                    armyGroupSlotInfos.Sort((a, b) => b.CurrentUnitCount.CompareTo(a.CurrentUnitCount));
                    break;
                default:
                    BurstSafe.UnexpectedEnum(_currentArmyGroupSortType);
                    break;
            }

            for (var i = 0; i < SlotComponents.Count; i++)
            {
                var slotComponent = SlotComponents[i];
                var slot = Slots[i];
                if (i < armyGroupSlotInfos.Count)
                {
                    var info = armyGroupSlotInfos[i];
                    slot.SetActive(true);
                    slotComponent.SetTarget(info);
                }
                else
                {
                    slot.SetActive(false);
                }
            }
        }

        public void SelectArmyGroupUnits(Entity armyGroup, bool ifAdd, int slotIndex)
        {
            OnEcsSelectArmyGroupUnits?.Invoke(armyGroup, ifAdd);
            if (!ifAdd)
            {
                for (int i = 0; i < SlotComponents.Count; i++)
                {
                    var slotComponent = SlotComponents[i];
                    if (i == slotIndex) continue;
                    slotComponent.SlotMoveLeft();
                }
            }
        }

        public void SprintArmyGroupUnits(Entity armyGroup)
        {
            OnEcsSprintArmyGroupUnits?.Invoke(armyGroup);
        }

        public void HoldSwitchArmyGroupUnits(Entity armyGroup)
        {
            OnEcsHoldSwitchArmyGroup?.Invoke(armyGroup);
        }

        public void TryAddSelectedUnitsToArmyGroup(Entity armyGroup)
        {
            OnEcsCheckSelected?.Invoke();
            if (HasSelectedUnitAlreadyInArmyGroup)
            {
                ArmyGroupAddTypeSelectWindow.Instance.Show();
                ArmyGroupAddTypeSelectWindow.Instance.SetTarget(armyGroup);
            }
            else
            {
                OnEcsAddToArmyGroup?.Invoke(armyGroup,
                    AddToArmyGroupType.AllSelectedExceptAlreadyIn);
            }
        }

        public void CastSkill(Entity armyGroup)
        {
            OnEcsCastSkill?.Invoke(armyGroup);
        }


        #region ButtonMethods

        public void OnClickEnterSelectionMode()
        {
            SwitchSelectionMode(true);
        }

        public void OnClickChangeSortTypeButton()
        {
            if (_currentArmyGroupSortType == ArmyGroupSortType.ByCurrentUnitCountDescending)
            {
                _currentArmyGroupSortType = ArmyGroupSortType.ByCreateTimeAscending;
            }
            else
            {
                var sortTypeValue = (int)_currentArmyGroupSortType + 1;
                _currentArmyGroupSortType = (ArmyGroupSortType)sortTypeValue;
            }

            UpdateSortButtonIcon();
        }

        public void OnClickRemoveSelectedUnitsFromTheirArmyGroup()
        {
            OnEcsRemoveSelectedUnitsFromTheirArmyGroup?.Invoke();
        }

        public void OnClickQuickSelectAllUnitsWithoutArmyGroupAndGarrisoned()
        {
            OnEcsSelectAllUnitsWithoutArmyGroupAndGarrisoned?.Invoke(_tierFilterEnabled, _currentFilterTier,
                _currentFilterUnitTypes);
        }

        public void OnClickExitSelectionMode()
        {
            SwitchSelectionMode(false);
            OnEcsUpdateArmyGroupAvgData?.Invoke();
        }

        public void OnClickTierFilterButton()
        {
            using var query =
                World.DefaultGameObjectInjectionWorld.EntityManager.CreateEntityQuery(typeof(UnitSelectionFilter));
            var data = query.GetSingletonRW<UnitSelectionFilter>();
            if (!_tierFilterEnabled)
            {
                _tierFilterEnabled = true;
                _currentFilterTier = Tier.Tier1;
                tierFilterButtonImage.color = Color.white;
                tierFilterButtonImage.sprite = BasicUIResourceManager.Instance.TierSprites[_currentFilterTier];
                data.ValueRW.TierFilterEnabled = _tierFilterEnabled;
                data.ValueRW.FilteredUnitTier = _currentFilterTier;
                return;
            }

            if (_currentFilterTier == MaxTier)
            {
                _tierFilterEnabled = false;
                tierFilterButtonImage.color = Color.gray;
                data.ValueRW.TierFilterEnabled = _tierFilterEnabled;
                data.ValueRW.FilteredUnitTier = _currentFilterTier;
                return;
            }

            _currentFilterTier = (Tier)((int)_currentFilterTier + 1);
            tierFilterButtonImage.sprite = BasicUIResourceManager.Instance.TierSprites[_currentFilterTier];
            data.ValueRW.TierFilterEnabled = _tierFilterEnabled;
            data.ValueRW.FilteredUnitTier = _currentFilterTier;
        }

        public void OnClickUnitTypeFilter(int typeIndex)
        {
            using var query =
                World.DefaultGameObjectInjectionWorld.EntityManager.CreateEntityQuery(typeof(UnitSelectionFilter));
            var data = query.GetSingletonRW<UnitSelectionFilter>();

            var unitType = (UnitType)typeIndex;
            if (_currentFilterUnitTypes.Contains(unitType))
            {
                _currentFilterUnitTypes.Remove(unitType);
                unitTypeFilterSelectedImages[typeIndex].enabled = false;
                data.ValueRW.FilteredUnitTypes.Remove(typeIndex);
            }
            else
            {
                _currentFilterUnitTypes.Add(unitType);
                unitTypeFilterSelectedImages[typeIndex].enabled = true;
                data.ValueRW.FilteredUnitTypes.Add(typeIndex);
            }
        }

        #endregion


        #region EventFunctions

        private void Awake()
        {
            if (!Instance)
                Instance = this;
            else
                Destroy(gameObject);
        }

        protected override void Start()
        {
            base.Start();

            _currentArmyGroupSortType = ArmyGroupSortType.ByCreateTimeAscending;
            UpdateSortButtonIcon();

            foreach (var image in unitTypeFilterSelectedImages)
            {
                image.enabled = true;
            }

            tierFilterButtonImage.color = Color.gray;

            if (!initNoUnitTypeSelected)
            {
                foreach (UnitType unitType in Enum.GetValues(typeof(UnitType)))
                {
                    _currentFilterUnitTypes.Add(unitType);
                }
            }

            foreach (var image in unitTypeFilterSelectedImages)
            {
                image.enabled = !initNoUnitTypeSelected;
            }

            accurateSelectionPanel.SetActive(false);
        }

        #endregion

        private ArmyGroupSortType _currentArmyGroupSortType;
        private bool _tierFilterEnabled;
        private const Tier MaxTier = Tier.Tier3;
        private Tier _currentFilterTier;
        private readonly List<UnitType> _currentFilterUnitTypes = new();

        private void UpdateSortButtonIcon()
        {
            foreach (var sortIcon in sortIcons)
            {
                if (sortIcon.type == _currentArmyGroupSortType)
                {
                    sortImage.sprite = sortIcon.sprite;
                    break;
                }
            }
        }

        public enum ArmyGroupSortType
        {
            ByCreateTimeAscending = 0,
            ByCreateTimeDescending = 1,
            ByCurrentUnitCountDescending = 2,
            // ByCurrentUnitCountAscending = 2,
        }

        [Serializable]
        public struct ArmyGroupSortTypeIcon
        {
            public ArmyGroupSortType type;
            public Sprite sprite;
        }
    }

    public struct ArmyGroupSlotInfo
    {
        public Entity ArmyGroup;
        public ArmyGroupIconType IconType;
        public float ChargeRatio;
        public float HpRatio;
        public int CurrentUnitCount;

        public int StartingUnitCount;

        // Left cool-down time/ Total cool-down time 
        public float SprintCooldownRatio;
        public bool IsHolding;
        public float CreateTimeTotalHours;
    }
}