using System;
using System.Collections.Generic;
using SparFlame.GamePlaySystem.Garrison;
using SparFlame.GamePlaySystem.General;
using SparFlame.UI.General;
using Unity.Entities;

namespace SparFlame.UI.GamePlay
{
    public class GarrisonInfoWindow : UIUtils.MultiSlotsWindow<GarrisonInfoSlot>, UIUtils.ISingleTargetWindow
    {
        public static GarrisonInfoWindow Instance;
        public Action<int, Entity> EcsMoveOutGarrisonUnits;
        public Action<Entity> EcsMoveOutAllGarrisonUnits;
        [NonSerialized] public bool InitGarrisonEvents;

        public bool TrySwitchTarget(Entity target)
        {
            _em = World.DefaultGameObjectInjectionWorld.EntityManager;
            if (!_em.HasComponent<GarrisonAttr>(target))
                return false;
            _targetEntity = target;
            UpdateDynamicData();
            return true;
        }

        public bool HasTarget()
        {
            return _targetEntity != Entity.Null;
        }

        
        public override void OnClickSlot(int slotIndex)
        {
            EcsMoveOutGarrisonUnits?.Invoke(_garrisonUnitsIds[slotIndex], _targetEntity);
        }

        public void OnClickMoveOutAllGarrisonUnits()
        {
            EcsMoveOutAllGarrisonUnits?.Invoke(_targetEntity);
        }


        private readonly List<int> _garrisonUnitsIds = new();

        private Entity _targetEntity = Entity.Null;
        private EntityManager _em;
        private EntityQuery _notPauseTag;

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
            for (var i = 0; i < config.rows * config.cols; i++)
            {
                _garrisonUnitsIds.Add(0);
            }
        }

        private void Start()
        {
            _em = World.DefaultGameObjectInjectionWorld.EntityManager;
            _notPauseTag = _em.CreateEntityQuery(typeof(NotPauseTag));
            Hide();
        }

        private void Update()
        {
            if (!IsOpened()) return;
            if (_notPauseTag.IsEmpty) return;
            if (_targetEntity == Entity.Null) return;
            if (!_em.HasComponent<GeneralAttr>(_targetEntity))
            {
                _targetEntity = Entity.Null;
                return;
            }

            UpdateDynamicData();
        }

        private void UpdateDynamicData()
        {
            var garrisonData = _em.GetBuffer<GarrisonTypeData>(_targetEntity);
            for (var i = 0; i < Slots.Count; i++)
            {
                if (i < garrisonData.Length)
                {
                    Slots[i].SetActive(true);
                    var slotComponent = SlotComponents[i];
                    var data = garrisonData[i];
                    var info = UnitWindowResourceManager.Instance.GetInfo(data.UnitType,
                        data.ID);
                    _garrisonUnitsIds[i] = data.ID;
                    slotComponent.garrisonUnitName.text = info.GameplayName;
                    slotComponent.garrisonUnitCount.text = data.Count.ToString();
                    slotComponent.button!.image.sprite = info.Sprite;
                }
                else
                {
                    Slots[i].SetActive(false);
                }
            }
        }
    }
}