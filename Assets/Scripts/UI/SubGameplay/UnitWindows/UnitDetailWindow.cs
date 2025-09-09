using SparFlame.Components.General;
using SparFlame.Components.SubGameplay;
using SparFlame.Database;
using SparFlame.Systems.General.BasicControl;
using SparFlame.UI.General;
using TMPro;
using Unity.Entities;
using UnityEngine;
using UnityEngine.UI;

namespace SparFlame.UI.SubGameplay
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
        [SerializeField] private Image generalFactionImage;
        [SerializeField] private Image subFactionImage;
        [SerializeField] private GameObject returnToMulti2DWindowButton;
        
        [Header("Unit Detail")]
        [SerializeField] private TMP_Text unitMoveSpeed;

        [SerializeField] private TMP_Text moveSpeedBonusText;
        // Interface
        public static UnitDetailWindow Instance;

        public override void Show(Vector2? pos = null)
        {
            base.Show(pos);
            if(pos != null)
                _panelRectTransform.anchoredPosition = pos.Value;
        }

        public override void Hide()
        {
            base.Hide();
            TargetEntity = Entity.Null;
            _panelRectTransform.anchoredPosition = _originalPanelPos;
            returnToMulti2DWindowButton.SetActive(false);
        }
        public void ClearCloseUpTarget()
        {
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

        public void ShowReturnButton()
        {
            returnToMulti2DWindowButton.SetActive(true);
        }

        public bool HasTarget()
        {
            return TargetEntity != Entity.Null;
        }

        #region ButtonMethods

        public void OnClickReturnToMulti2DWindow()
        {
            Hide();
            InteractAbilityWindow.Instance.Hide();
            returnToMulti2DWindowButton.SetActive(false);
        }

        #endregion
       
        // Internal Data
        protected Entity TargetEntity = Entity.Null;
        private Vector2 _originalPanelPos;
        private RectTransform _panelRectTransform;

        // ECS
        protected EntityManager Em;
        private EntityQuery _gamingTag;

        #region EventFunction

        protected virtual void Awake()
        {
            if (!Instance)
                Instance = this;
            else
                Destroy(gameObject);
        }


        protected override void Start()
        {
            base.Start();
            Em = World.DefaultGameObjectInjectionWorld.EntityManager;
            _gamingTag = Em.CreateEntityQuery(typeof(SubGamingTag));
            panel.SetActive(false);
            _originalPanelPos = panel.GetComponent<RectTransform>().anchoredPosition;
            _panelRectTransform = panel.GetComponent<RectTransform>();
            returnToMulti2DWindowButton.SetActive(false);
        }


        protected virtual void Update()
        {
            if (_gamingTag.IsEmpty) return;
            if (!IsOpened()) return;
            if (TargetEntity == Entity.Null) return;
            if (!Em.HasComponent<SubGameplayGeneralAttr>(TargetEntity))
            {
                TargetEntity = Entity.Null;
                return;
            }
            UpdateDynamicInfo();
        }

        #endregion
        
        private void UpdateStaticInfo()
        {
            var generalAttr = Em.GetComponentData<SubGameplayGeneralAttr>(TargetEntity);
            var unitAttr = Em.GetComponentData<UnitAttr>(TargetEntity);
            if (generalAttr.Faction == FactionTag.Neutral)
            {
                generalFactionImage.enabled = false;
                subFactionImage.enabled = false;
            }
            else
            {
                generalFactionImage.enabled = true;
                subFactionImage.enabled = generalAttr.SubFaction != SubFactionTag.None; 
                
                generalFactionImage.sprite = BasicUIResourceManager.Instance.GeneralFactionIconSprites[generalAttr.Faction];
                subFactionImage.sprite = BasicUIResourceManager.Instance.SubFactionIconSprites[generalAttr.SubFaction];
                var color = generalFactionImage.color;
                color.a = generalAttr.Faction == FactionTag.Light ? GlobalUIConfigger.Instance.lightGeneralFactionAlpha : GlobalUIConfigger.Instance.darkGeneralFactionAlpha;
                generalFactionImage.color = color;
                subFactionImage.color = color;
            }
            description.text = DatabaseManager.UnitDatabaseSo.GetItemById(generalAttr.PrefabID).description;
            generalTypeIcon.sprite = UnitWindowResourceManager.Instance.UnitGeneralTypeSprites[unitAttr.Type];
            generalTypeText.text = unitAttr.Type.ToString();
            idSingleIcon.sprite = UnitWindowResourceManager.Instance.GetInfoByGeneralTypeAndIdx(unitAttr.Type, generalAttr.PrefabID).Sprite;
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
            var bonus = Em.GetComponentData<InteractAbilityBonus>(TargetEntity);
            // Visualize these attributes
            if (bonus.MoveSpeedBonus == 0)
            {
                moveSpeedBonusText.enabled = false;
            }
            else
            {
                moveSpeedBonusText.enabled = true;
                var signal = bonus.MoveSpeedBonus >= 0 ? "+" : "-";
                moveSpeedBonusText.text = $"({signal}{bonus.MoveSpeedBonus})";
                moveSpeedBonusText.color = bonus.MoveSpeedBonus > 0 ? Color.green : Color.red;
            }
            unitMoveSpeed.text = (movableData.MoveSpeed + bonus.MoveSpeedBonus).ToString("F1") ;
        }
    }
}