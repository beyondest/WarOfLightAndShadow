using System;
using System.Collections.Generic;
using System.Net.NetworkInformation;
using SparFlame.BootStrapper;
using SparFlame.GamePlaySystem.Building;
using SparFlame.GamePlaySystem.CustomInput;
using SparFlame.GamePlaySystem.General;
using SparFlame.UI.General;
using UnityEngine;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.UI;

// ReSharper disable PossibleNullReferenceException

namespace SparFlame.UI.GamePlay
{
    public class ConstructWindow : UIUtils.MultiSlotsWindow<BuildingSlot>
    {
        [Header("Custom config")] [SerializeField]
        private GameObject constructEnterButton;

        [SerializeField] private GameObject constructExitButton;


        // Interaface
        public static ConstructWindow Instance;
        [NonSerialized] public bool InitWindowEvents = false;
        public Action<BuildingType, int> EcsGhostShowTargetByTypeIndex;
        public Action EcsExitGhostShow;


        #region ButtonMethods

        public override void OnClickSlot(int slotIndex)
        {
            EcsGhostShowTargetByTypeIndex?.Invoke(_currentBuildingType, _saveIndices[slotIndex]);
        }

        public void OnClickConstructEnter()
        {
            // TODO : Hide Construct enter when enter fly mode or any other modes that cannot construct
            Show();
        }

        public void OnClickConstructExit()
        {
            Hide();
            EcsExitGhostShow?.Invoke();
        }

        public void OnClickBuildingTypeButton(int buildingType)
        {
            _currentBuildingType = (BuildingType)buildingType;
            UpdateBuildingCandidates();
        }

        public void OnClickSubTypeButton(int subType)
        {
            _currentSubType = _currentSubType == subType ? -1 : subType;
            UpdateBuildingCandidates();
        }

        public void OnClickTierButton(Tier tier)
        {
            _currentTier = _currentTier == tier ? Tier.TierNone : tier;
            UpdateBuildingCandidates();
        }

        #endregion

        public override void Show(Vector2? pos = null)
        {
            base.Show(pos);
            InputListener.Instance.ToggleConstructMap();
            UpdateBuildingCandidates();
            constructEnterButton.SetActive(false);
            constructExitButton.SetActive(true);
        }

        public override void Hide()
        {
            base.Hide();
            InputListener.Instance.ToggleConstructMap();
            constructExitButton.SetActive(false);
            constructEnterButton.SetActive(true);
        }


        // Internal Data


        private int _currentSubType = -1;
        private Tier _currentTier = Tier.TierNone;
        private BuildingType _currentBuildingType;


        // Cache
        private readonly List<Sprite> _buildingSprites = new();
        private readonly List<int> _saveIndices = new();
        private readonly List<string> _buildingNames = new();

        private void Awake()
        {
            if (Instance == null)
                Instance = this;
            else
                Destroy(gameObject);
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            constructExitButton.SetActive(false);
            constructEnterButton.SetActive(true);
        }

        private void UpdateBuildingCandidates()
        {
            if (!BuildingWindowResourceManager.Instance.IsResourceLoaded()) return;
            _buildingSprites.Clear();
            _saveIndices.Clear();
            _buildingNames.Clear();
            BuildingWindowResourceManager.Instance.GetFilteredBuildingSprites(_currentBuildingType, _buildingSprites,
                _saveIndices, _buildingNames, _currentSubType, _currentTier);
            var count = Mathf.Min(Slots.Count, _buildingSprites.Count);
            for (var i = 0; i < Slots.Count; i++)
            {
                if (i < count)
                {
                    Slots[i].SetActive(true);
                    var buildingSlot = SlotComponents[i];
                    buildingSlot.button.image.sprite = _buildingSprites[i];
                    buildingSlot.gameplayNameText.text = _buildingNames[i];
                }
                else
                {
                    Slots[i].SetActive(false);
                }
            }
        }
    }
}