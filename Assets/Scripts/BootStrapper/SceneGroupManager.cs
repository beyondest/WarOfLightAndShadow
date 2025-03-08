using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Eflatun.SceneReference;
using SparFlame.Utils;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SparFlame.BootStrapper
{
    public class SceneGroupManager
    {
        public event Action<SceneReference> OnSceneLoaded;
        public event Action<SceneReference> OnSceneUnloaded;

        private readonly int _awaitInterval;
        private readonly List<SceneType> _alwaysLoadedSceneTypes = new List<SceneType> { SceneType.AlwaysLoaded };

        public SceneGroupManager(int awaitInterval = 100)
        {
            _awaitInterval = awaitInterval;
        }

        public async Task LoadSceneGroupAsync(SceneGroup toLoadSceneGroup, IProgress<float> loadingProgress,
            bool reloadDupScenes = false)
        {
            // Find scenes that already loaded or is loading/unloading
            var loadedSceneBuildIdx = new List<int>();
            for (var i = 0; i < SceneManager.sceneCount; i++)
            {
                loadedSceneBuildIdx.Add(SceneManager.GetSceneAt(i).buildIndex);
            }

            // Load the scenes that are not loaded already, if you set reloadDupScenes to false
            var operationsGroup = new AsyncOperationGroup(toLoadSceneGroup.Scenes.Count);
            foreach (var sceneData in toLoadSceneGroup.Scenes)
            {
                if (!reloadDupScenes && loadedSceneBuildIdx.Contains(sceneData.SceneRef.BuildIndex)) continue;
                var operation = SceneManager.LoadSceneAsync(sceneData.SceneRef.BuildIndex, LoadSceneMode.Additive);
                operationsGroup.Add(operation);
                // Tell listeners that Scene(Name) begins to load
                OnSceneLoaded?.Invoke(sceneData.SceneRef);
            }

            try
            {
                // Begin loading and report
                while (!operationsGroup.IsDone)
                {
                    loadingProgress.Report(operationsGroup.AverageProgress);
                    await Task.Delay(_awaitInterval);
                }
            }
            catch (Exception e)
            {
                Debug.Log($"Loading Scene Group failed: {e}");
                throw;
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
        }

        public async Task UnloadSceneGroupAsync(SceneGroup unloadSceneGroup)
        {
            // Get loaded scene
            var loadedSceneBuildIdx = new List<int>();
            for (var i = 0; i < SceneManager.sceneCount; i++)
            {
                loadedSceneBuildIdx.Add(SceneManager.GetSceneAt(i).buildIndex);
            }

            var operationGroup = new AsyncOperationGroup(unloadSceneGroup.Scenes.Count);

            foreach (var sceneData in unloadSceneGroup.Scenes)
            {
                // If not loaded, continue
                if (!loadedSceneBuildIdx.Contains(sceneData.SceneRef.BuildIndex)) continue;
                // If always loaded, continue
                if (_alwaysLoadedSceneTypes.Contains(sceneData.SceneType)) continue;

                var operation = SceneManager.UnloadSceneAsync(sceneData.SceneRef.BuildIndex);
                operationGroup.Add(operation);
                OnSceneUnloaded?.Invoke(sceneData.SceneRef);
            }

            try
            {
                while (!operationGroup.IsDone)
                {
                    await Task.Delay(_awaitInterval);
                }
            }
            catch (Exception e)
            {
                Debug.Log($"Unloading Scene Group failed: {e}");
                throw;
            }

            // This will release all the assets which are not being used in the loading scenes hierarchy
            //await Resources.UnloadUnusedAssets();
        }
    }
}