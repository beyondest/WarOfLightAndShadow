using System.Collections.Generic;
using SparFlame.Components.General;
using SparFlame.Components.SubGameplay;
using SparFlame.Core.Interfaces;
using SparFlame.UI.General;
using Unity.Entities;
using UnityEngine;

namespace SparFlame.UI.MainGameplay
{
    public class ArmyGroupManageInfoSlot : MultiShowSlot,IResourceManager
    {
        // Config
        [SerializeField] private ArmyGroupManageDetailInfoSlot armyGroupDetailInfoSlot;
        [SerializeField] private ArmyGroupManageCompositionWindow compoPanel;
        

        public void SetTarget(in ArmyGroupManageInfo manageInfo)
        {
            armyGroupDetailInfoSlot.TrySwitchTarget(manageInfo.ArmyGroupEntity);
            _targetEntity = manageInfo.ArmyGroupEntity;
            compoPanel.TrySwitchTarget(_targetEntity);
        }
        
        public void UpdateComposition(bool tierFilterEnabled, Tier currentFilterTier,
        List<UnitType> filterUnitTypes)
        {
            compoPanel.UpDateComposition(tierFilterEnabled, currentFilterTier, filterUnitTypes);
        }

        public void OnClickDelete()
        {
            ArmyGroupManageWindow.Instance.DeleteArmyGroup(Index);
        }

  

        public void ClearSelected()
        {
            compoPanel.ClearSelected();
        }

        private Entity _targetEntity;

        public bool IsInitialized => compoPanel.IsInitialized;
        public float InitProgress => compoPanel.InitProgress;
        public void LoadResources()
        {
            compoPanel.LoadResources();
        }

        public void UnloadResources()
        {
            compoPanel.UnloadResources();
        }
    }
}