using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Scenes;
using UnityEngine;
using UnityEngine.SceneManagement;
using SceneReference = Eflatun.SceneReference.SceneReference;

namespace SparFlame.Core.Utils
{
    
    
    public enum SceneType
    {
        None = 0x0,
        OutUI = 0x1,
        InUI = 0x2,

        /// <summary>
        /// AlwaysLoaded scene will not be unloaded using the scene group manager method
        /// </summary>
        AlwaysLoaded = 0x3,
        Normal = 0x4,

        /// <summary>
        /// FirstActive scene will be set to active in this group when they are loaded
        /// </summary>
        FirstActive = 0x5,
    }

    [Serializable]
    public class SceneData
    {
        public SceneReference sceneRef;
        public SceneType sceneType;
    }

    [Serializable]
    public struct SubsceneData
    {
        public SubScene sceneRef;
    }

    [Serializable]
    public class SceneGroup
    {
        
        public List<SceneData> scenes = new();
        public List<SubsceneData> subscenes = new();

        public void AddSceneGroup(SceneGroup group)
        {
            if(group == null)return;
            if (group.scenes is { Count: > 0 })
            {
                scenes.AddRange(group.scenes);
            }

            if (group.subscenes is { Count: > 0 })
            {
                subscenes.AddRange(group.subscenes);
            }
        }
       
        public SceneReference FindSceneRefByType(SceneType sceneType)
        {
            return scenes.FirstOrDefault(sceneData => sceneData.sceneType == sceneType)?.sceneRef;
        }

        public SceneReference FindSceneRefByBuildIndex(int buildIndex)
        {
            return scenes.FirstOrDefault(sceneData => sceneData.sceneRef.BuildIndex == buildIndex)?.sceneRef;
        }

        public bool FindSceneTypeByBuildIndex(int buildIndex, out SceneType sceneType)
        {
            var sceneData = scenes.FirstOrDefault(sceneData => sceneData.sceneRef.BuildIndex == buildIndex);
            if (sceneData != null)
            {
                sceneType = sceneData.sceneType;
                return true;
            }
        
            sceneType = SceneType.None;
            return false;
        }

    
    }
    public class NormalSceneLoader
    {

        private readonly List<SceneType> _alwaysLoadedSceneTypes = new List<SceneType> { SceneType.AlwaysLoaded };


        /// <summary>
        /// Load a group of scene asynchronously
        /// </summary>
        /// <param name="toLoadSceneGroup"></param>
        /// <param name="loadingProgress">This is used for getting the group average loading progress, if null, will not use</param>
        /// <param name="reloadDupScenes"></param>
        /// <param name="onSceneGroupLoaded"></param>
        /// <returns></returns>
        public IEnumerator LoadSceneGroupAsync(SceneGroup toLoadSceneGroup, ResourceLoadingUtils.LoadingProgress loadingProgress,
            bool reloadDupScenes = false, Action<SceneGroup> onSceneGroupLoaded = null)
        {
            // Find scenes that already loaded or is loading/unloading
            var loadedSceneBuildIdx = new List<int>();
            for (var i = 0; i < SceneManager.sceneCount; i++)
            {
                loadedSceneBuildIdx.Add(SceneManager.GetSceneAt(i).buildIndex);
            }

            // Load the scenes that are not loaded already, if you set reloadDupScenes to false
            var operationsGroup = new ResourceLoadingUtils.AsyncOperationGroup(toLoadSceneGroup.scenes.Count);
            foreach (var sceneData in toLoadSceneGroup.scenes)
            {
                if (!reloadDupScenes && loadedSceneBuildIdx.Contains(sceneData.sceneRef.BuildIndex)) continue;
                var operation = SceneManager.LoadSceneAsync(sceneData.sceneRef.BuildIndex, LoadSceneMode.Additive);
                operationsGroup.Add(operation);
                // Tell listeners that Scene(Name) begins to load
            }

            // Begin loading and report
            while (!operationsGroup.IsDone)
            {
                loadingProgress?.Report(operationsGroup.AverageProgress);
                yield return null;
            }
            
            
            // This part is only executed when while is completed, but it will not block the main thread
            // Active the FirstActive Scene if there is one
            var firstActiveSceneRef = toLoadSceneGroup.FindSceneRefByType(SceneType.FirstActive);
            if (firstActiveSceneRef != null)
            {
                var shouldActiveSceneBuildIndex = firstActiveSceneRef.BuildIndex;
                var shouldActiveScene = SceneManager.GetSceneByBuildIndex(shouldActiveSceneBuildIndex);
                if (shouldActiveScene.IsValid())
                {
                    SceneManager.SetActiveScene(shouldActiveScene);
                }
            }
            onSceneGroupLoaded?.Invoke(toLoadSceneGroup);

        }

        public IEnumerator UnloadSceneGroupAsync(SceneGroup unloadSceneGroup, Action<SceneGroup> onSceneGroupUnloaded = null)
        {
            // Get loaded scene
            var loadedSceneBuildIdx = new List<int>();
            for (var i = 0; i < SceneManager.sceneCount; i++)
            {
                loadedSceneBuildIdx.Add(SceneManager.GetSceneAt(i).buildIndex);
            }

            var operationGroup = new ResourceLoadingUtils.AsyncOperationGroup(unloadSceneGroup.scenes.Count);

            foreach (var sceneData in unloadSceneGroup.scenes)
            {
                // If not loaded, continue
                if (!loadedSceneBuildIdx.Contains(sceneData.sceneRef.BuildIndex)) continue;
                // If always loaded, continue
                if (_alwaysLoadedSceneTypes.Contains(sceneData.sceneType)) continue;

                var operation = SceneManager.UnloadSceneAsync(sceneData.sceneRef.BuildIndex);
                operationGroup.Add(operation);
            }

            while (!operationGroup.IsDone)
            {
                yield return null;
            }

            // This will release all the assets which are not being used in the loading scenes hierarchy
            //await Resources.UnloadUnusedAssets();
            
            onSceneGroupUnloaded?.Invoke(unloadSceneGroup);

        }
    }
}