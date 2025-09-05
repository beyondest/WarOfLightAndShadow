using System;
using System.Collections;
using System.Collections.Generic;
using SparFlame.Components.General;
using SparFlame.Components.MainGameplay;
using SparFlame.Components.SubGameplay;
using SparFlame.Core.Utils;
using SparFlame.Systems.General.Input;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

namespace SparFlame.Systems.General.BasicControl
{
    public class GameController : MonoBehaviour
    {
        [SerializeField] private float checkInitInterval = 0.1f;

        public static GameController Instance;

        #region Events
        
        public event Action<ResourceLoadingUtils.LoadingProgress> OnSwitchStatusLoadingProgress;

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

        public void EndGameToMainMenu(bool ifFromSubGameplay)
        {
            _ifInMainMenu = true;
            InputListener.Instance.DisableAllMaps();
            ResumeGame(false);
            SaveLoadController.Instance.SyncSaveGame();
            var unloads = new List<SceneGroupType>
            {
                ifFromSubGameplay
                    ? SceneGroupType.CurrentLoadingSubGameplaySceneGroup
                    : SceneGroupType.MainWorld
            };
            SceneController.Instance.UnloadSceneGroup(unloads);
            OnBackToMainMenu?.Invoke();
        }

        public void ExitGame()
        {
            SaveLoadController.Instance.SyncSaveGame();
            Application.Quit();
        }

        public void PlayerChooseFactionAndStartGame(FactionTag playerFaction)
        {
            OnPlayerChooseFaction?.Invoke(playerFaction);
        }

        public void PlayerChooseSavingSlot(int slot, bool ifNew)
        {
            _ifNewSlot = ifNew;
            OnPlayerChooseSavingSlot?.Invoke(slot, ifNew);
        }

        public void SubGameStartForPlayer()
        {
            InputListener.Instance.EnableSubGameMaps();
            OnSubGameStartForPlayer?.Invoke();
        }

        public void MainGameStartForPlayer()
        {
            if (!_ifNewSlot && _ifInMainMenu)
            {
                SaveLoadController.Instance.LoadGameMainData();
                SaveLoadController.Instance.LoadMainGameplayData();
            }
            _ifInMainMenu = false;
            InputListener.Instance.EnableMainGameMaps();
            OnMainGameStartForPlayer?.Invoke();
            _em.CreateSingleton<UpdateCityNavMeshRequest>();
        }

        public void EnterPlayerCity(Entity city, bool mainGameplayTransition = false)
        {
            // This is used for enter player city directly after load game
            if (mainGameplayTransition)
            {
                var saveCityId = _em.CreateEntityQuery(typeof(SaveCityId)).GetSingletonRW<SaveCityId>();
                var query = _em.CreateEntityQuery(typeof(CityAttr));
                var cities = query.ToEntityArray(Allocator.Temp);
                var cityAttrs = query.ToComponentDataArray<CityAttr>(Allocator.Temp);
                for (var i = 0; i < cityAttrs.Length; i++)
                {
                    var cityAttr = cityAttrs[i];
                    if (cityAttr.globalId == saveCityId.ValueRO.value)
                        city = cities[i];
                }
                saveCityId.ValueRW.value = 0; // Reset save city id
                saveCityId.ValueRW.mainGameplayTransition = false;
            }
            // Set status change type
            _targetSubGameStatusData = new SubGameStatusData
            {
                SubGameStatus = SubGameStatus.PlayerCity,
                City = city,
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
            SceneController.Instance.LoadSceneGroup(loads, _em.GetComponentData<CityAttr>(city).globalId,true);
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
                    BattleTriggerRequest = default
                });
            SubGameStartForPlayer();
        }

        #endregion

        // Internal Data
        private SubGameStatusData _targetSubGameStatusData;
        private readonly ResourceLoadingUtils.LoadingProgress _loadingProgress = new();
        private EntityManager _em;
        private bool _ifNewSlot;
        private bool _ifInMainMenu = true;

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