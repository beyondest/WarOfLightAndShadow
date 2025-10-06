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

namespace SparFlame.UI.SubGameplay
{
    public class ConjureWindow : MultiSlotWindowUtils.MultiSlotsWindow<ConjureSlot>,MultiSlotWindowUtils.ISingleTargetWindow
    {
        [SerializeField] private GameObject conjureWindowPanel;
        [SerializeField] private Tier maxTier = Tier.Tier3;
        [SerializeField] private Image tierFilterIcon;
        // Interface
        public static ConjureWindow Instance;
        // Unit, count, building, maxCount
        public Action<Entity, int,Entity,int> EcsConjureUnits;

        public override void Show(Vector2? pos = null)
        {
            base.Show(pos);
            conjureWindowPanel.SetActive(true);
        }
        public void ClearCloseUpTarget()
        {
            _targetEntity = Entity.Null;
        }

        public override void Hide()
        {
            base.Hide();
            conjureWindowPanel.SetActive(false);
            _targetEntity = Entity.Null;
            _shouldFilterTier = false;
            _shouldFilterSubType = false;
        }

        public bool TrySwitchTarget(Entity target)
        {
            if(!_em.HasComponent<ConjureAttr>(target))
                return false;
            _targetEntity = target;
            _currentGeneralType = _em.GetComponentData<ConjureAttr>(target).ConjuringType;
            UpdateCandidates();
            return true;
        }

        public bool HasTarget()
        {
            return _targetEntity != Entity.Null;
        }
        
        #region ButtonMethods

        // This method call when conjuring button in conjuring window is click. Will pass conjuring count and prefab to ecs
        public override void OnClickSlot(int slotIndex)
        {
            if(_targetEntity == Entity.Null)return;
            
            var count = SlotComponents[slotIndex].GetConjureCount();
            var entity = _infos[slotIndex].EntityPrefab;
            var maxConjureCount = SlotComponents[slotIndex].GetMaxConjureCount();
            if (maxConjureCount == 0)
            {
                var hintRequest = _em.CreateEntity();
                _em.AddComponent<HintRequest>(hintRequest);
                _em.SetComponentData(hintRequest, new HintRequest
                {
                    Name = HintName.NotEnoughResource,
                });
                return;
            }
            EcsConjureUnits?.Invoke(entity, count,_targetEntity,maxConjureCount);
        }

        // Filter methods
        public void OnClickSubTypeButton(int subType)
        {
            _shouldFilterSubType = _currentSubType == subType ? !_shouldFilterSubType : true;
            _currentSubType = subType;
            UpdateCandidates();
        }

        // Filter methods
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

        public void OnClickCloseButton()
        {
            Hide();
            _targetEntity = Entity.Null;
        }
        
        #endregion

        // Internal Data
        private EntityManager _em;
        private Entity _targetEntity = Entity.Null;
        private UnitType _currentGeneralType;
        private int _currentSubType;
        private Tier _currentTier;
        private bool _shouldFilterSubType;
        private bool _shouldFilterTier;
        private FactionTag _currentFaction;
        
        // Cache
        private List<SpriteEntityInfo> _infos = new();

        #region EventFunctions

        private void Awake()
        {
            if (!Instance)
                Instance = this;
            else
                Destroy(gameObject);
        }
        
        protected override void Start()
        {
            base.Start();
            _em = World.DefaultGameObjectInjectionWorld.EntityManager;
            Hide();
            _shouldFilterTier = false;
            _shouldFilterSubType = false;
            _currentTier = maxTier;
            tierFilterIcon.color = Color.gray;
            var customInputActions = InputListener.Instance.GetCustomInputActions();
            customInputActions.GeneralShortcut.CloseWindow.performed += _ => Hide();
        }

        private void OnDestroy()
        {
            
        }

        #endregion

        
        private void UpdateCandidates()
        {
            if (!UnitWindowResourceManager.Instance.IsResourceLoaded()) return;
            _infos.Clear();
            using var query = _em.CreateEntityQuery(typeof(PlayerFactionData));
            _currentFaction = query.GetSingleton<PlayerFactionData>()
                .faction;
            var tier = _em.GetComponentData<ExpData>(_targetEntity).curTier;
            _infos = UnitWindowResourceManager.Instance.GetFilteredInfoList(_currentGeneralType,_currentFaction,
                _currentSubType,tier , true,_shouldFilterSubType,true);
            var count = _infos.Count;
            for (var i = 0; i < Slots.Count; i++)
            {
                if (i < count)
                {
                    Slots[i].SetActive(true);
                    var slot = SlotComponents[i];
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