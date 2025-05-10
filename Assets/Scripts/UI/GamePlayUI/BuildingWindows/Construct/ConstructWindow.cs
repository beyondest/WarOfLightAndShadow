using System;
using System.Collections.Generic;
using SparFlame.GamePlaySystem.Building;
using SparFlame.GamePlaySystem.CustomInput;
using SparFlame.GamePlaySystem.Exp;
using SparFlame.GamePlaySystem.General;
using SparFlame.GamePlaySystem.UnitSelection;
using SparFlame.UI.General;
using Unity.Entities;
using UnityEngine;
using UnityEngine.UI;

// ReSharper disable SimplifyConditionalTernaryExpression

// ReSharper disable PossibleNullReferenceException

namespace SparFlame.UI.GamePlay
{
    public class ConstructWindow : MultiSlotWindowUtils.MultiSlotsWindow<ConstructBuildingSlot>
    {
        [Header("Custom config")] [SerializeField]
        private Tier maxTier;

        [SerializeField] private GameObject constructWindowPanel;
        [SerializeField] private Image tierFilterIcon;

        [SerializeField] private GameObject constructEnterButton;

        [SerializeField] private GameObject constructExitButton;

        // Interface
        public static ConstructWindow Instance;
        public Action<Entity> EcsGhostShowTargetByTypeIndex;
        public Action EcsExitGhostShow;

        public void EnterConstruct()
        {
            Show();
        }
        public void ExitConstruct()
        {
            Hide();
            EcsExitGhostShow?.Invoke();
        }

        #region ButtonMethods

        public override void OnClickSlot(int slotIndex)
        {
            var entity = _infos[slotIndex].EntityPrefab;
            EcsGhostShowTargetByTypeIndex?.Invoke(entity);
            InfoWindowController.Instance.Show();
            BuildingDetailWindow.Instance.Show();
            InfoWindowController.Instance.UpdateCloseUpTarget(entity);
        }


  
        public void OnClickBuildingTypeButton(int type)
        {
            _currentGeneralType = (BuildingType)type;
            UpdateCandidates();
        }

        public void OnClickSubTypeButton(int subType)
        {
            _shouldFilterSubType = _currentSubType == subType ? !_shouldFilterSubType : true;
            _currentSubType = subType;
            UpdateCandidates();
        }

        public void OnClickTierButton()
        {
            if (_currentTier == maxTier && _shouldFilterTier)
            {
                tierFilterIcon.color = Color.gray;
                _shouldFilterTier = false;
            }
            else
            {
                tierFilterIcon.color = Color.white;
                _shouldFilterTier = true;
                _currentTier = _currentTier == maxTier ? Tier.Tier1 : (Tier)((int)_currentTier + 1);
                tierFilterIcon.sprite = BasicUIResourceManager.Instance.TierSprites[_currentTier];
            }

            UpdateCandidates();
        }

        #endregion

        public override void Show(Vector2? pos = null)
        {
            base.Show(pos);
            InputListener.Instance.ToggleConstructMap();
            UpdateCandidates();
            constructEnterButton.SetActive(false);
            constructExitButton.SetActive(true);
            constructWindowPanel.SetActive(true);
        }

        public override void Hide()
        {
            base.Hide();
            InputListener.Instance.ToggleConstructMap();
            constructExitButton.SetActive(false);
            constructEnterButton.SetActive(true);
            constructWindowPanel.SetActive(false);
            // These line may not need to add, because this will record player preference
            // _shouldFilterTier = false;
            // _shouldFilterSubType = false;
        }


        // Internal Data


        private int _currentSubType;
        private bool _shouldFilterSubType;
        private Tier _currentTier = Tier.Tier1;
        private bool _shouldFilterTier;
        private BuildingType _currentGeneralType = BuildingType.Fortifications;
        private FactionTag _currentFaction;


        private EntityManager _em;


        // Cache
        private List<SpriteEntityInfo> _infos = new();


        private void Awake()
        {
            if (Instance == null)
                Instance = this;
            else
                Destroy(gameObject);
        }

        public override void LoadResources()
        {
            base.LoadResources();
            constructExitButton.SetActive(false);
            constructEnterButton.SetActive(true);
        }

        protected override void Start()
        {
            base.Start();
            _shouldFilterTier = false;
            _shouldFilterSubType = false;
            _currentTier = maxTier;
            tierFilterIcon.color = Color.gray;
            panel.SetActive(false);
            constructWindowPanel.SetActive(false);
            _em = World.DefaultGameObjectInjectionWorld.EntityManager;
        }

        private void UpdateCandidates()
        {
            if (!BuildingWindowResourceManager.Instance.IsResourceLoaded()) return;
            _currentFaction = _em.CreateEntityQuery(typeof(UnitSelectionData)).GetSingleton<UnitSelectionData>()
                .CurrentSelectFaction;

            _infos.Clear();
            _infos = BuildingWindowResourceManager.Instance.GetFilteredInfoList(_currentGeneralType, _currentFaction,
                _currentSubType, _currentTier, true, _shouldFilterSubType, _shouldFilterTier);
            var count = _infos.Count;
            for (var i = 0; i < Slots.Count; i++)
            {
                if (i < count)
                {
                    Slots[i].SetActive(true);
                    var slot = SlotComponents[i];
                    slot.button.image.sprite = _infos[i].Sprite;
                    slot.SetTarget(_infos[i]);
                }
                else
                {
                    Slots[i].SetActive(false);
                }
            }
        }
    }
}