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

        #region PublicEvents

        public event Action<ResourceLoadingUtils.LoadingProgress> OnSwitchStatusLoadingProgress;

        public event Action<bool> OnPause;
        public event Action<bool> OnResume;

        public event Action OnBackToMainMenu;

        public event Action<FactionTag> OnWinnerWin;

        // Choose faction, if new slot, slot index
        public event Action<FactionTag, bool, int> OnClickSlotAndStartGame;

        public event Action OnSubGameStartForPlayer;
        public event Action<bool> OnMainGameStartForPlayer;

        public event Action<SubGameStatusData> OnEcsSwitchSubGameStatus; // All systems will start running AFTER this action is called.

        public event Action<SubGameStatusData> OnEcsDealInSubGameTag;
        public event Action<ClearGameplayEntitiesType> OnEcsClearGameplayEntities;

        #endregion


        #region GameControlMethods

        public void GameOver(FactionTag winnerFaction)
        {
            OnWinnerWin?.Invoke(winnerFaction);
            PauseGame(false);
        }

        /// <summary>
        /// If true, pause menu will not show and unity time scale will not be set to 0.
        /// </summary>
        /// <param name="isSwitchingGameplay"></param>
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
            Application.Quit();
        }

        public void ClickSlotAndStartGame(FactionTag playerFaction,
            bool ifNewSlot, int slotIndex)
        {
            OnClickSlotAndStartGame?.Invoke(playerFaction, ifNewSlot, slotIndex);
        }


        public void SubGameStartForPlayer()
        {
            StartCoroutine(CheckLoadSubGameplay());
            
        }

        public void MainGameStartForPlayer(bool isTransitionProgress)
        {
            // if (!_ifNewSlot && _ifInMainMenu)
            // {
            //     SaveLoadController.Instance.LoadGameMainData();
            //     SaveLoadController.Instance.LoadMainGameplayData();
            // }

            var lastTimeSaveCitySlot = _em.CreateEntityQuery(typeof(LastTimeSaveCityId))
                .GetSingleton<LastTimeSaveCityId>().value;
            isTransitionProgress = isTransitionProgress || (_ifInMainMenu && lastTimeSaveCitySlot != 0);
            _ifInMainMenu = false;
            InputListener.Instance.EnableMainGameMaps();
            OnMainGameStartForPlayer?.Invoke(isTransitionProgress);
            _em.CreateSingleton<UpdateCityNavMeshRequest>();
        }

        public void EnterPlayerCity(Entity city, bool mainGameplayTransition = false)
        {
            // This is used for enter player city directly after load game
            if (mainGameplayTransition)
            {
                var saveCityId = _em.CreateEntityQuery(typeof(LastTimeSaveCityId)).GetSingletonRW<LastTimeSaveCityId>();
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
            }

            // Set status change type
            _targetSubGameStatusData = new SubGameStatusData
            {
                SubGameStatus = SubGameStatus.PlayerCity,
                City = city,
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
            SceneController.Instance.LoadSceneGroup(loads, _em.GetComponentData<CityAttr>(city).globalId, true);
            SceneController.Instance.UnloadSceneGroup(unloads);

            // Show and check loading progress
            OnSwitchStatusLoadingProgress?.Invoke(_loadingProgress);
            StartCoroutine(CheckResourceLoading());

            // Load needed saved data
            OnEcsDealInSubGameTag?.Invoke(_targetSubGameStatusData);
            SaveLoadController.Instance.SyncLoadSubGameData(_targetSubGameStatusData);
        }

        public void EnterBattleScene(Entity city, EcoType ecoType, SubGameStatus targetSubGameStatus)
        {
            var gameStatusData = _em.CreateEntityQuery(typeof(GameStatusData)).GetSingleton<GameStatusData>();
            if (gameStatusData.Value == GameStatus.SubGaming)
            {
                BackToMainWorld(true);
                StartCoroutine(CheckBackToMainWorldAndEnterBattleScene(city,ecoType, targetSubGameStatus));
                return;
            }
            // Set status change type
            _targetSubGameStatusData = new SubGameStatusData
            {
                SubGameStatus = targetSubGameStatus,
                City = city,
            };

            // Pause game
            PauseGame(true);

            // Load and unload scenes
            var loads = new List<SceneGroupType>
            {
                SceneGroupType.SubWorld,
            };
            switch (targetSubGameStatus)
            {
                case SubGameStatus.PlayerSiege:
                    loads.Add(SceneGroupType.CityEnv);
                    loads.Add(SceneGroupType.CityInvadeFight);
                    break;
                case SubGameStatus.PlayerDefend:
                    loads.Add(SceneGroupType.CityEnv);
                    break;
                case SubGameStatus.Encounter:
                    loads.Add(SceneGroupType.BattleField);
                    break;
                case SubGameStatus.Support:
                    loads.Add(SceneGroupType.CitySupportFight);
                    break;
                case SubGameStatus.None:
                case SubGameStatus.PlayerCity:
                default:
                    BurstSafe.UnexpectedEnum(targetSubGameStatus);
                    break;
            }

            var unloads = new List<SceneGroupType> { SceneGroupType.MainWorld };
            var cityId = city == Entity.Null? -1 : _em.GetComponentData<CityAttr>(city).globalId;
            SceneController.Instance.LoadSceneGroup(loads, cityId, true);
            SceneController.Instance.UnloadSceneGroup(unloads);

            // Show and check loading progress
            OnSwitchStatusLoadingProgress?.Invoke(_loadingProgress);
            StartCoroutine(CheckResourceLoading());

            // Load needed saved data
            OnEcsDealInSubGameTag?.Invoke(_targetSubGameStatusData);
            SaveLoadController.Instance.SyncLoadSubGameData(_targetSubGameStatusData);
        }

        public void BackToMainWorld(bool isTransitionProgress)
        {
            // Set status change type
            _targetSubGameStatusData = new SubGameStatusData
            {
                SubGameStatus = SubGameStatus.None,
                City = Entity.Null,
            };

            // Resume game
            PauseGame(true);

            // Save needed saved data. This is auto save, so should save to tmp only
            SaveLoadController.Instance.SyncSaveGame(true);
            OnEcsDealInSubGameTag?.Invoke(_targetSubGameStatusData);
            DestroyGameplayEntities(ClearGameplayEntitiesType.SubGameplay);

            // Load and unload scenes
            var loads = new List<SceneGroupType> { SceneGroupType.MainWorld };
            var unloads = new List<SceneGroupType> { SceneGroupType.CurrentLoadingSubGameplaySceneGroup };
            SceneController.Instance.LoadSceneGroup(loads);
            SceneController.Instance.UnloadSceneGroup(unloads);

            // Show and check loading progress
            OnSwitchStatusLoadingProgress?.Invoke(_loadingProgress);
            _isTransitionProgress = isTransitionProgress;
            StartCoroutine(CheckResourceLoading());
        }

        public void SwitchSubGameStatus(in SubGameStatusData targetSubGameStatusData)
        {
            OnEcsSwitchSubGameStatus?.Invoke(targetSubGameStatusData);
        }

        public void DestroyGameplayEntities(ClearGameplayEntitiesType clearType)
        {
            OnEcsClearGameplayEntities?.Invoke(clearType);
        }

        #endregion


        // Internal Data
        private SubGameStatusData _targetSubGameStatusData;
        private readonly ResourceLoadingUtils.LoadingProgress _loadingProgress = new();
        private EntityManager _em;
        private bool _ifInMainMenu = true;

        private bool _isTransitionProgress; // Transition progress should not hide loading screen when first time loading complete

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
                MainGameStartForPlayer(_isTransitionProgress);
            }
            else
            {
                SubGameStartForPlayer();
            }

            ResumeGame(true);
            OnEcsSwitchSubGameStatus?.Invoke(_targetSubGameStatusData);
            _isTransitionProgress = false;
        }

        private IEnumerator CheckBackToMainWorldAndEnterBattleScene(Entity city, EcoType ecoType, SubGameStatus targetSubGameStatus)
        {
            while (_isTransitionProgress)
            {
                yield return null;
            }

            var gameStatusData = _em.CreateEntityQuery(typeof(GameStatusData)).GetSingleton<GameStatusData>();
            if (gameStatusData.Value == GameStatus.SubGaming)
            {
                throw new InvalidOperationException("Game status should not be sub gaming here. Something wrong");
            }
            EnterBattleScene(city, ecoType, targetSubGameStatus);
        }

        private IEnumerator CheckLoadSubGameplay()
        {
            var crystalQuery = _em.CreateEntityQuery(typeof(CrystalDef));
            var unitsQuery  = _em.CreateEntityQuery(typeof(UnitAttr));
            while (crystalQuery.IsEmpty && unitsQuery.IsEmpty)
            {
                yield return null;
            }
            InputListener.Instance.EnableSubGameMaps();
            OnSubGameStartForPlayer?.Invoke();
        }
    }
}