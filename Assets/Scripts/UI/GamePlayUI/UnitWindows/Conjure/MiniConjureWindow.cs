using System;
using System.Collections.Generic;
using SparFlame.GamePlaySystem.General;
using SparFlame.GamePlaySystem.Spawn;
using SparFlame.GamePlaySystem.Units;
using SparFlame.GamePlaySystem.UnitSelection;
using SparFlame.UI.General;
using Unity.Entities;

namespace SparFlame.UI.GamePlay
{
    public class MiniConjureWindow : UIUtils.MultiSlotsWindow<MiniConjureSlot>, UIUtils.ISingleTargetWindow
    {
        // Public interface
        public static MiniConjureWindow Instance;

        [NonSerialized] public bool InitWindowEvents;

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
            if(!_em.HasComponent<ConjureAttr>(target))
                return false;
            _targetEntity = target;
            _currentGeneralType = _em.GetComponentData<ConjureAttr>(target).ConjuringType;
            UpdateCandidates();
            return true;
        }
        
        public bool TryGetConjureInfo(int index, out Entity conjureUnit, out Entity buildingEntity,
            out int maxConjureCount)
        {
            if (index >= _infos.Count)
            {
                conjureUnit = Entity.Null;
                buildingEntity = Entity.Null;
                maxConjureCount = 0;
                return false;
            }

            conjureUnit = _infos[index].EntityPrefab;
            buildingEntity = _targetEntity;
            maxConjureCount = SlotComponents[index].GetMaxConjureCount();
            return true;
        }

        public override void Hide()
        {
            base.Hide();
            _targetEntity = Entity.Null;
        }

        public override void OnClickSlot(int slotIndex)
        {
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

        private void Start()
        {
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
            _infos = UnitWindowResourceManager.Instance.GetFilteredInfoList(_currentGeneralType, currentSelectFaction);
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