using SparFlame.Components.MainGameplay;
using SparFlame.Systems.General.BasicControl;
using TMPro;
using Unity.Entities;
using UnityEngine;
using UnityEngine.UI;

namespace SparFlame.UI.General
{
    public class PreBattleMultiArmyGroupSlot : MultiShowSlot
    {
        [SerializeField] private TMP_Text armyGroupName;
        [SerializeField] private Image armyGroupIcon;
        [SerializeField] private TMP_Text avgLevel;
        [SerializeField] private TMP_Text unitCount;
        [SerializeField] private GameObject notReachedPanel;
        [SerializeField] private TMP_Text reachedDays;
        [SerializeField] private Image filledHp; 
        
        public void SetTarget(Entity armyGroup, bool ifReached)
        {
            var em = World.DefaultGameObjectInjectionWorld.EntityManager;
            var armyGroupAttr = em.GetComponentData<ArmyGroupAttr>(armyGroup);
            armyGroupName.text = armyGroupAttr.gameplayName.ToString();
            armyGroupIcon.sprite = ArmyGroupWindowResourceManager.Instance.ArmyGroupIcons[armyGroupAttr.iconType];
            avgLevel.text = $"Lv.{armyGroupAttr.avgLevel}";
            unitCount.text = em.GetBuffer<ArmyGroupUnit>(armyGroup).Length.ToString();

            var statData = em.GetComponentData<ArmyGroupStatData>(armyGroup);
            filledHp.fillAmount = statData.totalMaxHp == 0 ? 0 : statData.totalCurrentHp / statData.totalMaxHp;
            
            if (ifReached)
            {
                notReachedPanel.SetActive(false);
            }
            else
            {
                notReachedPanel.SetActive(true);
                reachedDays.text = "0 Days";
            }
            
            
        }
    }
}