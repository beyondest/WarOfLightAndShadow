using SparFlame.UI.General;
using Unity.Entities;
using UnityEngine;

namespace SparFlame.UI.MainGameplay
{
    public class ArmyGroupManageInfoSlot : MultiShowSlot
    {
        // Config
        [SerializeField] private GameObject controlPanel; // Delete, add function
        [SerializeField] private ArmyGroupManageDetailInfoSlot armyGroupDetailInfoSlot;
   

        public void SetTarget(in ArmyGroupManageInfo manageInfo)
        {
            armyGroupDetailInfoSlot.TrySwitchTarget(manageInfo.ArmyGroupEntity);
            _targetEntity = manageInfo.ArmyGroupEntity;
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
            if (ArmyGroupManageCompositionWindow.Instance.TrySwitchTarget(_targetEntity))
            {
                ArmyGroupManageCompositionWindow.Instance.Show();
            }
            
        }

        public void OnClickCloseComposition()
        {
            ArmyGroupManageCompositionWindow.Instance.Hide();
        }

        private Entity _targetEntity;

    }
}