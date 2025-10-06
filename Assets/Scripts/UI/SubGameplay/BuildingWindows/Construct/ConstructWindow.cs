using System;
using System.Collections.Generic;
using SparFlame.Components.General;
using SparFlame.Components.SubGameplay;
using SparFlame.Systems.General.BasicControl;
using SparFlame.Systems.General.Input;
using SparFlame.UI.General;
using Unity.Entities;
using UnityEngine;
using UnityEngine.UI;

// ReSharper disable SimplifyConditionalTernaryExpression

// ReSharper disable PossibleNullReferenceException

namespace SparFlame.UI.SubGameplay
{
    public class ConstructWindow : MultiSlotWindowUtils.MultiSlotsWindow<ConstructBuildingSlot>
    {
        [Header("Custom config")] [SerializeField]
        private Tier maxTier;

        [SerializeField] private bool onlyShowTier3ConjuringShrines = true;
        [SerializeField] private GameObject constructWindowPanel;
        [SerializeField] private Image tierFilterIcon;

        [SerializeField] private GameObject constructEnterButton;
        [SerializeField] private GameObject constructExitButton;
        [SerializeField] private Scrollbar scrollbar;
        
        // Interface
        public static ConstructWindow Instance;
        public Action EcsEnterConstruct;
        public Action EcsExitConstruct;
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
            BuildingDetailWindow.Instance.HideConstructPanel();
        }


  
        public void OnClickBuildingTypeButton(int type)
        {
            _currentGeneralType = (BuildingType)type;
            scrollbar.value = 1;
            UpdateCandidates();
        }

        public void OnClickSubTypeButton(int subType)
        {
            _shouldFilterSubType = _currentSubType == subType ? !_shouldFilterSubType : true;
            _currentSubType = subType;
            scrollbar.value = 1;
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

            scrollbar.value = 1;
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
            EcsEnterConstruct?.Invoke();
        }

        public override void Hide()
        {
            base.Hide();
            InputListener.Instance.ToggleConstructMap();
            constructExitButton.SetActive(false);
            constructEnterButton.SetActive(true);
            constructWindowPanel.SetActive(false);
            EcsExitConstruct?.Invoke();

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
        private EntityQuery _factionQuery;



        // Cache
        private List<SpriteEntityInfo> _infos = new();


        private void Awake()
        {
            if (!Instance)
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
            var em = World.DefaultGameObjectInjectionWorld.EntityManager;
            _factionQuery = em.CreateEntityQuery(typeof(PlayerFactionData));
        }

        private void OnDestroy()
        {
            if(_factionQuery != default)_factionQuery.Dispose();
        }

        private void UpdateCandidates()
        {
            var currentFaction = _factionQuery.GetSingleton<PlayerFactionData>().faction;
            _infos.Clear();
            var shouldFilterTier = _shouldFilterTier;
            var filterTier = _currentTier;
            if (onlyShowTier3ConjuringShrines && _currentGeneralType == BuildingType.ConjuringShrines)
            {
                shouldFilterTier = true;
                filterTier = Tier.Tier3;
            }
            _infos = BuildingWindowResourceManager.Instance.GetFilteredInfoList(_currentGeneralType, currentFaction,
                _currentSubType, filterTier, true, _shouldFilterSubType, shouldFilterTier, shouldTierExactlyMatch: true);
            if (_currentGeneralType == BuildingType.Ornaments)
            {
                for (int i = _infos.Count - 1; i >= 0; i--)
                {
                    if (currentFaction == FactionTag.Light && _infos[i].SubtypeIndex == (int)OrnamentType.Crystal)
                        _infos.RemoveAt(i);
                    
                }
            }
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