using System;
using System.Collections;
using SparFlame.Components.MainGameplay;
using SparFlame.Systems.General.BasicControl;
using SparFlame.UI.General;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace SparFlame.UI.MainGameplay
{
    public class ArmyGroupNewWindow : MultiSlotWindowUtils.MultiSlotsWindow<ArmyGroupTypeIconSlot>
    {
        [SerializeField] private Image newArmyGroupIcon;
        [SerializeField] private GameObject armyGroupNewPanel;
        [SerializeField] private TMP_Text newArmyGroupName;

        // Interface
        public static ArmyGroupNewWindow Instance;
        public Action<string, ArmyGroupIconType> OnEcsNewArmyGroup;
        
        public override void Show(Vector2? pos = null)
        {
            newArmyGroupName.text = "New Army";
            newArmyGroupIcon.sprite = ArmyGroupWindowResourceManager.Instance.ArmyGroupIcons[ArmyGroupIconType.Wolf];
            armyGroupNewPanel.SetActive(true);
            panel.SetActive(false);
            foreach (var slot in Slots)
            {
                slot.SetActive(false);
            }
        }

        public override void Hide()
        {
            armyGroupNewPanel.SetActive(false);
            panel.SetActive(false);
        }

        #region ButtonMethods

        public void OnClickConfirm()
        {
            Hide();
            OnEcsNewArmyGroup?.Invoke(newArmyGroupName.text, _newIconType);
        }

        public void OnClickCancel()
        {
            Hide();
        }

        public void OnClickChooseIcon()
        {
            // This panel is type icon panel
            panel.SetActive(true);
            var types = Enum.GetValues(typeof(ArmyGroupIconType));
            for (var i = 0; i < Slots.Count; i++)
            {
                var slot = Slots[i];
                var slotComponent = SlotComponents[i];
                if (i < types.Length)
                {
                    var iconType = (ArmyGroupIconType)types.GetValue(i);
                    slotComponent.SetTarget(iconType);
                    slot.SetActive(true);
                }
                else
                {
                    slot.SetActive(false);
                }
            }
        }

        public override void OnClickSlot(int slotIndex)
        {
            panel.SetActive(false);
            var types = Enum.GetValues(typeof(ArmyGroupIconType));
            _newIconType = (ArmyGroupIconType)types.GetValue(slotIndex);
            newArmyGroupIcon.sprite = ArmyGroupWindowResourceManager.Instance.ArmyGroupIcons[_newIconType];
        }


        #endregion

        private ArmyGroupIconType _newIconType;

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
            Hide();
        }
    }
}