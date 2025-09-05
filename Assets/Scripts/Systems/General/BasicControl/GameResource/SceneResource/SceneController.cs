using System;
using System.Collections.Generic;
using SparFlame.Components.General;
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
        [SerializeField] private SceneGroup lightInitSceneGroup;
        [SerializeField] private SceneGroup darkInitSceneGroup;
        [SerializeField] private SceneGroup mainWorldSceneGroup;
        [SerializeField] private SceneGroup subWorldSceneGroup;
        [SerializeField] private SceneGroup battleFieldSceneGroup;


        // Interface
        public static SceneController Instance;
        public Action EcsStartLoadScene;

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
            var sceneGroupTypes = new List<SceneGroupType>
            {
                SceneGroupType.MainWorld
            };
            if(_ifNewSaving) sceneGroupTypes.Add(SceneGroupType.Init);
            LoadSceneGroup(sceneGroupTypes);
        }

        // Return to main menu
        public void UnloadResources()
        {
            var sceneGroupTypes = new List<SceneGroupType>
            {
                SceneGroupType.Init
            };
            UnloadSceneGroup(sceneGroupTypes);
        }
        
        public void LoadSceneGroup(List<SceneGroupType> sceneGroupTypes, int cityId = -1,
            bool ifEnterSubGameplay = false)
        {
            // Reset Loading Progress
            _subsceneLoaded = false;
            _normalSceneLoaded = false;
            _normalSceneLoadProgress = 0f;
            _subsceneLoadProgress = 0f;

            var sceneGroup = GetSceneGroup(sceneGroupTypes, cityId);
            // Only record subGameplay scene group
            if(ifEnterSubGameplay)
                _currentLoadingSubGameplaySceneGroup = sceneGroup;
            
            StartCoroutine(_normalSceneLoader.LoadSceneGroupAsync(sceneGroup, _loading,
                onSceneGroupLoaded: _onNormalSceneLoaded));
            EcsStartLoadScene?.Invoke();

            foreach (var subsceneData in sceneGroup.subscenes)
            {
                SceneSystem.LoadSceneAsync(World.DefaultGameObjectInjectionWorld.Unmanaged,
                    subsceneData.sceneRef.SceneGUID);
            }
        }

        public void UnloadSceneGroup(List<SceneGroupType> sceneGroupTypes)
        {
            var sceneGroup = GetSceneGroup(sceneGroupTypes, -1);
            StartCoroutine(_normalSceneLoader.UnloadSceneGroupAsync(sceneGroup));
            foreach (var subsceneData in sceneGroup.subscenes)
            {
                SceneSystem.UnloadScene(World.DefaultGameObjectInjectionWorld.Unmanaged,
                    subsceneData.sceneRef.SceneGUID);
            }
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

        private void Awake()
        {
            if (!Instance)
                Instance = this;
            else
                Destroy(this);
            _loading.ProgressChanged += (f => _normalSceneLoadProgress = f);
            _onNormalSceneLoaded += _ =>
            {
                _normalSceneLoaded = true;
                _normalSceneLoadProgress = 1f;
            };
        }

        private void Start()
        {
            GeneralResourceManager.Instance.Register(this);
            GameController.Instance.OnPlayerChooseSavingSlot += (_, b) => _ifNewSaving = b;
            GameController.Instance.OnPlayerChooseFaction += factionTag => _playerFaction = factionTag;
        }


      


        private SceneGroup GetSceneGroup(List<SceneGroupType> sceneGroupTypes, int cityId)
        {
            var sceneGroup = new SceneGroup();
            var cityItem = cityId < 0 ? new CityDataItem() : DatabaseUtils.GetCityDataItemById(cityId);
            foreach (var sceneGroupType in sceneGroupTypes)
            {
                switch (sceneGroupType)
                {
                    case SceneGroupType.Init:
                        sceneGroup.AddSceneGroup(_playerFaction == FactionTag.Light ? lightInitSceneGroup : darkInitSceneGroup);
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
                        sceneGroup.AddSceneGroup(battleFieldSceneGroup);
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
                    default:
                        BurstSafe.UnexpectedEnum(sceneGroupType);
                        break;
                }
            }

            return sceneGroup;
        }


      
    }
    
    public enum SceneGroupType
    {
        Init,
        MainWorld,
        SubWorld,
        CityEnv,
        BattleField,
        CityInvadeFight,
        CitySupportFight,
        CurrentLoadingSubGameplaySceneGroup,
    }
}