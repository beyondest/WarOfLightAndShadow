using System;
using System.Linq;
using SparFlame.GamePlaySystem.General;
using UnityEngine;
using SparFlame.Utils;
using Unity.Entities;
using Unity.Scenes;

namespace SparFlame.BootStrapper
{
    public class SceneController : MonoBehaviour, CustomDs.IResourceManager
    {


        [SerializeField] private SubScene lightSubscene ;
        [SerializeField] private SubScene darkSubscene ;

        [SerializeField] private string gamingGroupName = "GamingGroup";
        [SerializeField] private SceneGroup[] sceneGroups;
        
        
        
        // Interface
        public static SceneController Instance;
        public Action EcsStartLoadScene;
        public bool IsInitialized => _normalSceneLoaded && _subsceneLoaded;
        public float InitProgress => IsInitialized ? 1f : (_normalSceneLoadProgress + _subsceneLoadProgress) /2f;
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
            LoadSceneGroup(gamingGroupName,_loading);

        }

        public void UnloadResources()
        {
            UnloadSceneGroup(gamingGroupName);
            _subsceneLoaded = false;
            _normalSceneLoaded = false;
            _normalSceneLoadProgress = 0f;
            _subsceneLoadProgress = 0f;
        }
        
        private bool _normalSceneLoaded;
        private float _normalSceneLoadProgress;
        private bool _subsceneLoaded;
        private float _subsceneLoadProgress;
        public FactionTag _playerFaction;

      

        public event Action<SceneGroup> OnSceneGroupLoaded;
        public event Action<SceneGroup> OnSceneGroupUnloaded;
        
        // Internal data
        private readonly NormalSceneLoader _normalSceneLoader = new();
        private readonly LoadingProgress _loading = new();

        private void Awake()
        {
            if(Instance == null)
                Instance = this;
            else
                Destroy(this);
            _loading.ProgressChanged += (f => _normalSceneLoadProgress = f);
            OnSceneGroupLoaded += _ => _normalSceneLoaded = true;
            OnSceneGroupUnloaded += _ => _normalSceneLoaded = false;
            
        }

        private void Start()
        {
            GeneralResourceManager.Instance.Register(this);
            // GameController.Instance.OnPlayerChooseFaction += factionTag => _playerFaction = factionTag;
        }

        public void LoadSceneGroup(string sceneGroupName, LoadingProgress progress = null)
        {
            var sceneGroup = sceneGroups.FirstOrDefault(group => group.groupName == sceneGroupName);
            if (sceneGroup == null)
            {
                Debug.LogWarning("Scene group not found: " + sceneGroupName);
                return;
            }
            StartCoroutine(_normalSceneLoader.LoadSceneGroupAsync(sceneGroup, progress,false, OnSceneGroupLoaded));
            EcsStartLoadScene?.Invoke();

            foreach (var subsceneData in sceneGroup.subscenes)
            {
                SceneSystem.LoadSceneAsync(World.DefaultGameObjectInjectionWorld.Unmanaged, subsceneData.sceneRef.SceneGUID);
            }

            SceneSystem.LoadSceneAsync(World.DefaultGameObjectInjectionWorld.Unmanaged,
                _playerFaction == FactionTag.Ally ? lightSubscene.SceneGUID : darkSubscene.SceneGUID);
        }

        public void UnloadSceneGroup(string sceneGroupName)
        {
            var sceneGroup = sceneGroups.FirstOrDefault(group => group.groupName == sceneGroupName);
            if (sceneGroup == null)
            {
                Debug.LogWarning("Scene group not found: " + sceneGroupName);
                return;
            }
            StartCoroutine(_normalSceneLoader.UnloadSceneGroupAsync(sceneGroup, OnSceneGroupUnloaded));
            foreach (var subsceneData in sceneGroup.subscenes)
            {
                SceneSystem.UnloadScene(World.DefaultGameObjectInjectionWorld.Unmanaged, subsceneData.sceneRef.SceneGUID);
            }

            if (_playerFaction == FactionTag.Ally)
            {
                SceneSystem.UnloadScene(World.DefaultGameObjectInjectionWorld.Unmanaged,lightSubscene.SceneGUID);

            }
            else
            {
                SceneSystem.UnloadScene(World.DefaultGameObjectInjectionWorld.Unmanaged,darkSubscene.SceneGUID);

            }
        }
    }

    
}