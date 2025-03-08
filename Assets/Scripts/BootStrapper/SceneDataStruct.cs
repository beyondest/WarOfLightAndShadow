using System;
using System.Collections.Generic;
using System.Linq;
using Eflatun.SceneReference;

namespace SparFlame.BootStrapper
{
    public enum SceneType
    {
        OutUI,
        InUI,
        /// <summary>
        /// AlwaysLoaded scene will not be unloaded using the scene group manager method
        /// </summary>
        AlwaysLoaded,
        Normal,
        /// <summary>
        /// FirstActive scene will be set to active in this group when they are loaded
        /// </summary>
        FirstActive,
        
        
        None
    }

    [Serializable]
    public class SceneData
    {
        public SceneReference SceneRef;
        public string Name => SceneRef.Name;
        public SceneType SceneType ;
    }
    
    
    [Serializable]
    public class SceneGroup
    {
        public string groupName = "New Scene Group";
        public List<SceneData> Scenes;
        public SceneReference FindSceneRefByType(SceneType sceneType)
        {
            return Scenes.FirstOrDefault(sceneData => sceneData.SceneType == sceneType)?.SceneRef;
        }
        public SceneReference FindSceneRefByBuildIndex(int buildIndex)
        {
            return Scenes.FirstOrDefault(sceneData => sceneData.SceneRef.BuildIndex == buildIndex)?.SceneRef;
        }
        public bool FindSceneTypeByBuildIndex(int buildIndex, out SceneType sceneType)
        {
            var sceneData = Scenes.FirstOrDefault(sceneData => sceneData.SceneRef.BuildIndex == buildIndex);
            if (sceneData != null)
            {
                sceneType = sceneData.SceneType;
                return true;
            }
            sceneType = SceneType.None;
            return false;
        }
        public bool FindSceneTypeByName(string name, out SceneType sceneType)
        {
            var sceneData = Scenes.FirstOrDefault(sceneData => sceneData.SceneRef.Name == name);
            if (sceneData != null)
            {
                sceneType = sceneData.SceneType;
                return true;
            }
            sceneType = SceneType.None;
            return false;
        }
    }

}