using System;
using SparFlame.GamePlaySystem.Exp;
using SparFlame.GamePlaySystem.General;
using SparFlame.UI.General;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

// ReSharper disable PossibleNullReferenceException

namespace SparFlame.UI.GamePlay
{
    public class UnitMulti2DWindow : MultiSlotWindowUtils.MultiSlotsWindow<Unit2DSlot>
    {
        // Config
        [Header("Custom Config")] [SerializeField]
        private GameObject pageUpButton;

        [SerializeField] private Tier maxTier;
        [SerializeField] private GameObject pageDownButton;
        

        // Interface
        public static UnitMulti2DWindow Instance;
        public Action<int> GetTargetEntityByIndex;

        public override void OnClickSlot(int slotIndex)
        {
            var trueIndex = _currentPage * _slotsMaxCountPerPage + slotIndex;
            if (_currentSelectIndex == trueIndex) return;
            // Set close up target 
            GetTargetEntityByIndex?.Invoke(trueIndex);
            if (_currentSelectCounts <= trueIndex) return;
            InfoWindowController.Instance.UpdateCloseUpTarget(_targetEntity);
        }

        public bool HasTarget()
        {
            return _currentSelectCounts > 0;
        }

        public override void Hide()
        {
            base.Hide();
            _currentSelectIndex = -1;
        }

        public void GetUnitData(int curSelectCount, Entity target)
        {
            _currentSelectCounts = curSelectCount;
            _targetEntity = target;
            
        }

        public void UpdateSelectedUnitView(NativeList<UnitRealTimeInfo> unitInfos, FactionTag faction)
        {
            _currentSelectFaction = faction;
            if (!UnitWindowResourceManager.Instance.IsResourceLoaded() || !SlotPrefabHandle.IsDone) return;
            var startIdx = _currentPage * _slotsMaxCountPerPage;
            var count = Mathf.Min(_slotsMaxCountPerPage, unitInfos.Length - startIdx);
            // Update corresponding images and hp sliders
            for (var i = 0; i < _slotsMaxCountPerPage; i++)
            {
                if (i < count)
                {
                    Slots[i].SetActive(true);
                    var unitShowSlot = SlotComponents[i];
                    var unitInfo = unitInfos[startIdx + i];
                    unitShowSlot.SetTarget(unitInfo, _currentSelectFaction,
                        _maxTierF
                        );
                    // unitShowSlot.button.image.sprite =
                    //     UnitWindowResourceManager.Instance.UnitGeneralTypeSprites[unitInfo.UnitType];
                    // unitShowSlot.hpFilled.fillAmount = unitInfo.HpRatio;
                    // var tier = (int)unitInfo.Tier - 2;
                    // unitShowSlot.tierImage.fillAmount = tier / _maxTierF;
                }
                else
                {
                    Slots[i].SetActive(false);
                }
            }

            // Update right and left button
            pageDownButton.SetActive((_currentPage + 1) * _slotsMaxCountPerPage < unitInfos.Length);
            pageUpButton.SetActive(_currentPage != 0);
            _currentSelectCounts = unitInfos.Length;
        }


        #region ButtonMethods
        public void OnPageRightClicked()
        {
            _currentPage++;
        }

        public void OnPageLeftClicked()
        {
            _currentPage--;
        }

        #endregion

        
        // Internal Data
        private int _slotsMaxCountPerPage;
        private int _currentPage;
        private int _currentSelectIndex = -1;
        private int _currentSelectCounts;
        private float _maxTierF;
        private FactionTag _currentSelectFaction;
        private Entity _targetEntity;
        
        #region EventFunction

        private void Awake()
        {
            if(Instance == null)
                Instance = this;
            else
                Destroy(gameObject);
        }

        public override void LoadResources()
        {
            base.LoadResources();
            _slotsMaxCountPerPage = config.rows * config.cols;
            _currentSelectIndex = -1;
             _maxTierF = (int)maxTier - 2;
        }

        #endregion
    }
}