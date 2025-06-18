using System;
using System.Collections.Generic;
using SparFlame.Components.General;
using SparFlame.Components.Input;
using SparFlame.Systems.General.BasicControl;
using SparFlame.Systems.General.Input;
using SparFlame.UI.General;
using TMPro;
using Unity.Entities;
using UnityEngine;
using UnityEngine.UI;

namespace SparFlame.UI.SubGameplay
{
    public class BuildingUpgradePopUpWindow : MultiSlotWindowUtils.MultiSlotsWindow<AttributeSlot>
    {

        
        [SerializeField] private Image originalSprite;
        [SerializeField] private TMP_Text originalName;
        [SerializeField] private Image originalTier;
        [SerializeField] private Image upgradeSprite;
        [SerializeField] private TMP_Text upgradeName;
        [SerializeField] private Image upgradeTier;
        public static BuildingUpgradePopUpWindow Instance;
        public Action<List<CostList>, Entity> EcsUpgradeBuilding;
        
        
        public void PopUp(List<CostList> upGradeCosts,in SpriteEntityInfo originalInfo, in SpriteEntityInfo upgradeInfo,
            Entity existEntity)
        {
            originalSprite.sprite = originalInfo.Sprite;
            originalName.text = originalInfo.GameplayName;
            originalTier.sprite = BasicUIResourceManager.Instance.TierSprites[originalInfo.Tier];
            
            upgradeSprite.sprite = upgradeInfo.Sprite;
            upgradeName.text = upgradeInfo.GameplayName;
            upgradeTier.sprite = BasicUIResourceManager.Instance.TierSprites[upgradeInfo.Tier]; 
            
            for (var i = 0; i < Slots.Count; i++)
            {
                if (i < upGradeCosts.Count)
                {
                    Slots[i].SetActive(true);
                    var cost = upGradeCosts[i];
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
            Show();
            _costs.AddRange(upGradeCosts);
            _existEntity = existEntity;
        }

        public void OnClickConfirm()
        {
            EcsUpgradeBuilding?.Invoke(_costs,_existEntity);
            Hide();
        }

        public void OnClickCancel()
        {
            Hide();
        }

        private readonly List<CostList> _costs = new ();
        private CustomInputActions _actions;
        private Entity _existEntity;
        private void Awake()
        {
            if(Instance == null)
                Instance = this;
            else
                Destroy(gameObject);
            Hide();
        }

        protected override void Start()
        {
            base.Start();
            _actions = InputListener.Instance.GetCustomInputActions();
        }

        private void Update()
        {
            if (_actions.InfoWindow.CloseWindow.WasPerformedThisFrame())
            {
                OnClickCancel();
            }
        }
    }
    
}