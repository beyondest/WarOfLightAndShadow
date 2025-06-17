using System;
using System.Collections.Generic;
using System.Linq;
using Sirenix.OdinInspector;
using SparFlame.GamePlaySystem.General;
using UnityEngine;
using SparFlame.Utils;
using Unity.Entities;
using Unity.Scenes;

namespace SparFlame.BootStrapper
{
    public class SceneController : MonoBehaviour, CustomDs.IResourceManager
    {
        
        [SerializeField] private string initGroupName = "Initialization";
        
        [Header("General Scene Groups")]
        [SerializeField,TableList] private List<SceneGroup> generalSceneGroups;
        
        [Header("City Scene Groups")]
        [SerializeField,TableList,HideLabel] private List<SceneGroup> citySceneGroups;
        
        [Header("Wild Scene Groups  ")]
        [SerializeField,TableList,HideLabel] private List<SceneGroup> wildSceneGroups;

        // Interface
        public static SceneController Instance;
        public Action EcsStartLoadScene;
        public readonly LoadingProgress Loading = new();
        
        
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

        public void LoadResources()
        {
            LoadSceneGroup(initGroupName, _gameInitLoading, LoadSceneGroupType.General);
        }

        public void UnloadResources()
        {
            UnloadSceneGroup(initGroupName, LoadSceneGroupType.General);
            _subsceneLoaded = false;
            _normalSceneLoaded = false;
            _normalSceneLoadProgress = 0f;
            _subsceneLoadProgress = 0f;
        }

        public void LoadCitySceneGroup(int cityId)
        {
            LoadSceneGroup(CityIdToSceneGroupName.Instance.CityIdToSceneGroupNameDict[cityId], Loading,
                LoadSceneGroupType.City);
        }

        public void LoadBattleFieldSceneGroup(BattleFieldType type)
        {
            LoadSceneGroup(type.ToString(), Loading, LoadSceneGroupType.Wild);
        }

        // Cache
        private bool _normalSceneLoaded;
        private float _normalSceneLoadProgress;
        private bool _subsceneLoaded;
        private float _subsceneLoadProgress;

       
   

        // Internal data
        private readonly NormalSceneLoader _normalSceneLoader = new();
        private readonly LoadingProgress _gameInitLoading = new();
        private event Action<SceneGroup> OnSceneGroupLoaded;
        private event Action<SceneGroup> OnSceneGroupUnloaded;

        private void Awake()
        {
            if (!Instance)
                Instance = this;
            else
                Destroy(this);
            _gameInitLoading.ProgressChanged += (f => _normalSceneLoadProgress = f);
            
        }

        private void Start()
        {
            GeneralResourceManager.Instance.Register(this);
            
        }

        private void LoadSceneGroup(string sceneGroupName, LoadingProgress progress, LoadSceneGroupType type)
        {
            var hash = sceneGroupName.GetHashCode();
            var groups = type switch
            {
                LoadSceneGroupType.General => generalSceneGroups,
                LoadSceneGroupType.City => citySceneGroups,
                LoadSceneGroupType.Wild => wildSceneGroups,
                _ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
            };
            var sceneGroup = groups.FirstOrDefault(group => group.NameHash == hash);
            if (sceneGroup == null)
            {
                Debug.LogError("Scene group not found: " + sceneGroupName);
                return;
            }

            StartCoroutine(_normalSceneLoader.LoadSceneGroupAsync(sceneGroup, progress, false, OnSceneGroupLoaded));
            EcsStartLoadScene?.Invoke();

            foreach (var subsceneData in sceneGroup.subscenes)
            {
                SceneSystem.LoadSceneAsync(World.DefaultGameObjectInjectionWorld.Unmanaged,
                    subsceneData.sceneRef.SceneGUID);
            }
        }

        private void UnloadSceneGroup(string sceneGroupName, LoadSceneGroupType type)
        {
            var hash = sceneGroupName.GetHashCode();
            var groups = type switch
            {
                LoadSceneGroupType.General => generalSceneGroups,
                LoadSceneGroupType.City => citySceneGroups,
                LoadSceneGroupType.Wild => wildSceneGroups,
                _ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
            };
            var sceneGroup = groups.FirstOrDefault(group => group.NameHash == hash);
            if (sceneGroup == null)
            {
                Debug.LogWarning("Scene group not found: " + sceneGroupName);
                return;
            }

            StartCoroutine(_normalSceneLoader.UnloadSceneGroupAsync(sceneGroup, OnSceneGroupUnloaded));
            foreach (var subsceneData in sceneGroup.subscenes)
            {
                SceneSystem.UnloadScene(World.DefaultGameObjectInjectionWorld.Unmanaged,
                    subsceneData.sceneRef.SceneGUID);
            }


        }
        private enum LoadSceneGroupType
        {
            General,
            City,
            Wild,
        }
       
    }
  
}