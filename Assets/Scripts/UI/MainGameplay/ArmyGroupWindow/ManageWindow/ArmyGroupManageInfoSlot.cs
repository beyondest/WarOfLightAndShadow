using System;
using SparFlame.Components.General;
using SparFlame.UI.General;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SparFlame.UI.MainGameplay
{
    public class ArmyGroupManageInfoSlot : MultiShowSlot
    {
        // Config
        [SerializeField] private GameObject controlPanel; // Delete, add function
        [SerializeField] private ArmyGroupDetailWindow armyGroupDetailWindow;
        [SerializeField] private GameObject armyGroupCompositionPanel;
        private void Start()
        {
            armyGroupCompositionPanel.SetActive(false);
        }

        public void SetTarget(in ArmyGroupManageInfo manageInfo)
        {
            armyGroupDetailWindow.TrySwitchTarget(manageInfo.ArmyGroupEntity);
        }

        public void OnClickDelete()
        {
            ArmyGroupManageWindow.Instance.DeleteArmyGroup(Index);
        }

        public void OnClickAdd()
        {
            ArmyGroupManageWindow.Instance.AddToArmyGroup(Index);
        }
        public void OnClickShowComposition()
        {
            armyGroupCompositionPanel.SetActive(true);
            armyGroupDetailWindow.ShowComposition();
        }

        public void OnClickCloseComposition()
        {
            armyGroupCompositionPanel.SetActive(false);
        }
    }
}