using System;
using System.Collections;
using System.Collections.Generic;
using SparFlame.Components.General;
using SparFlame.Components.MainGameplay;
using SparFlame.Components.SubGameplay;
using SparFlame.Core.Structs;
using SparFlame.Core.Utils;
using SparFlame.Systems.General.BasicControl.GlobalMonos;
using SparFlame.Systems.General.Input;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

namespace SparFlame.Systems.General.BasicControl
{
    public class GameController : MonoBehaviour
    {
        [Serializable]
        public struct ExtraWaitConfig
        {
            public int maxSubGameplayGeneralAttrEntityCount;
            public int waitFrameCount;
        }

        [SerializeField] private List<ExtraWaitConfig> extraWaitConfigs;
        public static GameController Instance;

        #region UI

        public readonly ResourceLoadingUtils.LoadingProgress LoadingProgress = new();
        public event Action OnShowLoadingScreen;
        public event Action OnHideLoadingScreen;

        public event Action OnStartWait;
        public event Action OnEndWait;

        #endregion

        #region Init Calls

        public event Action<PlayerFactionData> OnSetPlayerFactionData;

        public event Action OnEcsBeginSystemInit;

        #endregion


        #region Clean Calls

        public event Action OnEcsDestroyInitialization;


        public event Action<ClearGameplayEntitiesType> OnEcsClearGameplayEntities;
        public event Action OnCleanDontDestroyOnLoads;

        #endregion

        #region Switch Calls

        // Target sub game status, Current Sub Game status
        public event Action<SubGameStatusData, SubGameStatusData> OnSwitchGameStatus;

        // This event should be invoked before load sub data or enter main gameplay from sub gameplay
        public event Action<SubGameStatusData, SubGameStatusData> OnEcsDealWithInSubGameTag;

        #endregion


        #region Simple game control

        /// <summary>
        /// If true, pause menu will not show and unity timescale will not be set to 0.
        /// </summary>
        /// <param name="isSwitchingGameplay"></param>
        public void PauseGame(bool isSwitchingGameplay)
        {
            OnPause?.Invoke(isSwitchingGameplay);
        }

        public event Action<bool> OnPause;

        public void ResumeGame(bool isSwitchingGameplay)
        {
            OnResume?.Invoke(isSwitchingGameplay);
        }

        public void ExitGame()
        {
            Application.Quit();
        }

        public event Action<bool> OnResume;

        #endregion

        #region Complex game control

        // -------------------------------Start Game First Time ---------------------------//
        public IEnumerator StartGameFirstTime(FactionTag playerFaction,
            bool ifNewGame, int slotIndex, int cityPrefabId)
        {
            // Show loading screen
            OnSetPlayerFactionData?.Invoke(new PlayerFactionData
                { faction = playerFaction, subFaction = SubFactionTag.LightFaction1 });
            OnShowLoadingScreen?.Invoke();

            // Set init data
            SaveLoadController.Instance.SetSavingSlotAndDoSomeCleaning(ifNewGame, slotIndex);
            SceneController.Instance.SetSceneLoadInitData(playerFaction, ifNewGame, cityPrefabId);
            // Load game main data and resources
            var ops = new List<ResourceOperation>
            {
                GeneralResourceManager.Instance.LoadResourcesAsync()
            };
            var shouldEnterCityDirectly = cityPrefabId != 0;
            var targetSubGameStatusData = new SubGameStatusData
            {
                SubGameStatus = shouldEnterCityDirectly ? SubGameStatus.PlayerCity : SubGameStatus.None,
                City = Entity.Null
            };
            var currentSubGameStatusData = new SubGameStatusData
            {
                SubGameStatus = SubGameStatus.None,
                City = Entity.Null
            };
            if (!ifNewGame)
            {
                ops.Add(SaveLoadController.Instance.LoadGameMainDataAsync());
            }


            yield return CustomCoroutineRunner.Instance.WhenAll(ops, LoadingProgress);
            if (shouldEnterCityDirectly)
            {
                targetSubGameStatusData.City = GetCity(cityPrefabId);
                OnEcsDealWithInSubGameTag?.Invoke(targetSubGameStatusData, currentSubGameStatusData);
                yield return SaveLoadController.Instance.LoadGameSubDataAsync(targetSubGameStatusData);
            }

            OnEcsBeginSystemInit?.Invoke();
            yield return CheckEcsSystemInitComplete();
            yield return CheckNecessaryEntities(targetSubGameStatusData, currentSubGameStatusData);
            yield return WaitForExtraFramesBeforeSwitchGameplay(targetSubGameStatusData);
            OnSwitchGameStatus?.Invoke(targetSubGameStatusData, currentSubGameStatusData);
            if (shouldEnterCityDirectly) InputListener.Instance.EnableSubGameMaps();
            else InputListener.Instance.EnableMainGameMaps();
            OnHideLoadingScreen?.Invoke();
        }

        // ------------------------------Load Saving Slot In Game-----------------------------//
        public IEnumerator LoadSavingSlot(int slotIndex,
            RiftGameFileQuickData quickData)
        {
            OnSetPlayerFactionData?.Invoke(new PlayerFactionData
                { faction = quickData.Faction, subFaction = quickData.SubFaction });
            OnShowLoadingScreen?.Invoke();
            yield return CheckSaveComplete();
            InputListener.Instance.DisableAllMaps();
            // Reset scenes and initialization
            var currentSubGameStatus = _subGameStatusDataQuery.GetSingleton<SubGameStatusData>().SubGameStatus;
            var ifFromSubGameplay = currentSubGameStatus != SubGameStatus.None;
            var unloads = new List<SceneGroupType>
            {
                ifFromSubGameplay
                    ? SceneGroupType.CurrentLoadingSubGameplaySceneGroup
                    : SceneGroupType.MainWorld,
                SceneGroupType.Init
            };
            yield return SceneController.Instance.UnloadSceneGroupAsync(unloads);
            ResumeGame(false); // Load from pause menu
            OnEcsDestroyInitialization?.Invoke();
            OnEcsClearGameplayEntities?.Invoke(ClearGameplayEntitiesType.All);
            OnCleanDontDestroyOnLoads?.Invoke();
            // Set init data
            SaveLoadController.Instance.SetSavingSlotAndDoSomeCleaning(false, slotIndex);
            SceneController.Instance.SetSceneLoadInitData(quickData.Faction, false, quickData.CityPrefabId);
            // Start loading
            var shouldEnterCityDirectly = quickData.CityPrefabId != 0;
            var loads = new List<SceneGroupType>();
            if (shouldEnterCityDirectly)
            {
                loads.Add(SceneGroupType.SubWorld);
                loads.Add(SceneGroupType.CityEnv);
            }
            else
            {
                loads.Add(SceneGroupType.MainWorld);
            }

            var ops = new List<ResourceOperation>
            {
                SaveLoadController.Instance.LoadGameMainDataAsync(),
                SceneController.Instance.LoadSceneGroupAsync(loads, quickData.CityPrefabId, shouldEnterCityDirectly)
            };
            var targetSubGameStatusData = new SubGameStatusData
            {
                SubGameStatus = shouldEnterCityDirectly ? SubGameStatus.PlayerCity : SubGameStatus.None,
                City = Entity.Null
            };
            var currentSubGameStatusData = new SubGameStatusData
            {
                SubGameStatus = SubGameStatus.None,
                City = Entity.Null
            };

            yield return CustomCoroutineRunner.Instance.WhenAll(ops, LoadingProgress);

            if (shouldEnterCityDirectly)
            {
                targetSubGameStatusData.City = GetCity(quickData.CityPrefabId);
                OnEcsDealWithInSubGameTag?.Invoke(targetSubGameStatusData, currentSubGameStatusData);
                yield return SaveLoadController.Instance.LoadGameSubDataAsync(targetSubGameStatusData);
            }

            // Load complete, start init ecs system
            OnEcsBeginSystemInit?.Invoke();
            yield return CheckEcsSystemInitComplete();
            yield return CheckNecessaryEntities(targetSubGameStatusData, currentSubGameStatusData);
            yield return WaitForExtraFramesBeforeSwitchGameplay(targetSubGameStatusData);
            OnSwitchGameStatus?.Invoke(targetSubGameStatusData, currentSubGameStatusData);
            if (shouldEnterCityDirectly) InputListener.Instance.EnableSubGameMaps();
            else InputListener.Instance.EnableMainGameMaps();
            OnHideLoadingScreen?.Invoke();
        }

        // ----------------------------Ene Game to Main Menu--------------------------//
        public IEnumerator EndGameToMainMenu()
        {
            OnShowLoadingScreen?.Invoke();
            yield return CheckSaveComplete();
            var currentSubGameStatusData = _subGameStatusDataQuery.GetSingleton<SubGameStatusData>().SubGameStatus;
            var ifFromSubGameplay = currentSubGameStatusData != SubGameStatus.None;
            var unloads = new List<SceneGroupType>
            {
                ifFromSubGameplay
                    ? SceneGroupType.CurrentLoadingSubGameplaySceneGroup
                    : SceneGroupType.MainWorld
            };
            var ops = new List<ResourceOperation> { SceneController.Instance.UnloadSceneGroupAsync(unloads) };
            yield return CustomCoroutineRunner.Instance.WhenAll(ops, LoadingProgress);
            ResumeGame(false);
            OnCleanDontDestroyOnLoads?.Invoke();
            OnEcsDestroyInitialization?.Invoke();
            OnEcsClearGameplayEntities?.Invoke(ClearGameplayEntitiesType.All);
            GeneralResourceManager.Instance.ReleaseAllResources();
            InputListener.Instance.DisableAllMaps();
            OnHideLoadingScreen?.Invoke();
        }

        // ------------------------ Sub World to Main World -----------------//
        public IEnumerator SubWorldToMainWorld()
        {
            OnShowLoadingScreen?.Invoke();
            yield return CheckSaveComplete();
            PauseGame(true);
            InputListener.Instance.DisableAllMaps();

            yield return SaveLoadController.Instance.SaveAsync(SaveType.SaveSubGameplayDataToTmp, -1);
            var targetSubGameStatusData = new SubGameStatusData
            {
                SubGameStatus = SubGameStatus.None,
                City = Entity.Null,
            };
            var currentSubGameStatusData = _subGameStatusDataQuery.GetSingleton<SubGameStatusData>();
            OnEcsDealWithInSubGameTag?.Invoke(targetSubGameStatusData, currentSubGameStatusData);
            OnEcsClearGameplayEntities?.Invoke(ClearGameplayEntitiesType.SubGameplay);

            // Save, unload, and load
            var loads = new List<SceneGroupType> { SceneGroupType.MainWorld };
            var unloads = new List<SceneGroupType> { SceneGroupType.CurrentLoadingSubGameplaySceneGroup };
            var ops = new List<ResourceOperation>
            {
                SceneController.Instance.UnloadSceneGroupAsync(unloads),
                SceneController.Instance.LoadSceneGroupAsync(loads, 0, false),
            };
            yield return CustomCoroutineRunner.Instance.WhenAll(ops, LoadingProgress);
            yield return CheckNecessaryEntities(targetSubGameStatusData, currentSubGameStatusData);
            yield return WaitForExtraFramesBeforeSwitchGameplay(targetSubGameStatusData);
            ResumeGame(true);
            OnSwitchGameStatus?.Invoke(targetSubGameStatusData, currentSubGameStatusData);
            InputListener.Instance.EnableMainGameMaps();
            OnHideLoadingScreen?.Invoke();
        }


        // -------------------- Enter Player City --------------------//
        public IEnumerator EnterPlayerCity(Entity city)
        {
            OnShowLoadingScreen?.Invoke();
            yield return CheckSaveComplete();
            PauseGame(true);
            InputListener.Instance.DisableAllMaps();
            // Set status change type
            var targetSubGameStatusData = new SubGameStatusData
            {
                SubGameStatus = SubGameStatus.PlayerCity,
                City = city,
            };
            var currentSubGameStatusData = _subGameStatusDataQuery.GetSingleton<SubGameStatusData>();
            var loads = new List<SceneGroupType>
            {
                SceneGroupType.SubWorld,
                SceneGroupType.CityEnv
            };
            var unloads = new List<SceneGroupType> { SceneGroupType.MainWorld };
            var ops = new List<ResourceOperation>()
            {
                SceneController.Instance.LoadSceneGroupAsync(loads, _em.GetComponentData<PrefabId>(city).value,
                    true),
                SceneController.Instance.UnloadSceneGroupAsync(unloads),
                SaveLoadController.Instance.LoadGameSubDataAsync(targetSubGameStatusData)
            };
            OnEcsDealWithInSubGameTag?.Invoke(targetSubGameStatusData, currentSubGameStatusData);
            yield return CustomCoroutineRunner.Instance.WhenAll(ops, LoadingProgress);
            yield return CheckNecessaryEntities(targetSubGameStatusData, currentSubGameStatusData);
            yield return WaitForExtraFramesBeforeSwitchGameplay(targetSubGameStatusData);
            ResumeGame(true);
            OnSwitchGameStatus?.Invoke(targetSubGameStatusData, currentSubGameStatusData);
            InputListener.Instance.EnableSubGameMaps();
            OnHideLoadingScreen?.Invoke();
        }

        // ----------------- Enter Battle Scene -----------------//
        public IEnumerator EnterBattleScene(Entity city, EcoType ecoType, SubGameStatus targetSubGameStatus)
        {
            OnShowLoadingScreen?.Invoke();
            yield return CheckSaveComplete();
            var gameStatusData = _mainGameStatusDataQuery.GetSingleton<GameStatusData>();
            if (gameStatusData.Value == GameStatus.SubGaming)
            {
                yield return SubWorldToMainWorld(); // Automatically save sub data to tmp
            }
            OnShowLoadingScreen?.Invoke();
            PauseGame(true);
            // Set status change type
            var targetSubGameStatusData = new SubGameStatusData
            {
                SubGameStatus = targetSubGameStatus,
                City = city,
            };
            var currentSubGameStatusData = new SubGameStatusData
            {
                SubGameStatus = SubGameStatus.None,
                City = Entity.Null
            };
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
            var cityId = city == Entity.Null ? -1 : _em.GetComponentData<PrefabId>(city).value;
            var ops = new List<ResourceOperation>()
            {
                SceneController.Instance.LoadSceneGroupAsync(loads, cityId, true, ecoType),
                SceneController.Instance.UnloadSceneGroupAsync(unloads),
                SaveLoadController.Instance.LoadGameSubDataAsync(targetSubGameStatusData)
            };
            OnEcsDealWithInSubGameTag?.Invoke(targetSubGameStatusData, currentSubGameStatusData);
            yield return CustomCoroutineRunner.Instance.WhenAll(ops, LoadingProgress);
            yield return CheckNecessaryEntities(targetSubGameStatusData, currentSubGameStatusData);
            yield return WaitForExtraFramesBeforeSwitchGameplay(targetSubGameStatusData);
            // All resources and saving data loaded
            ResumeGame(true);
            OnSwitchGameStatus?.Invoke(targetSubGameStatusData, currentSubGameStatusData);
            InputListener.Instance.EnableSubGameMaps();
            OnHideLoadingScreen?.Invoke();
        }

        // ----------------- Stay To City After Battle -----------------//
        public void StayToCityAfterBattle()
        {
            // Enemy has no retreated AI, so when this function is called, no need to save sub data
            var currentSubGameStatusData = _subGameStatusDataQuery.GetSingleton<SubGameStatusData>();
            var targetSubGameStatusData = new SubGameStatusData
            {
                SubGameStatus = SubGameStatus.PlayerCity,
                City = currentSubGameStatusData.City,
            };
            OnSwitchGameStatus?.Invoke(targetSubGameStatusData, currentSubGameStatusData);
        }

        // ----------------- Wait Personalized or Wait Until Battle-----------------//
        public IEnumerator Wait(WaitInfo wai)
        {
            using var query = _em.CreateEntityQuery(typeof(GameTimeConfig));
            var timeConfig = query.GetSingleton<GameTimeConfig>();
            using var query2 = _em.CreateEntityQuery(typeof(GameTimeScale));
            var timeScale = query2
                .GetSingletonRW<GameTimeScale>();
            timeScale.ValueRW.Value = timeConfig.waitTimeScale;
            var waitInfo = _waitInfoQuery
                .GetSingletonRW<WaitInfo>();
            waitInfo.ValueRW = wai;
            OnStartWait?.Invoke();
            InputListener.Instance.DisableAllMaps();
            yield return CheckWaitComplete();
            OnEndWait?.Invoke();
            InputListener.Instance.ReEnableLastEnabledMap();
        }

        #endregion


        // Internal Data
        private EntityManager _em;
        private EntityQuery _subGameStatusDataQuery;
        private EntityQuery _mainGameStatusDataQuery;
        private EntityQuery _waitInfoQuery;

        #region Private Methods

        private Entity GetCity(int cityPrefabId)
        {
            using var query = _em.CreateEntityQuery(typeof(CityAttr), typeof(PrefabId));
            using var entities = query.ToEntityArray(Allocator.Temp);
            using var prefabIds = query.ToComponentDataArray<PrefabId>(Allocator.Temp);
            for (var i = 0; i < prefabIds.Length; i++)
            {
                var id = prefabIds[i];
                if (id.value == cityPrefabId)
                    return entities[i];
            }

            throw new ArgumentException("City prefab id not found, this should never happen");
        }

        private IEnumerator CheckEcsSystemInitComplete()
        {
            while (_mainGameStatusDataQuery.GetSingleton<GameStatusData>().Value != GameStatus.Pause)
            {
                yield return new WaitForSecondsRealtime(CustomCoroutineRunner.Instance.checkInterval);
            }
        }

        private IEnumerator CheckWaitComplete()
        {
            while (_waitInfoQuery.GetSingleton<WaitInfo>().WaitType != WaitType.None)
            {
                yield return new WaitForSecondsRealtime(CustomCoroutineRunner.Instance.checkInterval);
            }
        }

        private IEnumerator CheckNecessaryEntities(SubGameStatusData targetSubGameStatusData,
            SubGameStatusData currentSubGameStatusData)
        {
            if (targetSubGameStatusData.SubGameStatus == SubGameStatus.PlayerCity &&
                currentSubGameStatusData.SubGameStatus != SubGameStatus.None) yield break;
            using var queryGroup = new QueriesGroup(Allocator.Persistent);
            switch (targetSubGameStatusData.SubGameStatus)
            {
                case SubGameStatus.None:
                    break;
                case SubGameStatus.PlayerCity:
                    queryGroup.AddQuery(_em.CreateEntityQuery(typeof(CrystalDef)));
                    break;
                case SubGameStatus.PlayerSiege:
                    queryGroup.AddQuery(_em.CreateEntityQuery(typeof(CrystalDef)));
                    queryGroup.AddQuery(_em.CreateEntityQuery(typeof(UnitAttr), typeof(PlayerTag)));
                    break;
                case SubGameStatus.PlayerDefend:
                    queryGroup.AddQuery(_em.CreateEntityQuery(typeof(CrystalDef)));
                    queryGroup.AddQuery(_em.CreateEntityQuery(typeof(UnitAttr), typeof(AITag)));
                    break;
                case SubGameStatus.Support:
                    queryGroup.AddQuery(_em.CreateEntityQuery(typeof(CrystalDef)));
                    queryGroup.AddQuery(_em.CreateEntityQuery(typeof(UnitAttr), typeof(AITag)));
                    queryGroup.AddQuery(_em.CreateEntityQuery(typeof(UnitAttr), typeof(PlayerTag)));
                    break;
                case SubGameStatus.Encounter:
                    queryGroup.AddQuery(_em.CreateEntityQuery(typeof(UnitAttr), typeof(AITag)));
                    queryGroup.AddQuery(_em.CreateEntityQuery(typeof(UnitAttr), typeof(PlayerTag)));
                    break;
                default:
                    BurstSafe.UnexpectedEnum(targetSubGameStatusData.SubGameStatus);
                    break;
            }

            while (!queryGroup.IsAllNotEmpty())
            {
                yield return new WaitForSecondsRealtime(CustomCoroutineRunner.Instance.checkInterval);
            }
        }

        private IEnumerator WaitForExtraFramesBeforeSwitchGameplay(SubGameStatusData targetSubGameStatusData)
        {
            if (targetSubGameStatusData.SubGameStatus == SubGameStatus.None) yield break;
            var c = 0;
            using var query = _em.CreateEntityQuery(typeof(SubGameplayGeneralAttr));
            var count = query.CalculateEntityCount();
            var waitFrameCount = 0;
            foreach (var config in extraWaitConfigs)
            {
                if (count < config.maxSubGameplayGeneralAttrEntityCount)
                {
                    waitFrameCount = config.waitFrameCount;
                    break;
                }
            }

            while (c < waitFrameCount)
            {
                c += 1;
                yield return null;
            }
        }

        private IEnumerator CheckSaveComplete()
        {
            while (SaveLoadController.Instance.IsSaving)
            {
                yield return new WaitForSecondsRealtime(CustomCoroutineRunner.Instance.checkInterval);
            }
        }

        #endregion


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
            _subGameStatusDataQuery = _em.CreateEntityQuery(typeof(SubGameStatusData));
            _mainGameStatusDataQuery = _em.CreateEntityQuery(typeof(GameStatusData));
            _waitInfoQuery = _em.CreateEntityQuery(typeof(WaitInfo));
        }

        private void OnDestroy()
        {
            if (_subGameStatusDataQuery != default)
                _subGameStatusDataQuery.Dispose();

            if (_mainGameStatusDataQuery != default)
                _mainGameStatusDataQuery.Dispose();
            if (_waitInfoQuery != default)
                _waitInfoQuery.Dispose();
        }

        #endregion
    }
}