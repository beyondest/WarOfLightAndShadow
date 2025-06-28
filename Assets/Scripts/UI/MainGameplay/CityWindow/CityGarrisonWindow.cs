using SparFlame.Components.General;
using SparFlame.Components.Input;
using SparFlame.Components.MainGameplay;
using SparFlame.Systems.General.BasicControl;
using SparFlame.UI.General;
using Unity.Entities;
using UnityEngine;

namespace SparFlame.UI.MainGameplay
{
    public class CityGarrisonWindow : MultiSlotWindowUtils.MultiSlotsWindow<CityGarrisonSlot>,
        MultiSlotWindowUtils.ISingleTargetWindow
    {
        public static CityGarrisonWindow Instance;

        public bool TrySwitchTarget(Entity target)
        {
            if (!_em.HasBuffer<CityGarrisonEntity>(target)) return false;

            var buffer = _em.GetBuffer<CityGarrisonTypeData>(target);
            if (buffer.Length == 0) return false;
            _target = target;
            UpdateSlots();
            return true;
        }

        public bool HasTarget()
        {
            return _target != Entity.Null;
        }

        public void ClearCloseUpTarget()
        {
            _target = Entity.Null;
        }

        public override void OnClickSlot(int slotIndex)
        {
            var buffer = _em.GetBuffer<CityGarrisonTypeData>(_target);
            if (buffer.Length == 0 || slotIndex >= buffer.Length) return;
            var ifGarrisonOutAllSameIcon =
                _inputQuery.GetSingleton<InputArmyGroupControlData>().MoveOutAllSameIconArmyGroups;
            var iconType = buffer[slotIndex].iconType;
            var request = _em.CreateEntity();
            _em.AddComponent<ArmyGroupGarrisonRequest>(request);
            _em.SetComponentData(request, new ArmyGroupGarrisonRequest
            {
                City = _target,
                ArmyGroup = Entity.Null,
                IfGarrisonOutAllSameIcon = ifGarrisonOutAllSameIcon,
                IconType = iconType,
                IfGarrisonIn = false
            });
        }

        private EntityManager _em;
        private Entity _target;
        private EntityQuery _inputQuery;
        private EntityQuery _mainGamingTag;

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
            _inputQuery = _em.CreateEntityQuery(typeof(InputArmyGroupControlData));
            _mainGamingTag = _em.CreateEntityQuery(typeof(MainGamingTag));
            Hide();
        }

        private void Update()
        {
            if (!IsOpened() || _mainGamingTag.IsEmpty) return;
            if (!HasTarget()) return;
            if (!_em.HasBuffer<CityGarrisonEntity>(_target))
            {
                _target = Entity.Null;
                Hide();
                return;
            }
            UpdateSlots();
        }

        #endregion

        private void UpdateSlots()
        {
            var buffer = _em.GetBuffer<CityGarrisonTypeData>(_target);
            for (var i = 0; i < Slots.Count; i++)
            {
                if (i < buffer.Length)
                {
                    var typeData = buffer[i];
                    Slots[i].SetActive(true);
                    SlotComponents[i].button!.image.sprite =
                        ArmyGroupWindowResourceManager.Instance.ArmyGroupIcons[typeData.iconType];
                    SlotComponents[i].armyGroupCount.text = typeData.count.ToString();
                }
                else
                {
                    Slots[i].SetActive(false);
                }
            }
        }
    }
}