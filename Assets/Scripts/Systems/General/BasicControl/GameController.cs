using System;
using System.Collections;
using System.Collections.Generic;
using SparFlame.Components.General;
using SparFlame.Components.MainGameplay;
using SparFlame.Core.Utils;
using SparFlame.Systems.General.Input;
using Unity.Entities;
using UnityEngine;

namespace SparFlame.Systems.General.BasicControl
{
    public class GameController : MonoBehaviour
    {
        [SerializeField] private float checkInitInterval = 0.1f;

        public static GameController Instance;

        #region Events
        
        public Action<ResourceLoadingUtils.LoadingProgress> OnSwitchStatusLoadingProgress;

        public event Action<bool> OnPause;
        public event Action<bool> OnResume;

        public event Action OnBackToMainMenu;

        public event Action<FactionTag> OnWinnerWin;  

        public event Action<FactionTag> OnPlayerChooseFaction;

        public event Action OnSubGameStartForPlayer;
        public event Action OnMainGameStartForPlayer;

        public event Action< SubGameStatusData> OnSwitchGameStatusForSystems; // All systems will start running AFTER this action is called.

        public event Action<SubGameStatusData> OnEcsDealInSubGameTag;
        public event Action<ClearGameplayEntitiesType> OnEcsClearGameplayEntities; 
        
        public event Action<int, bool> OnPlayerChooseSavingSlot;


        #endregion


        #region GameControlMethods

        public void GameOver(FactionTag winnerFaction)
        {
            OnWinnerWin?.Invoke(winnerFaction);
            PauseGame(false);
        }

        public void PauseGame(bool isSwitchingGameplay)
        {
            OnPause?.Invoke(isSwitchingGameplay);
        }

        public void ResumeGame(bool isSwitchingGameplay)
        {
            OnResume?.Invoke(isSwitchingGameplay);
        }

        public void EndGameToMainMenu()
        {
            InputListener.Instance.DisableAllMaps();
            ResumeGame(false);
            OnBackToMainMenu?.Invoke();
        }

        public void ExitGame()
        {
            Application.Quit();
        }

        public void PlayerChooseFaction(FactionTag playerFaction)
        {
            OnPlayerChooseFaction?.Invoke(playerFaction);
        }

        public void PlayerChooseSavingSlot(int slot, bool ifNew)
        {
            OnPlayerChooseSavingSlot?.Invoke(slot, ifNew);
        }

        public void SubGameStartForPlayer()
        {
            InputListener.Instance.EnableSubGameMaps();
            OnSubGameStartForPlayer?.Invoke();
        }

        public void MainGameStartForPlayer()
        {
            InputListener.Instance.EnableMainGameMaps();
            OnMainGameStartForPlayer?.Invoke();
        }

        public void EnterPlayerCity(Entity city)
        {
            // Set status change type
            _targetSubGameStatusData = new SubGameStatusData
            {
                SubGameStatus = SubGameStatus.PlayerCity,
                City = city,
                IsInBattle = false,
                BattleTriggerRequest = default
            };
            
            // Pause game
            PauseGame(true);

            // Load and unload scenes
            var loads = new List<SceneGroupType>
            {
                SceneGroupType.SubWorld,
                SceneGroupType.CityEnv
            };
            var unloads = new List<SceneGroupType> { SceneGroupType.MainWorld };
            SceneController.Instance.LoadSceneGroup(loads, _em.GetComponentData<CityAttr>(city).ID,true);
            SceneController.Instance.UnloadSceneGroup(unloads);
            
            // Show and check loading progress
            OnSwitchStatusLoadingProgress?.Invoke(_loadingProgress);
            StartCoroutine(CheckResourceLoading());
            
            // Load needed saved data
            OnEcsDealInSubGameTag?.Invoke(_targetSubGameStatusData);
            SaveLoadController.Instance.SyncLoadSubGameData(_targetSubGameStatusData);
        }

        public void BackToMainWorld()
        {
            // Set status change type
            _targetSubGameStatusData = new SubGameStatusData
            {
                SubGameStatus = SubGameStatus.None,
                City = Entity.Null,
                IsInBattle = false,
                BattleTriggerRequest = default
            };
            
            // Resume game
            PauseGame(true);
            
            // Save needed saved data
            SaveLoadController.Instance.SyncSaveGame();
            OnEcsDealInSubGameTag?.Invoke(_targetSubGameStatusData);
            DestroyGameplayEntities(ClearGameplayEntitiesType.SubGameplay);

            // Load and unload scenes
            var loads = new List<SceneGroupType> { SceneGroupType.MainWorld };
            var unloads = new List<SceneGroupType> { SceneGroupType.CurrentLoadingSubGameplaySceneGroup };
            SceneController.Instance.LoadSceneGroup(loads);
            SceneController.Instance.UnloadSceneGroup(unloads);
            
            // Show and check loading progress
            OnSwitchStatusLoadingProgress?.Invoke(_loadingProgress);
            StartCoroutine(CheckResourceLoading());
            
        }

        public void DestroyGameplayEntities(ClearGameplayEntitiesType clearType)
        {
            OnEcsClearGameplayEntities?.Invoke(clearType);
        }
        

     

        #endregion

        #region TestButtonMethods

        public void OnClickSwitchToSubGame()
        {
            OnSwitchGameStatusForSystems?.Invoke(
                new SubGameStatusData
                {
                    SubGameStatus = SubGameStatus.Encounter,
                    City = Entity.Null,
                    IsInBattle = false,
                    BattleTriggerRequest = default
                });
            SubGameStartForPlayer();
        }

        #endregion

        // Internal Data
        private SubGameStatusData _targetSubGameStatusData;
        private readonly ResourceLoadingUtils.LoadingProgress _loadingProgress = new();
        private EntityManager _em;

        #region EventFunctions

        private void Awake()
        {
            if (!Instance)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
            }

            Application.targetFrameRate = 60;
        }

        private void Start()
        {
            _em = World.DefaultGameObjectInjectionWorld.EntityManager;
            
        }

        #endregion

        private IEnumerator CheckResourceLoading()
        {
            while (true)
            {
                if (SceneController.Instance.IsInitialized)
                    break;
                _loadingProgress.Report(SceneController.Instance.InitProgress);
                yield return new WaitForSeconds(checkInitInterval);
            }

            if (_targetSubGameStatusData.SubGameStatus == SubGameStatus.None)
            {
                MainGameStartForPlayer();
            }
            else
            {
                SubGameStartForPlayer();
            }

            ResumeGame(true);
            OnSwitchGameStatusForSystems?.Invoke( _targetSubGameStatusData);
        }
    }
}