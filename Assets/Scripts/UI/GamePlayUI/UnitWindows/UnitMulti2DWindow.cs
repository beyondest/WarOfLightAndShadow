using System;
using SparFlame.UI.General;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

// ReSharper disable PossibleNullReferenceException

namespace SparFlame.UI.GamePlay
{
    public class UnitMulti2DWindow : UIUtils.MultiSlotsWindow<Unit2DSlot,UnitMulti2DWindow>
    {
        // Config
        [Header("Custom Config")] [SerializeField]
        private GameObject pageUpButton;

        [SerializeField] private GameObject pageDownButton;


        // Interface
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

        public void UpdateSelectedUnitView(NativeList<UnitRealTimeInfo> unitInfos)
        {
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
                    unitShowSlot.button.image.sprite =
                        UnitWindowResourceManager.Instance.UnitSprites[unitInfo.UnitType];
                    unitShowSlot.hp.value = unitInfo.HpRatio;
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

        private int _slotsMaxCountPerPage;
        private int _currentPage;
        private int _currentSelectIndex = -1;
        private int _currentSelectCounts;
        private Entity _targetEntity;
        
        #region EventFunction

        protected override void OnEnable()
        {
            base.OnEnable();
            _slotsMaxCountPerPage = config.rows * config.cols;
            _currentSelectIndex = -1;
        }

        #endregion
    }
}