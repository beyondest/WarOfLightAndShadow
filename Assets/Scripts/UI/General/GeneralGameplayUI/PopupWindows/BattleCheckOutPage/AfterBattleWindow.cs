using System;
using SparFlame.Components.General;
using SparFlame.Components.MainGameplay;
using SparFlame.Core.Utils;
using TMPro;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

namespace SparFlame.UI.General.GeneralGameplayUI.PopupWindows.BattleCheckOutPage
{
    public class AfterBattleWindow : MonoBehaviour
    {

        #region Config

        [SerializeField] private GameObject windowPanel;
        [SerializeField] private GameObject buttonPanel1;
        [SerializeField] private GameObject buttonPanel2;
        [SerializeField] private AfterBattleMultiArmyGroupWindow playerArmyGroups;
        [SerializeField] private AfterBattleMultiArmyGroupWindow enemyArmyGroups;
        
        [SerializeField] private TMP_Text battleResultText;
        
        [SerializeField] private TMP_Text unitKills;
        [SerializeField] private TMP_Text structureDestroyed;
        [SerializeField] private TMP_Text survivingUnitsAllies;
        
        [SerializeField] private TMP_Text unitLoss;
        [SerializeField] private TMP_Text structureLost;
        [SerializeField] private TMP_Text survivingUnitsEnemies;

        [SerializeField] private TMP_Text unitsUpgraded;
        [SerializeField] private TMP_Text essenceRewardValue;

        
        #endregion

        public static AfterBattleWindow Instance;
        public event Action OnEcsReturn;
        public event Action OnEcsStay;
        

        public void UpdateInfo(in BattleRecorder recorder,in BattleEndRequest request,
            in SubGameStatusData currentSubGameStatusData,in BeforeBattleTotalSnapShot totalSnapShot,
            int essenceReward,
            NativeArray<Entity> playerSideArmyGroups, NativeArray<Entity> enemySideArmyGroups
            )
        {
            if (currentSubGameStatusData.City != Entity.Null &&
                request.Result is BattleResult.PlayerWin or BattleResult.EnemyRetreat)
            {
                buttonPanel2.SetActive(true);
                buttonPanel1.SetActive(false);
            }
            else
            {
                buttonPanel2.SetActive(false);
                buttonPanel1.SetActive(true);
            }
            playerArmyGroups.UpdateCandidates(playerSideArmyGroups);
            enemyArmyGroups.UpdateCandidates(enemySideArmyGroups);
            
            battleResultText.text = request.Result switch
            {
                BattleResult.EnemyRetreat => "Victory",
                BattleResult.PlayerLose => "Defeat",
                BattleResult.PlayerWin => "Victory",
                BattleResult.PlayerRetreat => "Defeat",
                _ => BurstSafe.UnexpectedEnum(request.Result, "Unknow")
            };
            unitKills.text = recorder.EnemySideDiedCount.ToString();
            structureDestroyed.text = recorder.EnemySideDestroyedBuildingsCount.ToString();
            survivingUnitsAllies.text = $"{totalSnapShot.PlayerSideUnitCount - recorder.PlayerSideDiedCount}";
            
            unitLoss.text = recorder.PlayerSideDiedCount.ToString();
            structureLost.text = recorder.PlayerSideDestroyedBuildingsCount.ToString();
            survivingUnitsEnemies.text = $"{totalSnapShot.EnemySideUnitCount - recorder.EnemySideDiedCount}";
            
            unitsUpgraded.text = recorder.PlayerUnitsUpgradeCount.ToString();
            essenceRewardValue.text = essenceReward.ToString();
            
            windowPanel.SetActive(true);
        }

        #region ButtonMethods


        public void OnClickReturn()
        {
            windowPanel.SetActive(false);
            OnEcsReturn?.Invoke();
        }

        public void OnClickStay()
        {
            windowPanel.SetActive(false);
            OnEcsStay?.Invoke();
        }

        #endregion
        
        #region EventFunctions

        

        private void Awake()
        {
            if(!Instance)
                Instance = this;
            else
                Destroy(gameObject);
        }

        private void Start()
        {
            windowPanel.SetActive(false);
        }
        #endregion

    }
}