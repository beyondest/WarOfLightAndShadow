using System;
using System.Collections.Generic;
using SparFlame.Components.General;
using SparFlame.Components.SubGameplay;
using SparFlame.Systems.General.BasicControl;
using SparFlame.UI.General;
using SparFlame.UI.SubGameplay.StaticWindows;
using TMPro;
using Unity.Entities;
using UnityEngine;
using UnityEngine.UI;

namespace SparFlame.UI.MainGameplay
{
    public class ArmyGroupManageWindow : MultiSlotWindowUtils.MultiSlotsWindow<ArmyGroupManageInfoSlot>
    {
        // Config

        [SerializeField] private GameObject totalPanel;

        [SerializeField] private TMP_Text armyGroupAllowedCountText;
        
        [SerializeField] private Image tierFilterButtonImage;
        [SerializeField] private List<Image> unitTypeFilterSelectedImages;
        // Interface
        public static ArmyGroupManageWindow Instance;
        public event Action OnEcsUpdateStaticData;
        public event Action<Entity> OnEcsDeleteArmyGroup;
        public event Action<int, int> OnEcsTryNewArmyGroup;
        
        public void UpdateStaticData(List<ArmyGroupManageInfo> infos,  int maxGarrisonCount)
        {
            _maxGarrisonCount = maxGarrisonCount;
            _infos.Clear();
            _infos.AddRange(infos);
            for (var i = 0; i < Slots.Count; i++)
            {
                var slot = Slots[i];
                var slotComponent = SlotComponents[i];
                if (i < infos.Count)
                {
                    slotComponent.SetTarget(infos[i]);
                    slotComponent.UpdateComposition(_tierFilterEnabled, _currentFilterTier,_currentFilterUnitTypes);
                    slot.SetActive(true);
                }
                else
                {
                    slot.SetActive(false);
                }
            }
            armyGroupAllowedCountText.text = _infos.Count + "/" +maxGarrisonCount ;
        }

        public void DeleteArmyGroup(int index)
        {
            var armyGroup = _infos[index].ArmyGroupEntity;
            OnEcsDeleteArmyGroup?.Invoke(armyGroup);
        }

      

        public override void Show(Vector2? pos = null)
        {
            base.Show(pos);
            totalPanel.SetActive(true);
        }

        public override void Hide()
        {
            base.Hide();
            totalPanel.SetActive(false);
        }

        public override bool IsOpened()
        {
            return totalPanel.activeSelf;
        }

        #region ButtonMethods

        public void OnClickOpenArmyGroupManageWindow()
        {
            OnEcsUpdateStaticData?.Invoke();
            Show();
        }

        public void OnClickCloseArmyGroupManageWindow()
        {
            Hide();
        }


       

        public void OnClickNewArmyGroup()
        {
            OnEcsTryNewArmyGroup?.Invoke(_infos.Count, config.rows * config.cols);
        }

  
        public void OnClickTierFilter()
        {
            foreach (var slot in SlotComponents)
            {
                slot.ClearSelected();
            }
            if (!_tierFilterEnabled)
            {
                _tierFilterEnabled = true;
                _currentFilterTier = Tier.Tier1;
                tierFilterButtonImage.color = Color.white;
                tierFilterButtonImage.sprite = BasicUIResourceManager.Instance.TierSprites[_currentFilterTier];
                for (int i = 0; i < _infos.Count; i++)
                {
                    var slotComponent = SlotComponents[i];
                    slotComponent.UpdateComposition(_tierFilterEnabled, _currentFilterTier, _currentFilterUnitTypes);
                }
                return;
            }

            if (_currentFilterTier == MaxTier)
            {
                _tierFilterEnabled = false;
                tierFilterButtonImage.color = Color.gray;
                for (int i = 0; i < _infos.Count; i++)
                {
                    var slotComponent = SlotComponents[i];
                    slotComponent.UpdateComposition(_tierFilterEnabled, _currentFilterTier, _currentFilterUnitTypes);
                }
                return;
            }
            _currentFilterTier = (Tier)((int)_currentFilterTier + 1);
            tierFilterButtonImage.sprite = BasicUIResourceManager.Instance.TierSprites[_currentFilterTier];

            for (var i = 0; i < _infos.Count; i++)
            {
                var slotComponent = SlotComponents[i];
                slotComponent.UpdateComposition(_tierFilterEnabled, _currentFilterTier, _currentFilterUnitTypes);
            }
        }

        public void OnClickUnitTypeFilter(int typeIndex)
        {
            foreach (var slot in SlotComponents)
            {
                slot.ClearSelected();
            }
            var unitType = (UnitType)typeIndex;
            if (_currentFilterUnitTypes.Contains(unitType))
            {
                _currentFilterUnitTypes.Remove(unitType);
                unitTypeFilterSelectedImages[typeIndex].enabled = false;
            }
            else
            {
                _currentFilterUnitTypes.Add(unitType);
                unitTypeFilterSelectedImages[typeIndex].enabled = true;
            }
            for (int i = 0; i < _infos.Count; i++)
            {
                var slotComponent = SlotComponents[i];
                slotComponent.UpdateComposition(_tierFilterEnabled, _currentFilterTier, _currentFilterUnitTypes);
            }
        }
        
        
        #endregion

        private readonly List<ArmyGroupManageInfo> _infos = new();
        private EntityManager _em;
        private int _maxGarrisonCount;
        private const Tier MaxTier = Tier.Tier3;
        private bool _tierFilterEnabled ;

        private Tier _currentFilterTier = MaxTier;

        private readonly List<UnitType> _currentFilterUnitTypes = new();

        
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
            Hide();
            foreach (var image in unitTypeFilterSelectedImages)
            {
                image.enabled = true;
            }
            tierFilterButtonImage.color = Color.gray;
            _currentFilterUnitTypes.Add(UnitType.Cavalry);
            _currentFilterUnitTypes.Add(UnitType.Ranged);
            _currentFilterUnitTypes.Add(UnitType.Shield);
            _currentFilterUnitTypes.Add(UnitType.Magic);
            _currentFilterUnitTypes.Add(UnitType.Worker);
        }
    }

    public struct ArmyGroupManageInfo
    {
        public Entity ArmyGroupEntity;
       
    }
}