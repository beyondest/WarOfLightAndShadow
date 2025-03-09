using System;
using System.Collections.Generic;
using System.Linq;
using Eflatun.SceneReference;

namespace SparFlame.BootStrapper
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
        public string Name => sceneRef.Name;
         public SceneType sceneType ;
    }
    
    
    [Serializable]
    public class SceneGroup
    {
        public string groupName = "New Scene Group";
        public List<SceneData> scenes;
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
        public bool FindSceneTypeByName(string name, out SceneType sceneType)
        {
            var sceneData = scenes.FirstOrDefault(sceneData => sceneData.sceneRef.Name == name);
            if (sceneData != null)
            {
                sceneType = sceneData.sceneType;
                return true;
            }
            sceneType = SceneType.None;
            return false;
        }
    }

}