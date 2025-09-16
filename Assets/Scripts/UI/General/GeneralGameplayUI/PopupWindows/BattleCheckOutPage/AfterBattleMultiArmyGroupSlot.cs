using SparFlame.Components.MainGameplay;
using SparFlame.Systems.General.BasicControl;
using TMPro;
using Unity.Entities;
using UnityEngine;
using UnityEngine.UI;

namespace SparFlame.UI.General.GeneralGameplayUI.PopupWindows.BattleCheckOutPage
{
    public class AfterBattleMultiArmyGroupSlot : MultiShowSlot
    {
        [SerializeField] private TMP_Text deaths;
        [SerializeField] private TMP_Text survivors;
        [SerializeField] private Image filledHp;
        [SerializeField] private TMP_Text armyGroupName;
        [SerializeField] private GameObject disbandIcon;
        [SerializeField] private Image armyGroupIcon;
        
        public void SetTarget(Entity armyGroup)
        {
            var em = World.DefaultGameObjectInjectionWorld.EntityManager;

            var armyGroupAttr = em.GetComponentData<ArmyGroupAttr>(armyGroup);
            var livingUnits = em.GetBuffer<ArmyGroupUnit>(armyGroup);
            var snapShot = em.GetComponentData<BeforeBattleArmyGroupSnapShot>(armyGroup);
            var statData = em.GetComponentData<ArmyGroupStatData>(armyGroup);
            
            armyGroupName.text = armyGroupAttr.gameplayName.ToString();
            armyGroupIcon.sprite = ArmyGroupWindowResourceManager.Instance.ArmyGroupIcons[armyGroupAttr.iconType];
            
            deaths.text = $"{snapShot.UnitCount - livingUnits.Length}";
            survivors.text = $"{ livingUnits.Length}";
            disbandIcon.SetActive(livingUnits.Length == 0);
            filledHp.fillAmount = statData.totalCurrentHp / statData.totalMaxHp;
            
        }
    }
}