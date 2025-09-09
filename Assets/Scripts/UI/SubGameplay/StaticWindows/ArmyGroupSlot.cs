using System;
using SparFlame.Components.Input;
using SparFlame.Systems.General.BasicControl;
using SparFlame.UI.General;
using TMPro;
using Unity.Entities;
using UnityEngine;
using UnityEngine.UI;

namespace SparFlame.UI.SubGameplay.StaticWindows
{
    public class ArmyGroupSlot : MultiShowSlot
    {
       
        
        [SerializeField] private Image armyGroupIcon;
        [SerializeField] private Image filledHp;
        [SerializeField] private Image filledCharge;
        [SerializeField] private TMP_Text unitCountText;
        [SerializeField] private Image filledSprintGray;
        [SerializeField] private Image holdDeeperImage;
        
        [SerializeField] private float leftDeltaAmount;
        [SerializeField] private RectTransform slotTrans;

        [SerializeField] private Button addButton;
        [SerializeField] private Button sprintButton;
        [SerializeField] private Button holdButton;
        [SerializeField] private Button armyGroupSlotButton;
        [SerializeField] private Button armyGroupSkillButton;
        
        public void SetTarget(in ArmyGroupSlotInfo armyGroupSlotInfo)
        {
            _armyGroup = armyGroupSlotInfo.ArmyGroup;
            armyGroupIcon.sprite = ArmyGroupWindowResourceManager.Instance.ArmyGroupIcons[armyGroupSlotInfo.IconType];
            filledHp.fillAmount = armyGroupSlotInfo.HpRatio;
            filledCharge.fillAmount = armyGroupSlotInfo.ChargeRatio;
            unitCountText.text = $"{armyGroupSlotInfo.CurrentUnitCount}/{armyGroupSlotInfo.StartingUnitCount}";
            filledSprintGray.fillAmount = armyGroupSlotInfo.SprintCooldownRatio;
            holdDeeperImage.enabled = armyGroupSlotInfo.IsHolding;
            sprintButton.interactable = armyGroupSlotInfo.SprintCooldownRatio <= 0.001;
            armyGroupSkillButton.interactable = armyGroupSlotInfo.ChargeRatio >= 0.999;
        }

        public void SetAddButton(bool enable)
        {
            addButton.gameObject.SetActive(enable);
        }

        public void SlotMoveRight()
        {
            if(!_isLeft)return;
            var pos = slotTrans.anchoredPosition;
            pos.x += leftDeltaAmount;
            slotTrans.anchoredPosition = pos;
            _isLeft = false;
        }


        #region EventFunctions

        private void Start()
        {
            _inputQuery =
                World.DefaultGameObjectInjectionWorld.EntityManager.CreateEntityQuery(typeof(InputUnitControlData));
            
            addButton.onClick.RemoveAllListeners();
            addButton.onClick.AddListener(() =>
            {
                ArmyGroupSlotWindow.Instance.TryAddSelectedUnitsToArmyGroup(_armyGroup);
            });
            
            sprintButton.onClick.RemoveAllListeners();
            sprintButton.onClick.AddListener(() =>
            {
                ArmyGroupSlotWindow.Instance.SprintArmyGroupUnits(_armyGroup);
            });
            holdButton.onClick.RemoveAllListeners();
            holdButton.onClick.AddListener(() =>
            {
                ArmyGroupSlotWindow.Instance.HoldSwitchArmyGroupUnits(_armyGroup);
            });
            armyGroupSlotButton.onClick.RemoveAllListeners();
            armyGroupSlotButton.onClick.AddListener(() =>
            {
                var inputData = _inputQuery.GetSingleton<InputUnitControlData>();
                ArmyGroupSlotWindow.Instance.SelectArmyGroupUnits(_armyGroup,inputData.AddUnit,
                    Index);
                if(!_isLeft)
                    SlotMoveLeft();
            });
        }

        private void Update()
        {
            if(_inputQuery.IsEmpty)return;
            var inputData = _inputQuery.GetSingleton<InputUnitControlData>();
            if (inputData.SingleSelect || inputData.DragSelectStart )
            {
                if(_isLeft)SlotMoveRight();
            }
        }

        #endregion
        private Entity _armyGroup;
        private EntityQuery _inputQuery;
        private bool _isLeft;

        
        private void SlotMoveLeft()
        {
            if(_isLeft)return;
            var pos = slotTrans.anchoredPosition;
            pos.x -= leftDeltaAmount;
            slotTrans.anchoredPosition = pos;
            _isLeft = true;
        }

  
        
    }
}