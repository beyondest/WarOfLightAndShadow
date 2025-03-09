using System;
using System.Linq;
using UnityEngine;
using SparFlame.Utils;
namespace SparFlame.BootStrapper
{
    public class SceneController : MonoBehaviour
    {
        public static SceneController instance;
        [SerializeField] private SceneGroup[] sceneGroups;
        public event Action<SceneGroup> OnSceneGroupLoaded;
        public event Action<SceneGroup> OnSceneGroupUnloaded;
        
        private SceneGroupManager _sceneGroupManager;


        private void Awake()
        {
            if (instance == null)
            {
                instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else Destroy(gameObject);
            _sceneGroupManager = new SceneGroupManager();
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