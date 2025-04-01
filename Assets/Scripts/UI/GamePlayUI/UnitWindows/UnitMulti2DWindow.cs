using System;
using System.Collections.Generic;
using SparFlame.UI.General;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

// ReSharper disable PossibleNullReferenceException

namespace SparFlame.UI.GamePlay
{
    public class UnitMulti2DWindow : UIUtils.UIWindow
    {
        // Config
        [Header("Custom Config")] 
        public GameObject multiUnitPanel;
        public GameObject pageUpButton;
        public GameObject pageDownButton;
        
        
        [Header("Multi unit slot config")] 
        public AssetReference slotAsset;
        public UIUtils.MultiShowSlotConfig multiShowSlotConfig;
        
        
        // Interface
        public static UnitMulti2DWindow Instance;
        public Action<int> GetTargetEntityByIndex;

        
        public override void Show(Vector2? pos = null)
        {
            multiUnitPanel.SetActive(true);
        }

        public override void Hide()
        {
            multiUnitPanel.SetActive(false);
            _currentSelectIndex = -1;
        }

        public override bool IsOpened()
        {
            return multiUnitPanel.activeSelf;
        }

        public void GetUnitData(int curSelectCount, Entity target)
        {
            _currentSelectCounts = curSelectCount;
            _targetEntity = target;
        }
        public void UpdateSelectedUnitView(NativeList<UnitRealTimeInfo> unitInfos)
        {
            if(!UnitWindowResourceManager.Instance.IsResourceLoaded() || !_slotPrefabHandle.IsDone)return;
            var startIdx = _currentPage * _slotsMaxCountPerPage;
            var count = Mathf.Min(_slotsMaxCountPerPage, unitInfos.Length - startIdx);
            // Update corresponding images and hp sliders
            for (var i = 0; i < _slotsMaxCountPerPage; i++)
            {
                if (i < count)
                {
                    _slots[i].SetActive(true);
                    var unitShowSlot = _slots[i].GetComponent<UnitMulti2DSlot>();
                    var unitInfo = unitInfos[startIdx + i];
                    unitShowSlot.button.image.sprite = UnitWindowResourceManager.Instance.UnitSprites[unitInfo.UnitType];
                    unitShowSlot.hp.value = unitInfo.HpRatio;
                }
                else
                {
                    _slots[i].SetActive(false);
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

        public void OnClickUnit2D(int index)
        {
            var trueIndex = _currentPage * _slotsMaxCountPerPage + index;
            if(_currentSelectIndex == trueIndex) return;
            // Set close up target 
            GetTargetEntityByIndex?.Invoke(trueIndex);
            if (_currentSelectCounts <= trueIndex) return;
            InfoWindowController.Instance.UpdateCloseUpTarget(_targetEntity);
        }
        #endregion


        private int _slotsMaxCountPerPage;
        private int _currentPage;
        private int _currentSelectIndex = -1;
        private int _currentSelectCounts;
        private readonly List<GameObject> _slots = new();
        private GameObject _slotPrefab;
        private AsyncOperationHandle<GameObject> _slotPrefabHandle;
        private Entity _targetEntity;


        #region EventFunction
        private void Awake()
        {
            if (Instance == null)
                Instance = this;
            else
                Destroy(gameObject);
        }


        private void OnEnable()
        {
            _slotPrefabHandle = CR.LoadAssetRefAsync<GameObject>(slotAsset, slotPrefab =>
            {
                _slotPrefab = slotPrefab;
                UIUtils.InitMultiShowSlotsByIndex(_slots,multiUnitPanel,_slotPrefab,multiShowSlotConfig,OnClickUnit2D);
            });
            _slotsMaxCountPerPage = multiShowSlotConfig.rows * multiShowSlotConfig.cols;
            _currentSelectIndex = -1;
            multiUnitPanel.SetActive(false);
        }
        

        private void OnDisable()
        {
            foreach (var go in _slots)
            {
                Destroy(go);
            }
            _slots.Clear();
            Addressables.Release(_slotPrefabHandle);
        }
        #endregion
 
        
    }
}