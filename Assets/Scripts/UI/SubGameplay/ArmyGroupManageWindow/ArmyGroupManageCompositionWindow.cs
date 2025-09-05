using System;
using System.Collections.Generic;
using SparFlame.Components.General;
using SparFlame.Components.MainGameplay;
using SparFlame.Components.SubGameplay;
using SparFlame.Database;
using SparFlame.Systems.General.BasicControl;
using SparFlame.UI.General;
using Unity.Entities;
using UnityEngine;
using UnityEngine.UI;

namespace SparFlame.UI.MainGameplay
{
    public class ArmyGroupManageCompositionWindow : MultiSlotWindowUtils.MultiSlotsWindow<ArmyGroupUnitCompositionSlot>, MultiSlotWindowUtils.ISingleTargetWindow
    {
        // Config
        [SerializeField] private GameObject generalPanel;

        [SerializeField] private Image tierFilterButtonImage;
        [SerializeField] private List<Image> unitTypeFilterSelectedImages;
        
        
        public static ArmyGroupManageCompositionWindow Instance;
        public event Action<List<ArmyGroupUnitTypeData>, Entity> OnEcsRemoveSelectedFromArmyGroup; 


        public bool TrySwitchTarget(Entity target)
        {
            _em = World.DefaultGameObjectInjectionWorld.EntityManager;
            if (!_em.HasComponent<ArmyGroupAttr>(target))
                return false;
            _targetEntity = target;
            ShowComposition();
            return true;
        }

        public bool HasTarget()
        {
            return _targetEntity != Entity.Null;
        }

        public void ClearCloseUpTarget()
        {
            _targetEntity = Entity.Null;
        }

        public override void Show(Vector2? pos = null)
        {
            base.Show(pos);
            generalPanel.SetActive(true);
            ClearSelected();
        }

        public override void Hide()
        {
            base.Hide();
            ClearCloseUpTarget();
            generalPanel.SetActive(false);
            _selectedUnits.Clear();
        }

        #region ButtonMethods

        public void OnClickTierFilter()
        {
            ClearSelected();
            if (!_tierFilterEnabled)
            {
                _tierFilterEnabled = true;
                _currentFilterTier = Tier.Tier1;
                tierFilterButtonImage.color = Color.white;
                tierFilterButtonImage.sprite = BasicUIResourceManager.Instance.TierSprites[_currentFilterTier];
                return;
            }

            if (_currentFilterTier == MaxTier)
            {
                _tierFilterEnabled = false;
                tierFilterButtonImage.color = Color.gray;
                return;
            }
            _currentFilterTier = (Tier)((int)_currentFilterTier + 1);
            tierFilterButtonImage.sprite = BasicUIResourceManager.Instance.TierSprites[_currentFilterTier];
            
        }

        public void OnClickUnitTypeFilter(int typeIndex)
        {
            ClearSelected();
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
        }

        public void OnClickClose()
        {
            Hide();
        }

        public override void OnClickSlot(int slotIndex)
        {
            var slotComponent = SlotComponents[slotIndex];
            var data = slotComponent.GetData();
            if (_selectedUnits.Contains(data))
            {
                _selectedUnits.Remove(data);
                slotComponent.ToggleSelected(false);
            }
            else
            {
                _selectedUnits.Add(data);
                slotComponent.ToggleSelected(true);
            }
        }

        public void OnClickSelectAll()
        {
            var typeDatas = _em.GetBuffer<ArmyGroupUnitTypeData>(_targetEntity);
            if (_selectedUnits.Count == typeDatas.Length)
            {
                _selectedUnits.Clear();
                foreach (var slotComponent in SlotComponents)
                {
                    slotComponent.ToggleSelected(false);
                }
                return;
            }
            _selectedUnits.Clear();
            for (var i = 0; i < SlotComponents.Count; i++)
            {
                if(i >= typeDatas.Length)break;
                if(!Slots[i].activeSelf)continue;
                var slotComponent = SlotComponents[i];
                var data = slotComponent.GetData();
                _selectedUnits.Add(data);
                slotComponent.ToggleSelected(true);
            }
        }

        public void OnClickRemoveSelected()
        {
            OnEcsRemoveSelectedFromArmyGroup?.Invoke(_selectedUnits, _targetEntity);
            _selectedUnits.Clear();
            foreach (var slotComponent in SlotComponents)
            {
                slotComponent.ToggleSelected(false);
            }
        }

        #endregion

        private Entity _targetEntity;
        private EntityManager _em;
        private const Tier MaxTier = Tier.Tier3;
        private Tier _currentFilterTier = MaxTier;
        private bool _tierFilterEnabled ;
        private readonly List<UnitType> _currentFilterUnitTypes = new();
        private readonly List<ArmyGroupUnitTypeData> _selectedUnits = new();
        
        #region EventFunctions

        private void Awake()
        {
            if(!Instance)
                Instance = this;
            else
                Destroy(gameObject);
        }

        protected override void Start()
        {
            base.Start();
            tierFilterButtonImage.color = Color.gray;
            _currentFilterUnitTypes.Add(UnitType.Cavalry);
            _currentFilterUnitTypes.Add(UnitType.Ranged);
            _currentFilterUnitTypes.Add(UnitType.Shield);
            _currentFilterUnitTypes.Add(UnitType.Magic);
            _currentFilterUnitTypes.Add(UnitType.Worker);
            foreach (var image in unitTypeFilterSelectedImages)
            {
                image.enabled = true;
            }
            Hide();
        }

        private void Update()
        {
            if (IsOpened() && HasTarget())
            {
                ShowComposition();
            }
        }

        #endregion

        private void ClearSelected()
        {
            _selectedUnits.Clear();
            foreach (var slot in SlotComponents)
            {
                slot.ToggleSelected(false);
            }
        }
        private void ShowComposition()
        {
            var unitTypeDatas = _em.GetBuffer<ArmyGroupUnitTypeData>(_targetEntity);

            int i = 0;
            foreach(var unitTypeData in unitTypeDatas)
            {
                if (i >= Slots.Count) break;
                var item = DatabaseManager.UnitDatabaseSo.GetItemById(unitTypeData.Id);
                if(_tierFilterEnabled && _currentFilterTier != item.curTier)continue;
                if(!_currentFilterUnitTypes.Contains(item.type))continue;
                var slot = Slots[i];
                var slotComponent = SlotComponents[i];
                slot.SetActive(true);
                slotComponent.SetTarget(unitTypeData);
                i++;
            }

            for (int index = i; index < Slots.Count; index++)
            {
                var slot = Slots[index];
                slot.SetActive(false);
            }
        }
    }
}