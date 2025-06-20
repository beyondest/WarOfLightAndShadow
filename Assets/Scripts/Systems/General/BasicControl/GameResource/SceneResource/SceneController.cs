using System;
using System.Collections;
using SparFlame.Components.MainGameplay;
using SparFlame.Core.Interfaces;
using SparFlame.Core.Utils;
using SparFlame.Database;
using UnityEngine;
using Unity.Entities;
using Unity.Scenes;

namespace SparFlame.Systems.General.BasicControl
{
    public class SceneController : MonoBehaviour, IResourceManager
    {
        [SerializeField] private SceneGroup initSceneGroup;
        [SerializeField] private SceneGroup mainWorldSceneGroup;
        [SerializeField] private SceneGroup generalSubWorldSceneGroup;
        
        [SerializeField] private float checkInterval = 0.1f;
        // Interface
        public static SceneController Instance;
        public Action EcsStartLoadScene;
        public Action<ResourceLoadingUtils.LoadingProgress> OnSceneSwitchGameplay;

        public bool IsInitialized => _normalSceneLoaded && _subsceneLoaded;
        public float InitProgress => IsInitialized ? 1f : (_normalSceneLoadProgress + _subsceneLoadProgress) / 2f;

        public void SetSubsceneLoadingProgress(float progress)
        {
            _subsceneLoadProgress = progress;
            if (progress > 0.99f)
            {
                _subsceneLoaded = true;
            }
        }

        // After select faction
        public void LoadResources()
        {
            LoadSceneGroup(SceneGroupLoadType.Init);
        }

        // Return to main menu
        public void UnloadResources()
        {
            UnloadSceneGroup(SceneGroupLoadType.Init);
        }

        public void EnterCityGameplay(int cityId)
        {
            OnSceneSwitchGameplay?.Invoke(_totalSceneLoadingProgress);
            LoadSceneGroup(SceneGroupLoadType.City, cityId);
            UnloadSceneGroup(SceneGroupLoadType.MainWorld);
        }

        public void EnterBattleField(BattleFieldType type)
        {
            OnSceneSwitchGameplay?.Invoke(_totalSceneLoadingProgress);
            LoadSceneGroup(SceneGroupLoadType.BattleField, fieldType: type);
            UnloadSceneGroup(SceneGroupLoadType.MainWorld);
        }

        public void ReturnToMainGameplayFromSubGameplay()
        {
            OnSceneSwitchGameplay?.Invoke(_totalSceneLoadingProgress);
            LoadSceneGroup(SceneGroupLoadType.MainWorld);
            // Battlefield is also ok
            UnloadSceneGroup(SceneGroupLoadType.City);
        }

        // Cache
        private bool _normalSceneLoaded;
        private float _normalSceneLoadProgress;
        private bool _subsceneLoaded;
        private float _subsceneLoadProgress;
        private readonly ResourceLoadingUtils.LoadingProgress _loading = new();
        private readonly ResourceLoadingUtils.LoadingProgress _totalSceneLoadingProgress = new();

        // Internal data
        private SceneGroup _currentLoadingSubGameplaySceneGroup;
        private bool _ifSubGameplaySceneLoaded;
        private readonly NormalSceneLoader _normalSceneLoader = new();
        private Action<SceneGroup> _onNormalSceneLoaded;
        
        
        private void Awake()
        {
            if (!Instance)
                Instance = this;
            else
                Destroy(this);
            _loading.ProgressChanged += (f => _normalSceneLoadProgress = f);
            _onNormalSceneLoaded+= _ =>
            {
                _normalSceneLoaded = true;
                _normalSceneLoadProgress = 1f;
            };
        }

        private void Start()
        {
            GeneralResourceManager.Instance.Register(this);
        }

        private IEnumerator CheckTotalLoadingProgress()
        {
            while (!IsInitialized)
            {
                _totalSceneLoadingProgress.Report(InitProgress);
                yield return new WaitForSeconds(checkInterval);
            }
        }

        private void LoadSceneGroup(SceneGroupLoadType sceneGroupLoadType, int cityId = -1,
            BattleFieldType fieldType = BattleFieldType.Forest)
        {
            // Reset Loading Progress
            _subsceneLoaded = false;
            _normalSceneLoaded = false;
            _normalSceneLoadProgress = 0f;
            _subsceneLoadProgress = 0f;

            SceneGroup sceneGroup;
            switch (sceneGroupLoadType)
            {
                case SceneGroupLoadType.Init:
                    sceneGroup = new SceneGroup();
                    sceneGroup.AddSceneGroup(initSceneGroup);
                    sceneGroup.AddSceneGroup(mainWorldSceneGroup);
                    break;
                // This happens when player return to main gameplay from city/wild
                case SceneGroupLoadType.MainWorld:
                    sceneGroup = mainWorldSceneGroup;
                    break;
                // This happens when player enter city gameplay
                case SceneGroupLoadType.City:
                    _ifSubGameplaySceneLoaded = true;
                    sceneGroup = new SceneGroup();
                    var idStart = DatabaseManager.CityDatabaseSo.idStart;
                    sceneGroup.AddSceneGroup(DatabaseManager.CityDatabaseSo.items[idStart + cityId].sceneGroup);
                    sceneGroup.AddSceneGroup(generalSubWorldSceneGroup);
                    _currentLoadingSubGameplaySceneGroup = sceneGroup;
                    break;
                // This happens when player enter wild gameplay
                case SceneGroupLoadType.BattleField:
                    _ifSubGameplaySceneLoaded = true;
                    sceneGroup = new SceneGroup();
                    sceneGroup.AddSceneGroup(generalSubWorldSceneGroup);
                    _currentLoadingSubGameplaySceneGroup = sceneGroup;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(sceneGroupLoadType), sceneGroupLoadType, null);
            }

            StartCoroutine(CheckTotalLoadingProgress());
            StartCoroutine(_normalSceneLoader.LoadSceneGroupAsync(sceneGroup, _loading, onSceneGroupLoaded:_onNormalSceneLoaded));
            EcsStartLoadScene?.Invoke();

            foreach (var subsceneData in sceneGroup.subscenes)
            {
                SceneSystem.LoadSceneAsync(World.DefaultGameObjectInjectionWorld.Unmanaged,
                    subsceneData.sceneRef.SceneGUID);
            }
        }

        private void UnloadSceneGroup(SceneGroupLoadType sceneGroupLoadType)
        {
            SceneGroup sceneGroup;
            switch (sceneGroupLoadType)
            {
                // This happens when player return to main menu
                case SceneGroupLoadType.Init:
                    sceneGroup = new SceneGroup();
                    sceneGroup.AddSceneGroup(initSceneGroup);
                    sceneGroup.AddSceneGroup(mainWorldSceneGroup);
                    if (_ifSubGameplaySceneLoaded)
                    {
                        sceneGroup.AddSceneGroup(_currentLoadingSubGameplaySceneGroup);
                    }

                    break;
                // This happens when player enter sub gameplay
                case SceneGroupLoadType.MainWorld:
                    sceneGroup = mainWorldSceneGroup;
                    break;
                // This happens when player return to main gameplay from city/wild
                case SceneGroupLoadType.City:
                case SceneGroupLoadType.BattleField:
                    _ifSubGameplaySceneLoaded = false;
                    sceneGroup = _currentLoadingSubGameplaySceneGroup;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(sceneGroupLoadType), sceneGroupLoadType, null);
            }

            StartCoroutine(_normalSceneLoader.UnloadSceneGroupAsync(sceneGroup));
            foreach (var subsceneData in sceneGroup.subscenes)
            {
                SceneSystem.UnloadScene(World.DefaultGameObjectInjectionWorld.Unmanaged,
                    subsceneData.sceneRef.SceneGUID);
            }
        }

        private enum SceneGroupLoadType
        {
            Init,
            MainWorld,
            City,
            BattleField,
        }
    }
}