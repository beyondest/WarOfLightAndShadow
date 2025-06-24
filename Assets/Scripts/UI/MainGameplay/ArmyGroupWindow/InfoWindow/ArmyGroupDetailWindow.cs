using SparFlame.Components.General;
using SparFlame.Components.MainGameplay;
using SparFlame.Systems.General.BasicControl;
using SparFlame.UI.General;
using TMPro;
using Unity.Entities;
using UnityEngine;
using UnityEngine.UI;

namespace SparFlame.UI.MainGameplay
{
    public class ArmyGroupDetailWindow : MultiSlotWindowUtils.MultiSlotsWindow<ArmyGroupUnitCompositionSlot>,
        MultiSlotWindowUtils.ISingleTargetWindow
    {
        
        // Config
        [Header("General")] 
        [SerializeField] private Image generalFactionIcon;
        [SerializeField] private Image subFactionIcon;
        [SerializeField] private GameObject formationButton;
        [Header("Config")]
        [SerializeField] private bool isSingleton;
        [SerializeField] private GameObject totalPanel;
        
        [Header("ArmyGroupAttr")]
        [SerializeField] private Image armyGroupIcon;
        [SerializeField] private TMP_Text armyGroupNameText;
        [Header("Attr")] [SerializeField] private TMP_Text speedText;
        // [SerializeField] private TMP_Text moraleText;
        
        [Header("Composition")]
        [SerializeField] private TMP_Text totalUnitCountText;
        [SerializeField] private GameObject compositionPanel;
        
        // Interface
        public static ArmyGroupDetailWindow Instance;


        public override void Show(Vector2? pos = null)
        {
            totalPanel.SetActive(true);
            panel.SetActive(true);
        }

        public override void Hide()
        {
            panel.SetActive(false);
            totalPanel.SetActive(false);
            _targetEntity = Entity.Null;
            foreach (var slot in Slots)
            {
                slot.SetActive(false);
            }
        }

        public override bool IsOpened()
        {
            return totalPanel.activeSelf;
        }

        public bool TrySwitchTarget(Entity target)
        {
            Em = World.DefaultGameObjectInjectionWorld.EntityManager;
            if (!Em.HasComponent<ArmyGroupAttr>(target))
            {
                return false;
            }
            _targetEntity = target;
            UpdateStaticData();
            if(isSingleton)
                ShowComposition();
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

        private Entity _targetEntity;
        protected EntityManager Em;


        #region EventFunctions

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
            Hide();
        }

        #endregion


        private void UpdateStaticData()
        {
            var playerFaction = Em.CreateEntityQuery(typeof(PlayerFactionData)).GetSingleton<PlayerFactionData>().Value;
            var generalAttr = Em.GetComponentData<MainGameplayGeneralAttr>(_targetEntity);
            generalFactionIcon.sprite = BasicUIResourceManager.Instance.GeneralFactionIconSprites[generalAttr.Faction];
            subFactionIcon.sprite = BasicUIResourceManager.Instance.SubFactionIconSprites[generalAttr.SubFaction];
            
            var armyGroupAttr = Em.GetComponentData<ArmyGroupAttr>(_targetEntity);
            armyGroupIcon.sprite = ArmyGroupWindowResourceManager.Instance.ArmyGroupIcons[armyGroupAttr.IconType];
            armyGroupIcon.color = generalAttr.Faction == FactionTag.Ally ? Color.white : Color.black;
            armyGroupNameText.text = armyGroupAttr.GameplayName.ToString();
            var units = Em.GetBuffer<ArmyGroupUnit>(_targetEntity);
            totalUnitCountText.text = units.Length.ToString();
            var movableData = Em.GetComponentData<ArmyGroupMovableData>(_targetEntity);
            speedText.text = movableData.Speed.ToString("F1");

            var isPlayerFaction = playerFaction == generalAttr.Faction;
            compositionPanel.SetActive(isPlayerFaction);
            formationButton.SetActive(isPlayerFaction && !isSingleton);
        }

        public void ShowComposition()
        {
            Em = World.DefaultGameObjectInjectionWorld.EntityManager;
            var unitTypeDatas = Em.GetBuffer<ArmyGroupUnitTypeData>(_targetEntity);
            
            for (int i = 0; i < Slots.Count; i++)
            {
                var slot = Slots[i];
                var slotComponent = SlotComponents[i];
                if (i < unitTypeDatas.Length)
                {
                    slot.SetActive(true);
                    slotComponent.SetTarget(unitTypeDatas[i]);
                }
                else
                {
                    slot.SetActive(false);
                }
            }
        }
    }
}