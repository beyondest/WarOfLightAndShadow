using System;
using System.Collections;
using System.Collections.Generic;
using SparFlame.Components.General;
using SparFlame.Components.MainGameplay;
using SparFlame.Core.Utils;
using SparFlame.Database;
using UnityEngine;
using Unity.Entities;
using Unity.Scenes;

namespace SparFlame.Systems.General.BasicControl
{
    public enum SceneGroupType
    {
        Init,
        MainWorld,
        SubWorld,
        CityEnv, // Contains city terrain data
        BattleField, // Contains battlefield terrain data
        CityInvadeFight, // Contains enemy buildings and units
        CitySupportFight, // Contains ally buildings and units
        CurrentLoadingSubGameplaySceneGroup, 
        RetreatPortalScene, // Contains functional buildings in battle status, like retreat portal
    }

    public class SceneController : MonoBehaviour, IResourceManager
    {
        [SerializeField] private SceneGroup lightInitSceneGroup;
        [SerializeField] private SceneGroup darkInitSceneGroup;
        [SerializeField] private SceneGroup mainWorldSceneGroup;
        [SerializeField] private SceneGroup subWorldSceneGroup;
        [SerializeField] private SceneGroup debugInitSceneGroup;
        [SerializeField] private float checkInterval = 0.1f;

        public class Operation : ResourceOperation
        {
            public override float Progress => _sceneController.InitProgress;

            public Operation(IEnumerator routine) : base(routine)
            {
                _sceneController = Instance;
            }
            private readonly SceneController _sceneController;
        }

        public static SceneController Instance;
        public event Action OnEcsStartLoadScene;

        public bool IsInitialized => _normalSceneLoaded && _subsceneLoaded;
        public float InitProgress => IsInitialized ? 1f : (_normalSceneLoadProgress + _subsceneLoadProgress) / 2f;

        public Operation LoadSceneGroupAsync(List<SceneGroupType> sceneGroupTypes, int cityId ,
            bool ifEnterSubGameplay , EcoType ecoType = EcoType.Unknown) =>
            new(SelfLoadSceneGroupAsync(sceneGroupTypes, cityId, ifEnterSubGameplay, ecoType));

        public Operation UnloadSceneGroupAsync(List<SceneGroupType> sceneGroupTypes) =>
            new(SelfUnloadSceneGroup(sceneGroupTypes));

        public void LoadDebugInitSubscene()
        {
            StartCoroutine(_normalSceneLoader.LoadSceneGroupAsync(debugInitSceneGroup, null));
            _currentLoadingSubGameplaySceneGroup.AddSceneGroup(debugInitSceneGroup);
        }
        // After select faction
        public void LoadResources()
        {
            var sceneGroupTypes = new List<SceneGroupType>();

            if (_ifNewSaving)
            {
                sceneGroupTypes.Add(SceneGroupType.Init);
                sceneGroupTypes.Add(SceneGroupType.MainWorld);
            }
            else
            {
                if (_cityPrefabId == 0) sceneGroupTypes.Add(SceneGroupType.MainWorld);
                else
                {
                    sceneGroupTypes.Add(SceneGroupType.SubWorld);
                    sceneGroupTypes.Add(SceneGroupType.CityEnv);
                }
            }

            StartLoadSceneGroup(sceneGroupTypes, _cityPrefabId,_cityPrefabId != 0);
        }

        // Return to main menu
        public void UnloadResources()
        {
            // Other scene groups will be unloaded manually by game controller
            var sceneGroupTypes = new List<SceneGroupType>
            {
                SceneGroupType.Init
            };
            StartCoroutine(SelfUnloadSceneGroup(sceneGroupTypes));
        }

        internal void SetSubsceneLoadingProgress(float progress)
        {
            _subsceneLoadProgress = progress;
            if (progress > 0.99f)
            {
                _subsceneLoaded = true;
            }
        }


        private IEnumerator SelfLoadSceneGroupAsync(List<SceneGroupType> sceneGroupTypes, int cityId,
            bool ifEnterSubGameplay , EcoType ecoType = EcoType.Unknown)
        {
            StartLoadSceneGroup(sceneGroupTypes, cityId, ifEnterSubGameplay, ecoType);
            yield return CheckSceneLoading();
        }

        /// <summary>
        /// Ecs subscene will not be full loaded when this method return
        /// </summary>
        /// <param name="sceneGroupTypes"></param>
        /// <param name="cityId"></param>
        /// <param name="ifEnterSubGameplay"></param>
        /// <param name="ecoType"></param>
        /// <returns></returns>
        private void StartLoadSceneGroup(List<SceneGroupType> sceneGroupTypes, int cityId ,
            bool ifEnterSubGameplay , EcoType ecoType = EcoType.Unknown)
        {
            // Reset Loading Progress
            _subsceneLoaded = false;
            _normalSceneLoaded = false;
            _normalSceneLoadProgress = 0f;
            _subsceneLoadProgress = 0f;

            var sceneGroup = GetSceneGroup(sceneGroupTypes, ecoType, cityId);
            // Only record subGameplay scene group
            if (ifEnterSubGameplay)
                _currentLoadingSubGameplaySceneGroup = sceneGroup;

            StartCoroutine(_normalSceneLoader.LoadSceneGroupAsync(sceneGroup, _loading,
                onSceneGroupLoaded: _onNormalSceneLoaded));
            OnEcsStartLoadScene?.Invoke();

            foreach (var subsceneData in sceneGroup.subscenes)
            {
                SceneSystem.LoadSceneAsync(World.DefaultGameObjectInjectionWorld.Unmanaged,
                    subsceneData.sceneRef.SceneGUID);
            }
        }

        private IEnumerator SelfUnloadSceneGroup(List<SceneGroupType> sceneGroupTypes)
        {
            var sceneGroup = GetSceneGroup(sceneGroupTypes, EcoType.Unknown);
            yield return _normalSceneLoader.UnloadSceneGroupAsync(sceneGroup);
            foreach (var subsceneData in sceneGroup.subscenes)
            {
                SceneSystem.UnloadScene(World.DefaultGameObjectInjectionWorld.Unmanaged,
                    subsceneData.sceneRef.SceneGUID);
            }
        }
        [Obsolete]
        public void UnloadSceneGroupOld(List<SceneGroupType> sceneGroupTypes)
        {
            var sceneGroup = GetSceneGroup(sceneGroupTypes, EcoType.Unknown);
            StartCoroutine(_normalSceneLoader.UnloadSceneGroupAsync(sceneGroup));
            foreach (var subsceneData in sceneGroup.subscenes)
            {
                SceneSystem.UnloadScene(World.DefaultGameObjectInjectionWorld.Unmanaged,
                    subsceneData.sceneRef.SceneGUID);
            }
        }

        public void SetSceneLoadInitData(FactionTag faction, bool ifNewGame, int cityPrefabId)
        {
            _playerFaction = faction;
            _ifNewSaving = ifNewGame;
            _cityPrefabId = cityPrefabId;
        }

        // Cache
        private bool _normalSceneLoaded;
        private float _normalSceneLoadProgress;
        private bool _subsceneLoaded;
        private float _subsceneLoadProgress;
        private readonly ResourceLoadingUtils.LoadingProgress _loading = new();

        // Internal data
        private SceneGroup _currentLoadingSubGameplaySceneGroup;
        private bool _ifSubGameplaySceneLoaded;
        private readonly NormalSceneLoader _normalSceneLoader = new();
        private Action<SceneGroup> _onNormalSceneLoaded;
        private FactionTag _playerFaction;
        private bool _ifNewSaving;
        private int _cityPrefabId;

        private void Awake()
        {
            if (!Instance)
                Instance = this;
            else
                Destroy(this);
            _loading.OnProgressChanged += (f => _normalSceneLoadProgress = f);
            _onNormalSceneLoaded += _ =>
            {
                _normalSceneLoaded = true;
                _normalSceneLoadProgress = 1f;
            };
        }

        private void Start()
        {
            GeneralResourceManager.Instance.Register(this);
        }

        private IEnumerator CheckSceneLoading()
        {
            while (!IsInitialized)
            {
                yield return new WaitForSecondsRealtime(checkInterval);
            }
        }

        private SceneGroup GetSceneGroup(List<SceneGroupType> sceneGroupTypes, EcoType ecoType, int cityId = 0)
        {
            var sceneGroup = new SceneGroup();
            var cityItem = cityId <= 0 ? new CityDataItem() : DatabaseUtils.GetCityDataItemById(cityId);
            foreach (var sceneGroupType in sceneGroupTypes)
            {
                switch (sceneGroupType)
                {
                    case SceneGroupType.Init:
                        sceneGroup.AddSceneGroup(_playerFaction == FactionTag.Light
                            ? lightInitSceneGroup
                            : darkInitSceneGroup);
                        break;
                    case SceneGroupType.MainWorld:
                        sceneGroup.AddSceneGroup(mainWorldSceneGroup);
                        break;
                    case SceneGroupType.SubWorld:
                        sceneGroup.AddSceneGroup(subWorldSceneGroup);
                        break;
                    case SceneGroupType.CityEnv:
                        sceneGroup.AddSceneGroup(cityItem.envSceneGroup);
                        break;
                    case SceneGroupType.BattleField:
                        var item = DatabaseManager.EcoDatabaseSo.GetEcoDataItemByEcoType(ecoType);
                        sceneGroup.AddSceneGroup(item.ecoEnvSceneGroup);
                        break;
                    case SceneGroupType.CityInvadeFight:
                        sceneGroup.AddSceneGroup(_playerFaction == FactionTag.Light
                            ? cityItem.lightInvadeSceneGroup
                            : cityItem.darkInvadeSceneGroup);
                        break;
                    case SceneGroupType.CitySupportFight:
                        sceneGroup.AddSceneGroup(_playerFaction == FactionTag.Light
                            ? cityItem.lightSupportSceneGroup
                            : cityItem.darkSupportSceneGroup);
                        break;
                    case SceneGroupType.CurrentLoadingSubGameplaySceneGroup:
                        sceneGroup.AddSceneGroup(_currentLoadingSubGameplaySceneGroup);
                        break;
                    case SceneGroupType.RetreatPortalScene:
                        sceneGroup.AddSceneGroup(cityItem.retreatPortalSceneGroup);
                        break;
                    default:
                        BurstSafe.UnexpectedEnum(sceneGroupType);
                        break;
                }
            }

            return sceneGroup;
        }
    }
}