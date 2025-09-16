using SparFlame.Components.General;
using SparFlame.Systems.General.BasicControl;
using SparFlame.Systems.General.Battle;
using Unity.Entities;
using UnityEngine;
using UnityEngine.UI;

namespace SparFlame.UI.SubGameplay.StaticWindows.Buttons
{
    public class SubGameplayStaticButtonManager : MonoBehaviour
    {
        public GameObject constructButton;
        public GameObject armyGroupManageButton;
        public GameObject enterAccurateSelectionButton;
        public GameObject backToMainWorldButton;
        public GameObject retreatButton;

        private void Start()
        {
            constructButton.SetActive(false);
            armyGroupManageButton.SetActive(false);
            enterAccurateSelectionButton.SetActive(false);
            backToMainWorldButton.SetActive(false);
            GameController.Instance.OnSwitchGameStatusForSystems += targetSubgameStatus =>
            {
                backToMainWorldButton.SetActive(targetSubgameStatus.SubGameStatus == SubGameStatus.PlayerCity);
                constructButton.SetActive(targetSubgameStatus.SubGameStatus == SubGameStatus.PlayerCity);
                armyGroupManageButton.SetActive(targetSubgameStatus.SubGameStatus == SubGameStatus.PlayerCity);
                enterAccurateSelectionButton.SetActive(targetSubgameStatus.SubGameStatus == SubGameStatus.PlayerCity);
                retreatButton.SetActive(targetSubgameStatus.SubGameStatus != SubGameStatus.None &&
                                        targetSubgameStatus.SubGameStatus != SubGameStatus.PlayerCity);
            };
            backToMainWorldButton.GetComponent<Button>().onClick.AddListener(OnClickBackToMainWorldButton);
            retreatButton.GetComponent<Button>().onClick.AddListener(OnClickRetreatButton);
        }

        public void OnClickBackToMainWorldButton()
        {
            GameController.Instance.BackToMainWorld(false);
        }

        public void OnClickRetreatButton()
        {
            var em = World.DefaultGameObjectInjectionWorld.EntityManager;
            var query = em.CreateEntityQuery(typeof(PlayerRetreatRequest));
            var query2 = em.CreateEntityQuery(typeof(BattleEndRequest));
            if(!query.IsEmpty || !query2.IsEmpty)return;
            BattleUtils.StartRetreat(em, true);
        }
    }
}