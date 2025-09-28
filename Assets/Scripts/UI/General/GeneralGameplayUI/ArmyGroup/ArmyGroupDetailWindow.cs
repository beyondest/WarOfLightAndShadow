using SparFlame.Components.General;
using SparFlame.Components.MainGameplay;
using SparFlame.Systems.General.BasicControl;
using TMPro;
using Unity.Entities;
using UnityEngine;
using UnityEngine.UI;

namespace SparFlame.UI.General
{
    public class ArmyGroupDetailWindow : MultiSlotWindowUtils.MultiSlotsWindow<ArmyGroupUnitCompositionSlot>,
        MultiSlotWindowUtils.ISingleTargetWindow
    {
        
     
        [Header("Config")]
        [SerializeField] private bool isSingleton;
        [SerializeField] private GameObject totalPanel;
        [SerializeField] private GameObject compositionPanel;
        [SerializeField] private GameObject formationButton;
        [SerializeField] private GameObject returnToMulti2DWindowButton;
        
        [Header("ArmyGroupAttr")]
        [SerializeField] private Image generalFactionIcon;
        [SerializeField] private Image subFactionIcon;
        [SerializeField] private Image armyGroupIcon;
        [SerializeField] private Image armyGroupHpIcon;
        
        [SerializeField] private TMP_Text armyGroupNameText;
        [SerializeField] private TMP_Text speedText;
        [SerializeField] private TMP_Text totalUnitCountText;
        [SerializeField] private TMP_Text hpRatioText;
        [SerializeField] private TMP_Text avgLevelText;
        
        
        // [SerializeField] private TMP_Text moraleText;
        
        
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
            returnToMulti2DWindowButton.SetActive(false);
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
            UpdateDynamicData();
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
        
        public void ShowReturnButton()
        {
            returnToMulti2DWindowButton.SetActive(true);
        }

        #region ButtonMethods

        public void OnClickReturnToMulti2DWindow()
        {
            Hide();
            returnToMulti2DWindowButton.SetActive(false);
        }
        

        #endregion
        

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
            using var query = Em.CreateEntityQuery(typeof(PlayerFactionData));
            var playerFactionData = query.GetSingleton<PlayerFactionData>();
            var generalAttr = Em.GetComponentData<MainGameplayGeneralAttr>(_targetEntity);
            if (generalAttr.faction == FactionTag.Neutral)
            {
                generalFactionIcon.enabled = false;
                subFactionIcon.enabled = false;
            }
            else
            {
                generalFactionIcon.enabled = true;
                subFactionIcon.enabled = generalAttr.subFaction!= SubFactionTag.None;
                generalFactionIcon.sprite = BasicUIResourceManager.Instance.GeneralFactionIconSprites[generalAttr.faction];
                subFactionIcon.sprite = BasicUIResourceManager.Instance.SubFactionIconSprites[generalAttr.subFaction];
            }
        
            var armyGroupAttr = Em.GetComponentData<ArmyGroupAttr>(_targetEntity);
            armyGroupIcon.sprite = ArmyGroupWindowResourceManager.Instance.ArmyGroupIcons[armyGroupAttr.iconType];
            armyGroupIcon.color = generalAttr.faction == FactionTag.Light ? Color.white : Color.black;
            armyGroupNameText.text = armyGroupAttr.gameplayName.ToString();
            armyGroupHpIcon.sprite = BasicUIResourceManager.Instance.FactionHpSprites[generalAttr.faction];
            
            var relationship =
                FactionUtils.GetRelationship(playerFactionData.faction,
                    playerFactionData.subFaction, generalAttr.faction, generalAttr.subFaction);
            
            compositionPanel.SetActive(relationship is Relationship.Self or Relationship.Ally);
            formationButton.SetActive(relationship == Relationship.Self && isSingleton);
        }

        protected void UpdateDynamicData()
        {
            var units = Em.GetBuffer<ArmyGroupUnit>(_targetEntity);
            totalUnitCountText.text = units.Length.ToString();
            var movableData = Em.GetComponentData<ArmyGroupMovableData>(_targetEntity);
            speedText.text = movableData.minUnitMoveSpeed.ToString("F1");
            var armyGroupAttr = Em.GetComponentData<ArmyGroupAttr>(_targetEntity);
            var statData = Em.GetComponentData<ArmyGroupStatData>(_targetEntity);

            var percent = statData.totalMaxHp == 0 ? 0 : 100 * statData.totalCurrentHp / statData.totalMaxHp;
            hpRatioText.text = $"{(int)percent}%";
            avgLevelText.text = $"Lv. {armyGroupAttr.avgLevel}";
            
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