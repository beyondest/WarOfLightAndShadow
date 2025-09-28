using System;
using System.Collections.Generic;
using SparFlame.Components.General;
using SparFlame.Components.SubGameplay;
using SparFlame.Systems.General.BasicControl;
using SparFlame.UI.General;
using Unity.Entities;

namespace SparFlame.UI.SubGameplay
{
    public class SubGameplayGarrisonInfoWindow : MultiSlotWindowUtils.MultiSlotsWindow<SubGameplayGarrisonInfoSlot>, MultiSlotWindowUtils.ISingleTargetWindow
    {
        public static SubGameplayGarrisonInfoWindow Instance;
        public Action<int, Entity> EcsMoveOutGarrisonUnits;
        public Action<Entity> EcsMoveOutAllGarrisonUnits;

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
        public void ClearCloseUpTarget()
        {
            _targetEntity = Entity.Null;
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
        private EntityQuery _gamingTag;

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
            for (var i = 0; i < config.rows * config.cols; i++)
            {
                _garrisonUnitsIds.Add(0);
            }
        }

        public override void UnloadResources()
        {
            base.UnloadResources();
            _garrisonUnitsIds.Clear();
        }

        protected override void Start()
        {
            base.Start();
            _em = World.DefaultGameObjectInjectionWorld.EntityManager;
            if(_gamingTag != default)_gamingTag.Dispose();
            _gamingTag = _em.CreateEntityQuery(typeof(SubGamingTag));
            Hide();
        }

        private void Update()
        {
            if (_gamingTag.IsEmpty) return;
            if (!IsOpened()) return;
            if (_targetEntity == Entity.Null) return;
            if (!_em.HasComponent<SubGameplayGeneralAttr>(_targetEntity))
            {
                _targetEntity = Entity.Null;
                return;
            }

            UpdateDynamicData();
        }

        private void OnDestroy()
        {
            if(_gamingTag != default)_gamingTag.Dispose();
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
                    var info = UnitWindowResourceManager.Instance.GetInfoByGeneralTypeAndIdx(data.unitType,
                        data.id);
                    _garrisonUnitsIds[i] = data.id;
                    slotComponent.garrisonUnitName.text = info.GameplayName;
                    slotComponent.garrisonUnitCount.text = data.count.ToString();
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