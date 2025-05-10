using System.Globalization;
using SparFlame.Database;
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
    public class UnitDetailWindow : MultiSlotWindowUtils.MultiSlotsWindow<AttributeSlot>, MultiSlotWindowUtils.ISingleTargetWindow
    {
        // Config
        [Header("General")] 
        [SerializeField] private TMP_Text generalTypeText;
        [SerializeField] private Image generalTypeIcon;
        [SerializeField] private TMP_Text description;
        [SerializeField] private Image idSingleIcon;
        [SerializeField] private Image interactAbilityTriangle;


        [Header("Unit Detail")]
        [SerializeField] private TMP_Text unitMoveSpeed;
        
        
        // Interface
        public static UnitDetailWindow Instance;

        public override void Hide()
        {
            base.Hide();
            TargetEntity = Entity.Null;
        }

        public bool TrySwitchTarget(Entity target)
        {
            Em = World.DefaultGameObjectInjectionWorld.EntityManager;
            if (!Em.HasComponent<UnitAttr>(target)
                || !Em.HasComponent<MovableData>(target)
                || !Em.HasBuffer<CostList>(target)
                || !Em.HasComponent<StatData>(target))
                return false;
            TargetEntity = target;
            UpdateStaticInfo();
            UpdateDynamicInfo();
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
        private EntityQuery _gamingTag;

        #region EventFunction

        protected virtual void Awake()
        {
            if (Instance == null)
                Instance = this;
            else
                Destroy(gameObject);
        }


        protected override void Start()
        {
            base.Start();
            Em = World.DefaultGameObjectInjectionWorld.EntityManager;
            _gamingTag = Em.CreateEntityQuery(typeof(GamingTag));
            panel.SetActive(false);
        }


        protected virtual void Update()
        {
            if (_gamingTag.IsEmpty) return;
            if (!IsOpened()) return;
            if (TargetEntity == Entity.Null) return;
            if (!Em.HasComponent<GeneralAttr>(TargetEntity))
            {
                TargetEntity = Entity.Null;
                return;
            }
            UpdateDynamicInfo();
        }

        #endregion


        private void UpdateStaticInfo()
        {
            var generalAttr = Em.GetComponentData<GeneralAttr>(TargetEntity);
            var unitAttr = Em.GetComponentData<UnitAttr>(TargetEntity);
            description.text = DatabaseManager.UnitDatabaseSo.GetItemById(generalAttr.ID).description;
            generalTypeIcon.sprite = UnitWindowResourceManager.Instance.UnitGeneralTypeSprites[unitAttr.Type];
            generalTypeText.text = unitAttr.Type.ToString();
            idSingleIcon.sprite = UnitWindowResourceManager.Instance.GetInfoByGeneralTypeAndIdx(unitAttr.Type, generalAttr.ID).Sprite;
            UpdateCostSlots();
        }
        public virtual void UpdateCostSlots()
        {
            var costList = Em.GetBuffer<CostList>(TargetEntity);
            for (var i = 0; i < Slots.Count; i++)
            {
                if (i < costList.Length)
                {
                    Slots[i].SetActive(true);
                    var cost = costList[i];
                    var costSlot = SlotComponents[i];
                    costSlot.icon.sprite = BasicUIResourceManager.Instance.ResourceSprites[cost.Type];
                    costSlot.label.text = cost.Type.ToString();
                    costSlot.value.text = $"x{cost.Amount}";
                }
                else
                {
                    Slots[i].SetActive(false);
                }
            }
        }
        
        private void UpdateDynamicInfo()
        {
            var movableData = Em.GetComponentData<MovableData>(TargetEntity);
            // Visualize these attributes
            unitMoveSpeed.text = movableData.MoveSpeed.ToString(CultureInfo.InvariantCulture);
        }
    }
}