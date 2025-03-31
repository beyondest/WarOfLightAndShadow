using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using SparFlame.GamePlaySystem.Units;
using SparFlame.UI.General;
using Unity.Entities;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

// ReSharper disable PossibleNullReferenceException

namespace SparFlame.UI.GamePlay
{
    public class UnitMulti2DWindow : UIWindow
    {
        public static UnitMulti2DWindow Instance;

        [Header("Custom Config")] public GameObject multiUnitPanel;
        public GameObject pageUpButton;
        public GameObject pageDownButton;
        [CanBeNull] public string unitMulti2DSpriteSuffix;
        
        
        [Header("Multi unit slot config")] 
        public AssetReference slotAsset;
        public MultiShowSlotConfig multiShowSlotConfig;

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

        public void UpdateSelectedUnitView()
        {
            if(!_unitSpritesHandle.IsDone || !_slotPrefabHandle.IsDone)return;
            var startIdx = _currentPage * _slotsMaxCountPerPage;
            var count = Mathf.Min(_slotsMaxCountPerPage, _unitInfos.Count - startIdx);
            // Update corresponding images and hp sliders
            for (var i = 0; i < _slotsMaxCountPerPage; i++)
            {
                if (i < count)
                {
                    _slots[i].SetActive(true);
                    var unitShowSlot = _slots[i].GetComponent<UnitMulti2DSlot>();
                    var unitInfo = _unitInfos[startIdx + i];
                    unitShowSlot.button.image.sprite = _unitSpriteDict[unitInfo.UnitType];
                    unitShowSlot.hp.value = unitInfo.HpRatio;
                }
                else
                {
                    _slots[i].SetActive(false);
                }
            }
    
            // Update right and left button
            pageDownButton.SetActive((_currentPage + 1) * _slotsMaxCountPerPage < _unitInfos.Count);
            pageUpButton.SetActive(_currentPage != 0);
        }

        public void ClearUnitInfo()
        {
            _unitInfos.Clear();
        }
        public void AddUnitInfo(in UnitInfo unitInfo)
        {
            _unitInfos.Add(unitInfo);
        }
        
        public struct UnitInfo
        {
            public UnitType UnitType;
            public float HpRatio;
            public Entity Entity;
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
            if (_unitInfos.Count <= index) return;
            if (_currentSelectIndex == index) return;
            // Set close up target 
            InfoWindowController.Instance.UpdateCloseUpTarget(_unitInfos[index + _currentPage * _slotsMaxCountPerPage].Entity);
        }

        #endregion


        private int _slotsMaxCountPerPage;
        private int _currentPage;
        private int _currentSelectIndex = -1;
        private List<GameObject> _slots = new();
        private Dictionary<UnitType, Sprite> _unitSpriteDict = new();
        private readonly List<UnitInfo> _unitInfos = new();
        private GameObject _slotPrefab;
        private AsyncOperationHandle<GameObject> _slotPrefabHandle;
        private AsyncOperationHandle<IList<Sprite>> _unitSpritesHandle;



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
            _slotPrefabHandle = CR.LoadPrefabAddressableRefAsync<GameObject>(slotAsset, o =>
            {
                _slotPrefab = o;
            });
            _unitSpritesHandle = CR.LoadTypeSuffixAddressableAsync<UnitType, Sprite>(unitMulti2DSpriteSuffix, result =>
                CR.OnTypeSuffixAddressableLoadComplete(result, _unitSpriteDict));
        }

        private void Start()
        {
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
            _unitSpriteDict.Clear();
            _unitInfos.Clear();
            Addressables.Release(_slotPrefabHandle);
            Addressables.Release(_unitSpritesHandle);
        }
        #endregion

        private void OnPrefabLoadComplete(AsyncOperationHandle<GameObject> handle)
        {
            _slotPrefab = handle.Result;
            UIUtils.InitMultiShowSlots(ref _slots, multiUnitPanel, _slotPrefab, in multiShowSlotConfig, OnClickUnit2D);
        }

        private void OnUnitSpritesLoadComplete(AsyncOperationHandle<IList<Sprite>> handle)
        {
            var keys = Enum.GetValues(typeof(UnitType));
            var sprites = handle.Result;
            var i = 0;
            foreach (UnitType key in keys)
            {
                _unitSpriteDict.Add(key, sprites[i]);
                i++;
            }
        }
        
    }
}