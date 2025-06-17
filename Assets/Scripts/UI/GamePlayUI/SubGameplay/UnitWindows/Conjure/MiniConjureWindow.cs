using System;
using System.Collections.Generic;
using SparFlame.GamePlaySystem.Conjure;
using SparFlame.GamePlaySystem.General;
using SparFlame.GamePlaySystem.Hints;
using SparFlame.GamePlaySystem.Interact;
using SparFlame.GamePlaySystem.Units;
using SparFlame.GamePlaySystem.UnitSelection;
using SparFlame.UI.General;
using Unity.Entities;
using UnityEngine;

namespace SparFlame.UI.SubGameplay
{
    public class MiniConjureWindow : MultiSlotWindowUtils.MultiSlotsWindow<MiniConjureSlot>,
        MultiSlotWindowUtils.ISingleTargetWindow
    {
        // Public interface
        public static MiniConjureWindow Instance;


        // Unit, Building, ConjureCount
        public Action<Entity, Entity, int> EcsConjureUnit;

        // Internal Data
        private List<SpriteEntityInfo> _infos = new();
        private UnitType _currentGeneralType;

        private EntityManager _em;
        private Entity _targetEntity = Entity.Null;

        public bool TrySwitchTarget(Entity target)
        {
            _em = World.DefaultGameObjectInjectionWorld.EntityManager;
            if (!_em.HasComponent<ConjureAttr>(target))
                return false;
            _targetEntity = target;
            _currentGeneralType = _em.GetComponentData<ConjureAttr>(target).ConjuringType;
            UpdateCandidates();
            return true;
        }

        public bool TryGetConjureInfo(int index, out int conjureIndex
          )
        {
            if (index > _infos.Count || _targetEntity == Entity.Null)
            {
                conjureIndex = 0;
                return false;
            }

            conjureIndex = index - 1;
            // conjureUnit = _infos[index - 1].EntityPrefab;
            // buildingEntity = _targetEntity;
            // maxConjureCount = SlotComponents[index].GetMaxConjureCount();
            return true;
        }

        public override void Hide()
        {
            base.Hide();
            _targetEntity = Entity.Null;
        }

        public void ClearCloseUpTarget()
        {
            _targetEntity = Entity.Null;
        }

        public override void OnClickSlot(int slotIndex)
        {
            if (_targetEntity == Entity.Null) return;
            var maxCount = SlotComponents[slotIndex].GetMaxConjureCount();
            if (maxCount == 0)
            {
                var hintRequest = _em.CreateEntity();
                _em.AddComponent<HintRequest>(hintRequest);
                _em.SetComponentData(hintRequest, new HintRequest
                {
                    Name = HintName.NotEnoughResource,
                });
                return;
            }
            EcsConjureUnit?.Invoke(_infos[slotIndex].EntityPrefab, _targetEntity,
                SlotComponents[slotIndex].GetMaxConjureCount());
        }

        private void Awake()
        {
            if (Instance == null)
                Instance = this;
            else
                Destroy(gameObject);
        }

        protected override void Start()
        {
            base.Start();
            _em = World.DefaultGameObjectInjectionWorld.EntityManager;
            Hide();
        }

        private void UpdateCandidates()
        {
            if (!UnitWindowResourceManager.Instance.IsResourceLoaded()) return;
            var currentSelectFaction = _em.CreateEntityQuery(typeof(UnitSelectionData))
                .GetSingleton<UnitSelectionData>()
                .CurrentSelectFaction;
            _infos.Clear();
            _infos = UnitWindowResourceManager.Instance.GetFilteredInfoList(_currentGeneralType, currentSelectFaction,
                tier: _em.GetComponentData<ExpData>(_targetEntity).curTier, filterTier:true);
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


        public bool HasTarget()
        {
            return _targetEntity != Entity.Null;
        }
    }
}