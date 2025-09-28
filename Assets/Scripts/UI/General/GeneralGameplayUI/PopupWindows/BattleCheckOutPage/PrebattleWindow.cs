using System;
using System.Collections.Generic;
using SparFlame.Components.General;
using SparFlame.Components.MainGameplay;
using SparFlame.Database;
using SparFlame.Systems.General.BasicControl;
using TMPro;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;
using UnityEngine.UI;

namespace SparFlame.UI.General
{
    public class PrebattleWindow : MonoBehaviour
    {
        #region Config

        [Header("Panels")] [SerializeField] private GameObject panel;
        [SerializeField] private PreBattleMultiArmyGroupsWindow playerArmyGroupPanel;
        [SerializeField] private PreBattleMultiArmyGroupsWindow enemyArmyGroupPanel;
        [SerializeField] private GameObject buttonPanelForPlayerSiege;
        [SerializeField] private GameObject buttonPanelForOthers;


        [Header("Battle field info")] [SerializeField]
        private TMP_Text battleTypeText;

        [SerializeField] private TMP_Text locationName;
        [SerializeField] private TMP_Text locationDescription;
        [SerializeField] private Image ecoBuffImage;
        [SerializeField] private TMP_Text ecoBuffDescription;

        [SerializeField] private TMP_Text playerSideTotalUnitAvgLevel;
        [SerializeField] private TMP_Text playerSideTotalUnitCount;
        [SerializeField] private TMP_Text enemySideTotalUnitAvgLevel;
        [SerializeField] private TMP_Text enemySideTotalUnitCount;

        [SerializeField] private Image centerCityImage;

        [Header("Faction info")] [SerializeField]
        private GameObject playerSideFactionPanel1;

        [SerializeField] private GameObject playerSideFactionPanel2;
        [SerializeField] private GameObject playerSideFactionPanel3;

        [SerializeField] private GameObject enemySideFactionPanel1;
        [SerializeField] private GameObject enemySideFactionPanel2;
        [SerializeField] private GameObject enemySideFactionPanel3;

        [SerializeField] private Image playerSideGeneralFactionImage1;
        [SerializeField] private Image playerSideGeneralFactionImage2;
        [SerializeField] private Image playerSideGeneralFactionImage3;
        [SerializeField] private Image playerSideGeneralFactionImage4;
        [SerializeField] private Image playerSideGeneralFactionImage5;
        [SerializeField] private Image playerSideGeneralFactionImage6;

        
        [SerializeField] private Image playerSideSubFactionImage1;
        [SerializeField] private Image playerSideSubFactionImage2;
        [SerializeField] private Image playerSideSubFactionImage3;
        [SerializeField] private Image playerSideSubFactionImage4;
        [SerializeField] private Image playerSideSubFactionImage5;
        [SerializeField] private Image playerSideSubFactionImage6;


        [SerializeField] private Image enemySideGeneralFactionImage1;
        [SerializeField] private Image enemySideGeneralFactionImage2;
        [SerializeField] private Image enemySideGeneralFactionImage3;
        [SerializeField] private Image enemySideGeneralFactionImage4;
        [SerializeField] private Image enemySideGeneralFactionImage5;
        [SerializeField] private Image enemySideGeneralFactionImage6;
        
        [SerializeField] private Image enemySideSubFactionImage1;
        [SerializeField] private Image enemySideSubFactionImage2;
        [SerializeField] private Image enemySideSubFactionImage3;
        [SerializeField] private Image enemySideSubFactionImage4;
        [SerializeField] private Image enemySideSubFactionImage5;
        [SerializeField] private Image enemySideSubFactionImage6;

        [Header("Loading position info")] [SerializeField]
        private List<UIBlinker> loadingPosBlinkers;

        [SerializeField] private Color playerSideBlinkColor;
        [SerializeField] private Color enemySideBlinkColor;

        #endregion


        public static PrebattleWindow Instance;
        public event Action OnEcsAssault;
        public event Action OnEcsStation;
        public event Action OnEcsBesiege;
        public event Action OnEcsFight;

        public void UpdatePrebattleWindow(SubGameStatus targetSubGameStatus, Entity city,
            NativeList<Entity> playerSideArmyGroups, NativeList<Entity> enemySideArmyGroups,
            NativeList<Entity> notReachedPlayerArmyGroups,
            EcoType ecoType, NativeList<int> playerSideLoadPositions, NativeList<int> enemySideLoadPositions)
        {
            var em = World.DefaultGameObjectInjectionWorld.EntityManager;

            AssignGeneralInfo(targetSubGameStatus, city, ecoType, em);

            // Calculate total statics
            var playerSideParticipatedTotalUnitCount = 0;
            var enemySideParticipatedTotalUnitCount = 0;

            var playerSideParticipatedTotalUnitLevel = 0;
            var enemySideParticipatedTotalUnitLevel = 0;

            var playerSideSubFactionTypes = new List<SubFactionTag>();
            var enemySideSubFactionTypes = new List<SubFactionTag>();
            using var query = em.CreateEntityQuery(typeof(PlayerFactionData));
            var playerFactionData = query.GetSingleton<PlayerFactionData>();
            var playerSideGeneralFaction = playerFactionData.faction;
            var enemySideGeneralFaction = ~playerSideGeneralFaction; 

            // Calculate participated subfactions
            foreach (var armyGroup in playerSideArmyGroups)
            {
                var unitCount = em.GetBuffer<ArmyGroupUnit>(armyGroup).Length;
                var generalAttr = em.GetComponentData<MainGameplayGeneralAttr>(armyGroup);
                var armyGroupAttr = em.GetComponentData<ArmyGroupAttr>(armyGroup);
                if (!playerSideSubFactionTypes.Contains(generalAttr.subFaction))
                    playerSideSubFactionTypes.Add(generalAttr.subFaction);
                playerSideParticipatedTotalUnitCount += unitCount;
                playerSideParticipatedTotalUnitLevel += armyGroupAttr.avgLevel * unitCount;
            }

            foreach (var armyGroup in enemySideArmyGroups)
            {
                var unitCount = em.GetBuffer<ArmyGroupUnit>(armyGroup).Length;
                var generalAttr = em.GetComponentData<MainGameplayGeneralAttr>(armyGroup);
                var armyGroupAttr = em.GetComponentData<ArmyGroupAttr>(armyGroup);
                if (!enemySideSubFactionTypes.Contains(generalAttr.subFaction))
                    enemySideSubFactionTypes.Add(generalAttr.subFaction);
                enemySideParticipatedTotalUnitCount += unitCount;
                enemySideParticipatedTotalUnitLevel += armyGroupAttr.avgLevel * unitCount;
            }

            if (targetSubGameStatus is SubGameStatus.PlayerDefend or SubGameStatus.Support)
            {
                var cityGeneralAttr = em.GetComponentData<MainGameplayGeneralAttr>(city);
                if (!playerSideSubFactionTypes.Contains(cityGeneralAttr.subFaction))
                {
                    playerSideSubFactionTypes.Add(cityGeneralAttr.subFaction);
                }
            }
            else if (targetSubGameStatus == SubGameStatus.PlayerSiege)
            {
                var cityGeneralAttr = em.GetComponentData<MainGameplayGeneralAttr>(city);
                if (!enemySideSubFactionTypes.Contains(cityGeneralAttr.subFaction))
                {
                    enemySideSubFactionTypes.Add(cityGeneralAttr.subFaction);
                }
            }

            AssignBothSideFactionImages(playerSideSubFactionTypes, playerSideGeneralFaction, enemySideSubFactionTypes, enemySideGeneralFaction);

            SetBothSideActualParticipateInfo(playerSideParticipatedTotalUnitCount, enemySideParticipatedTotalUnitCount, playerSideParticipatedTotalUnitLevel, enemySideParticipatedTotalUnitLevel);


            SetBlinkersByLoadingPos(playerSideLoadPositions, enemySideLoadPositions);

            SetBothSideArmyGroupInfos(playerSideArmyGroups, enemySideArmyGroups, notReachedPlayerArmyGroups);
        }

        #region PrivateInfoUpdateMethods

        

        private void AssignGeneralInfo(SubGameStatus targetSubGameStatus, Entity city, EcoType ecoType, EntityManager em)
        {
            battleTypeText.text = targetSubGameStatus == SubGameStatus.Encounter ? "Field Battle" : "Siege Battle";
            centerCityImage.enabled = targetSubGameStatus != SubGameStatus.Encounter;
            buttonPanelForOthers.SetActive(targetSubGameStatus != SubGameStatus.PlayerSiege);
            buttonPanelForPlayerSiege.SetActive(targetSubGameStatus == SubGameStatus.PlayerSiege);


            var prefabId = targetSubGameStatus == SubGameStatus.Encounter
                ? new PrefabId()
                : em.GetComponentData<PrefabId>(city);
            var cityDataItem = targetSubGameStatus == SubGameStatus.Encounter
                ? new CityDataItem()
                : DatabaseManager.CityDatabaseSo.GetItemById(prefabId.value);
            var cityGeneralAttr = em.GetComponentData<MainGameplayGeneralAttr>(city);
            centerCityImage.color = cityGeneralAttr.faction == FactionTag.Light ? Color.white : Color.black;

            var ecoDataItem = DatabaseManager.EcoDatabaseSo.GetEcoDataItemByEcoType(ecoType);

            locationName.text = targetSubGameStatus == SubGameStatus.Encounter
                ? $"{ecoType.ToString()}"
                : $"{cityDataItem.gameplayName}";
            locationDescription.text = targetSubGameStatus == SubGameStatus.Encounter
                ? ecoDataItem.locationDescription
                : cityDataItem.description;
            ecoBuffImage.sprite = BasicUIResourceManager.Instance.EcoBuffSprites[ecoType];
            ecoBuffDescription.text = ecoDataItem.buffDescription;
        }

        private void SetBlinkersByLoadingPos(NativeList<int> playerSideLoadPositions, NativeList<int> enemySideLoadPositions)
        {
            for (var i = 0; i < loadingPosBlinkers.Count; i++)
            {
                var blinker = loadingPosBlinkers[i];
                if (playerSideLoadPositions.Contains(i))
                {
                    blinker.blinkColor = playerSideBlinkColor;
                    blinker.StartBlink();
                }
                else if (enemySideLoadPositions.Contains(i) && !playerSideLoadPositions.Contains(i))
                {
                    blinker.blinkColor = enemySideBlinkColor;
                    blinker.StartBlink();
                }
                else
                {
                    blinker.StopBlink();
                    blinker.gameObject.SetActive(false);
                }
            }
        }

        private void SetBothSideActualParticipateInfo(int playerSideParticipatedTotalUnitCount,
            int enemySideParticipatedTotalUnitCount, int playerSideParticipatedTotalUnitLevel,
            int enemySideParticipatedTotalUnitLevel)
        {
            playerSideTotalUnitCount.text = playerSideParticipatedTotalUnitCount.ToString();
            enemySideTotalUnitCount.text = enemySideParticipatedTotalUnitCount.ToString();

            var playerTotalAvgLevel = playerSideParticipatedTotalUnitCount == 0
                ? 0
                : playerSideParticipatedTotalUnitLevel / playerSideParticipatedTotalUnitCount;
            playerSideTotalUnitAvgLevel.text = $"Lv.{playerTotalAvgLevel}";

            var enemyTotalAvgLevel = enemySideParticipatedTotalUnitCount == 0
                ? 0
                : enemySideParticipatedTotalUnitLevel / enemySideParticipatedTotalUnitCount;
            enemySideTotalUnitAvgLevel.text = $"Lv.{enemyTotalAvgLevel}";
        }

        private void SetBothSideArmyGroupInfos(NativeList<Entity> playerSideArmyGroups, NativeList<Entity> enemySideArmyGroups,
            NativeList<Entity> notReachedPlayerArmyGroups)
        {
            var playerSideArmyGroupInfos = new List<ArmyGroupReachedInfo>();
            foreach (var
                         armyGroup in playerSideArmyGroups)
            {
                playerSideArmyGroupInfos.Add(new ArmyGroupReachedInfo
                {
                    ArmyGroup = armyGroup,
                    IfReached = true
                });
            }

            foreach (var armyGroup in notReachedPlayerArmyGroups)
            {
                playerSideArmyGroupInfos.Add(new
                    ArmyGroupReachedInfo
                    {
                        ArmyGroup = armyGroup,
                        IfReached = false
                    });
            }

            var enemySideArmyGroupInfos = new List<ArmyGroupReachedInfo>();
            foreach (var armyGroup in enemySideArmyGroups)
            {
                enemySideArmyGroupInfos.Add(new ArmyGroupReachedInfo
                {
                    ArmyGroup = armyGroup,
                    IfReached = true
                });
            }

            playerArmyGroupPanel.UpdateCandidates(playerSideArmyGroupInfos);
            enemyArmyGroupPanel.UpdateCandidates(enemySideArmyGroupInfos);
        }

        private void AssignBothSideFactionImages(List<SubFactionTag> playerSideSubFactionTypes, FactionTag playerSideGeneralFaction,
            List<SubFactionTag> enemySideSubFactionTypes, FactionTag enemySideGeneralFaction)
        {
            switch (playerSideSubFactionTypes.Count)
            {
                case 1:
                    playerSideFactionPanel1.SetActive(true);
                    playerSideFactionPanel2.SetActive(false);
                    playerSideFactionPanel3.SetActive(false);
                    playerSideGeneralFactionImage1.sprite =
                        BasicUIResourceManager.Instance.GeneralFactionIconSprites[playerSideGeneralFaction];
                    playerSideSubFactionImage1.sprite =
                        BasicUIResourceManager.Instance.SubFactionIconSprites[playerSideSubFactionTypes[0]];
                    break;
                case 2:
                    playerSideFactionPanel1.SetActive(false);
                    playerSideFactionPanel2.SetActive(true);
                    playerSideFactionPanel3.SetActive(false);
                    playerSideGeneralFactionImage2.sprite =
                        BasicUIResourceManager.Instance.GeneralFactionIconSprites[playerSideGeneralFaction];
                    playerSideGeneralFactionImage3.sprite = 
                        BasicUIResourceManager.Instance.GeneralFactionIconSprites[playerSideGeneralFaction];
                    playerSideSubFactionImage2.sprite =
                        BasicUIResourceManager.Instance.SubFactionIconSprites[playerSideSubFactionTypes[0]];
                    playerSideSubFactionImage3.sprite =
                        BasicUIResourceManager.Instance.SubFactionIconSprites[playerSideSubFactionTypes[1]];
                    break;
                case 3:
                    playerSideFactionPanel1.SetActive(false);
                    playerSideFactionPanel2.SetActive(false);
                    playerSideFactionPanel3.SetActive(true);
                    playerSideGeneralFactionImage4.sprite =
                        BasicUIResourceManager.Instance.GeneralFactionIconSprites[playerSideGeneralFaction];
                    playerSideGeneralFactionImage5.sprite = 
                        BasicUIResourceManager.Instance.GeneralFactionIconSprites[playerSideGeneralFaction];
                    playerSideGeneralFactionImage6.sprite = 
                        BasicUIResourceManager.Instance.GeneralFactionIconSprites[playerSideGeneralFaction];
                    playerSideSubFactionImage4.sprite =
                        BasicUIResourceManager.Instance.SubFactionIconSprites[playerSideSubFactionTypes[0]];
                    playerSideSubFactionImage5.sprite =
                        BasicUIResourceManager.Instance.SubFactionIconSprites[playerSideSubFactionTypes[1]];
                    playerSideSubFactionImage6.sprite =
                        BasicUIResourceManager.Instance.SubFactionIconSprites[playerSideSubFactionTypes[2]];
                    break;
            }

            switch (enemySideSubFactionTypes.Count)
            {
                case 1:
                    enemySideFactionPanel1.SetActive(true);
                    enemySideFactionPanel2.SetActive(false);
                    enemySideFactionPanel3.SetActive(false);
                    enemySideGeneralFactionImage1.sprite =
                        BasicUIResourceManager.Instance.GeneralFactionIconSprites[enemySideGeneralFaction];
                    enemySideSubFactionImage1.sprite =
                        BasicUIResourceManager.Instance.SubFactionIconSprites[enemySideSubFactionTypes[0]];
                    break;
                case 2:
                    enemySideFactionPanel1.SetActive(false);
                    enemySideFactionPanel2.SetActive(true);
                    enemySideFactionPanel3.SetActive(false);
                    enemySideGeneralFactionImage2.sprite =
                        BasicUIResourceManager.Instance.GeneralFactionIconSprites[enemySideGeneralFaction];
                    enemySideGeneralFactionImage3.sprite = 
                        BasicUIResourceManager.Instance.GeneralFactionIconSprites[enemySideGeneralFaction];
                    enemySideSubFactionImage2.sprite =
                        BasicUIResourceManager.Instance.SubFactionIconSprites[enemySideSubFactionTypes[0]];
                    enemySideSubFactionImage3.sprite =
                        BasicUIResourceManager.Instance.SubFactionIconSprites[enemySideSubFactionTypes[1]];
                    break;
                case 3:
                    enemySideFactionPanel1.SetActive(false);
                    enemySideFactionPanel2.SetActive(false);
                    enemySideFactionPanel3.SetActive(true);
                    enemySideGeneralFactionImage4.sprite =
                        BasicUIResourceManager.Instance.GeneralFactionIconSprites[enemySideGeneralFaction];
                    enemySideGeneralFactionImage5.sprite = 
                        BasicUIResourceManager.Instance.GeneralFactionIconSprites[enemySideGeneralFaction];
                    enemySideGeneralFactionImage6.sprite = 
                        BasicUIResourceManager.Instance.GeneralFactionIconSprites[enemySideGeneralFaction];
                    enemySideSubFactionImage4.sprite =
                        BasicUIResourceManager.Instance.SubFactionIconSprites[enemySideSubFactionTypes[0]];
                    enemySideSubFactionImage5.sprite =
                        BasicUIResourceManager.Instance.SubFactionIconSprites[enemySideSubFactionTypes[1]];
                    enemySideSubFactionImage6.sprite =
                        BasicUIResourceManager.Instance.SubFactionIconSprites[enemySideSubFactionTypes[2]];
                    break;
            }
        }
        #endregion

        public void Show()
        {
            panel.SetActive(true);
            GeneralModalWindowController.Instance.Show();
        }

        public void Hide()
        {
            foreach (var blinker in loadingPosBlinkers)
            {
                blinker.StopBlink();
            }
            panel.SetActive(false);
            GeneralModalWindowController.Instance.Hide();
        }

        #region ButtonMethods

        public void OnClickAssault()
        {
            Hide();
            OnEcsAssault?.Invoke();
        }

        public void OnClickStation()
        {
            Hide();
            OnEcsStation?.Invoke();
        }

        public void OnClickBesiege()
        {
            Hide();
            OnEcsBesiege?.Invoke();
        }

        public void OnClickFight()
        {
            Hide();
            OnEcsFight?.Invoke();
        }

        #endregion


        #region EventFunctions

        private void Awake()
        {
            if (!Instance)
                Instance = this;
            else
            {
                Destroy(gameObject);
            }
        }

        private void Start()
        {
            Hide();
        }

        #endregion
    }
}