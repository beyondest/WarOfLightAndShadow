using System;
using System.Linq;
using UnityEngine;
using SparFlame.Utils;
namespace SparFlame.BootStrapper
{
    public class SceneController : MonoBehaviour, CustomDs.IResourceManager
    {
        
                
        [SerializeField] private string gamingGroupName = "GamingGroup";
        [SerializeField] private SceneGroup[] sceneGroups;
        
        // Interface
        public bool IsInitialized { get; private set; }
        public float InitProgress { get; private set; }
        public void LoadResources()
        {
            LoadSceneGroup(gamingGroupName,_loading);
        }

        public void UnloadResources()
        {
            UnloadSceneGroup(gamingGroupName);
            IsInitialized = false;
            InitProgress = 0f;
        }

        public event Action<SceneGroup> OnSceneGroupLoaded;
        public event Action<SceneGroup> OnSceneGroupUnloaded;
        
        // Internal data
        private readonly SceneGroupManager _sceneGroupManager = new();
        private readonly LoadingProgress _loading = new();

        private void Awake()
        {
            _loading.ProgressChanged += (f => InitProgress = f);
            OnSceneGroupLoaded += _ => IsInitialized = true;
            OnSceneGroupUnloaded += _ => IsInitialized = false;
        }

        private void Start()
        {
            GeneralResourceManager.Instance.Register(this);
        }

        public void LoadSceneGroup(string sceneGroupName, LoadingProgress progress = null)
        {
            var sceneGroup = sceneGroups.FirstOrDefault(group => group.groupName == sceneGroupName);
            if (sceneGroup == null)
            {
                Debug.LogWarning("Scene group not found: " + sceneGroupName);
                return;
            }
            StartCoroutine(_sceneGroupManager.LoadSceneGroupAsync(sceneGroup, progress,false, OnSceneGroupLoaded));
        }

        public void UnloadSceneGroup(string sceneGroupName)
        {
            var sceneGroup = sceneGroups.FirstOrDefault(group => group.groupName == sceneGroupName);
            if (sceneGroup == null)
            {
                Debug.LogWarning("Scene group not found: " + sceneGroupName);
                return;
            }
            StartCoroutine(_sceneGroupManager.UnloadSceneGroupAsync(sceneGroup, OnSceneGroupUnloaded));
        }

    }
}