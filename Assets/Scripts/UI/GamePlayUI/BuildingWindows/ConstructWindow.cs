using System;
using System.Collections.Generic;
using SparFlame.GamePlaySystem.Building;
using SparFlame.GamePlaySystem.General;
using SparFlame.GamePlaySystem.Mouse;
using SparFlame.GamePlaySystem.UnitSelection;
using SparFlame.UI.General;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

// ReSharper disable PossibleNullReferenceException

namespace SparFlame.UI.GamePlay
{
    public class ConstructWindow : UIUtils.UIWindow
    {
        public static ConstructWindow Instance;
        [NonSerialized] public bool InitWindowEvents = false;
        [Header("Custom config")] public GameObject constructPanel;
        public KeyCode exitGhostModeKey = KeyCode.Escape;

        [Header("Multi building slot config")] public AssetReferenceGameObject buildingSlotPrefab;
        public UIUtils.MultiShowSlotConfig multiShowSlotConfig;

        public Action<BuildingType, int> EcsGhostShowTarget;
        public Action EcsExitGhostShow;
        public Action EcsBuildTarget;


        #region ButtonMethods
        public void OnClickBuildingSprite(int slotIndex)
        {
            EcsGhostShowTarget?.Invoke(_currentBuildingType,_saveIndices[slotIndex]);
        }
        
        
        public void OnClickBuildingTypeButton(BuildingType buildingType)
        {
            _currentBuildingType = buildingType;
            UpdateBuildingCandidates();
        }

        public void OnClickSubTypeButton(int subType)
        {
            _currentSubType = _currentSubType == subType ? -1 : subType;
            UpdateBuildingCandidates();
        }

        public void OnClickTierButton(Tier tier)
        {
            _currentTier = _currentTier == tier ? Tier.None : tier;
            UpdateBuildingCandidates();
        }
        #endregion
        
        public override void Show(Vector2? pos = null)
        {
            constructPanel.SetActive(true);
            UpdateBuildingCandidates();
        }

        public override void Hide()
        {
            constructPanel.SetActive(false);
        }

        public override bool IsOpened()
        {
            return constructPanel.activeSelf;
        }


        // Internal Data


        private int _currentSubType = -1;
        private Tier _currentTier = Tier.None;
        private BuildingType _currentBuildingType;
        private AsyncOperationHandle<GameObject> _slotPrefabHandle;


        // Cache
        private GameObject _buildingSlotPrefab;
        private readonly List<Sprite> _buildingSprites = new();
        private readonly List<int> _saveIndices = new();
        private readonly List<GameObject> _buildingSlots = new();

        private void Awake()
        {
            if (Instance == null)
                Instance = this;
            else
                Destroy(gameObject);
        }

        private void OnEnable()
        {
            _slotPrefabHandle = CR.LoadAssetRefAsync<GameObject>(buildingSlotPrefab, prefab =>
            {
                _buildingSlotPrefab = prefab;
                UIUtils.InitMultiShowSlotsByIndex(_buildingSlots, constructPanel, _buildingSlotPrefab,
                    in multiShowSlotConfig,
                    OnClickBuildingSprite);
            });
        }

        private void OnDisable()
        {
            Addressables.Release(_slotPrefabHandle);
            _buildingSlots.Clear();
        }


        private void UpdateBuildingCandidates()
        {
            if (!BuildingWindowResourceManager.Instance.IsResourceLoaded() || !_slotPrefabHandle.IsDone) return;
            _buildingSprites.Clear();
            _saveIndices.Clear();
            BuildingWindowResourceManager.Instance.GetFilteredSprites(_currentBuildingType, _buildingSprites,
                _saveIndices, _currentSubType, _currentTier);

            var count = Mathf.Min(_buildingSlots.Count, _buildingSprites.Count);
            for (var i = 0; i < _buildingSlots.Count; i++)
            {
                if (i < count)
                {
                    _buildingSlots[i].SetActive(true);
                    var buildingSlot = _buildingSlots[i].GetComponent<BuildingSlot>();
                    buildingSlot.button.image.sprite = _buildingSprites[i];
                }
                else
                {
                    _buildingSlots[i].SetActive(false);
                }
            }
        }

     
    }


}