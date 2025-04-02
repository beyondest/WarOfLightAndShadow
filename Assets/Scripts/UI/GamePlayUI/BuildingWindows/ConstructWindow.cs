using System;
using System.Collections.Generic;
using SparFlame.GamePlaySystem.Building;
using SparFlame.GamePlaySystem.General;
using SparFlame.UI.General;
using UnityEngine;
using UnityEngine.ResourceManagement.AsyncOperations;

// ReSharper disable PossibleNullReferenceException

namespace SparFlame.UI.GamePlay
{
    public class ConstructWindow : UIUtils.MultiSlotsWindow<MultiShowSlot, ConstructWindow>
    {
        // public static ConstructWindow Instance;
        [NonSerialized] public bool InitWindowEvents = false;
        [Header("Custom config")]
        public KeyCode exitGhostModeKey = KeyCode.Escape;

        public Action<BuildingType, int> EcsGhostShowTarget;
        public Action EcsExitGhostShow;
        public Action EcsBuildTarget;


        #region ButtonMethods

        public override void OnClickSlot(int slotIndex)
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
            _currentTier = _currentTier == tier ? Tier.TierNone : tier;
            UpdateBuildingCandidates();
        }
        #endregion
        
        public override void Show(Vector2? pos = null)
        {
            base.Show(pos);
            UpdateBuildingCandidates();
        }

   

   

        // Internal Data


        private int _currentSubType = -1;
        private Tier _currentTier = Tier.TierNone;
        private BuildingType _currentBuildingType;
        private AsyncOperationHandle<GameObject> _slotPrefabHandle;


        // Cache
        private GameObject _buildingSlotPrefab;
        private readonly List<Sprite> _buildingSprites = new();
        private readonly List<int> _saveIndices = new();



    
     

        private void UpdateBuildingCandidates()
        {
            if (!BuildingWindowResourceManager.Instance.IsResourceLoaded() || !_slotPrefabHandle.IsDone) return;
            _buildingSprites.Clear();
            _saveIndices.Clear();
            BuildingWindowResourceManager.Instance.GetFilteredSprites(_currentBuildingType, _buildingSprites,
                _saveIndices, _currentSubType, _currentTier);

            var count = Mathf.Min(Slots.Count, _buildingSprites.Count);
            for (var i = 0; i < Slots.Count; i++)
            {
                if (i < count)
                {
                    Slots[i].SetActive(true);
                    var buildingSlot = SlotComponents[i];
                    buildingSlot.button.image.sprite = _buildingSprites[i];
                }
                else
                {
                    Slots[i].SetActive(false);
                }
            }
        }

     
    }


}