using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Scenes;
using UnityEngine;
using SceneReference = Eflatun.SceneReference.SceneReference;

namespace SparFlame.GamePlaySystem.General
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
        [SerializeField]
        private string groupName;
        [NonSerialized]
        public int NameHash;
        public List<SceneData> scenes;
        public List<SubsceneData> subscenes;

        public SceneGroup(string groupName = "NewSceneGroup")
        {
            this.groupName = groupName;
            NameHash = groupName.GetHashCode();
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
}