using System.Globalization;
using SparFlame.GamePlaySystem.General;
using SparFlame.GamePlaySystem.Interact;
using SparFlame.GamePlaySystem.Movement;
using SparFlame.GamePlaySystem.Resource;
using SparFlame.GamePlaySystem.Units;
using SparFlame.UI.General;
using TMPro;
using Unity.Entities;
using UnityEngine;
using UnityEngine.UI;

namespace SparFlame.UI.GamePlay
{
    public class UnitDetailWindow : UIUtils.MultiSlotsWindow<AttributeSlot>, UIUtils.ISingleTargetWindow
    {
        // Config
        [Header("Custom Config")] [SerializeField]
        private TMP_Text unitType;

        [SerializeField] private TMP_Text unitMoveSpeed;
        [SerializeField] private Image unitIcon;
        
        // Interface
        public static UnitDetailWindow Instance;

        public override void Hide()
        {
            base.Hide();
            TargetEntity = Entity.Null;
        }

        public bool TrySwitchTarget(Entity target)
        {
            if (!Em.HasComponent<UnitAttr>(target)
                || !Em.HasComponent<MovableData>(target)
                || !Em.HasBuffer<CostList>(target)
                || !Em.HasComponent<StatData>(target))
                return false;
            TargetEntity = target;
            UpdateUnitDetailInfo();
            return true;
        }

        public bool HasTarget()
        {
            return TargetEntity != Entity.Null;
        }

        // Internal Data
        protected Entity TargetEntity = Entity.Null;

        // ECS
        protected EntityManager Em;
        protected EntityQuery NotPauseTag;

        #region EventFunction

        protected virtual void Awake()
        {
            if (Instance == null)
                Instance = this;
            else
                Destroy(gameObject);
        }


        protected virtual void Start()
        {
            Em = World.DefaultGameObjectInjectionWorld.EntityManager;
            NotPauseTag = Em.CreateEntityQuery(typeof(NotPauseTag));
            panel.SetActive(false);
        }

        protected virtual void Update()
        {
            if (NotPauseTag.IsEmpty) return;
            if (!IsOpened()) return;

            if (!UnitWindowResourceManager.Instance.IsResourceLoaded()
                || !BasicWindowResourceManager.Instance.IsResourceLoaded()
                || !IsResourceLoaded()) return;
            if (TargetEntity == Entity.Null) return;
            if (!Em.HasComponent<InteractableAttr>(TargetEntity))
            {
                TargetEntity = Entity.Null;
                return;
            }
            UpdateUnitDetailInfo();
        }

        #endregion


        private void UpdateUnitDetailInfo()
        {
            // var interactableAttr = _em.GetComponentData<InteractableAttr>(_targetEntity);
            var attr = Em.GetComponentData<UnitAttr>(TargetEntity);
            var movableData = Em.GetComponentData<MovableData>(TargetEntity);
            var costList = Em.GetBuffer<CostList>(TargetEntity);
            // Visualize these attributes
            unitType.text = attr.Type.ToString();
            unitMoveSpeed.text = movableData.MoveSpeed.ToString(CultureInfo.InvariantCulture);
            unitIcon.sprite = UnitWindowResourceManager.Instance.UnitSprites[attr.Type];
            for (var i = 0; i < Slots.Count; i++)
            {
                if (i < costList.Length)
                {
                    Slots[i].SetActive(true);
                    var cost = costList[i];
                    var costSlot = SlotComponents[i];
                    costSlot.icon.sprite = BasicWindowResourceManager.Instance.ResourceSprites[cost.Type];
                    costSlot.label.text = cost.Type.ToString();
                    costSlot.value.text = $"x{cost.Amount}";
                }
                else
                {
                    Slots[i].SetActive(false);
                }
            }
        }
    }
}