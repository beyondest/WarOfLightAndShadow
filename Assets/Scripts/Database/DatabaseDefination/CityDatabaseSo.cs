using System;
using System.Collections.Generic;
using GamePlaySystem.Functionality.MainGameplay.City;
using Sirenix.OdinInspector;
using SparFlame.Components.General;
using SparFlame.Core.Utils;
using UnityEditor;
using UnityEngine;

namespace SparFlame.Database
{
    [CreateAssetMenu(fileName = "CityDatabase", menuName = "GameData/CityDatabase", order = 0)]
    public class CityDatabaseSo : ScriptableObject
    {
        
        public int idStart ;
        [ReadOnly]
        public int idEnd;
        [TableList]
        public List<CityDataItem> items;
        
        [Button("Reassign all id and ReBake")]
        private void ReassignAllIDs()
        {
            if (items.Count > 30)
                throw new ArgumentException(
                    $"Max count is 30, item count is {items.Count}, please split the database.");
            idEnd = idStart + items.Count - 1;
            for (int i = 0; i < items.Count; i++)
            {
                items[i].id = idStart + i;

                GameObject go = items[i].prefab;
#if UNITY_EDITOR
                MonoBehaviour authoring = null;
                authoring = go.GetComponent<CityAuthoring>();
                if (authoring)
                {
                    var so = new SerializedObject(authoring);
                    so.FindProperty("globalIdx").intValue = items[i].id;
                    so.ApplyModifiedProperties();

                    EditorUtility.SetDirty(go);
                    UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(go.scene);
                }
                
#endif
            }
        }
    }

    [Serializable]
    public class CityDataItem
    {
        [VerticalGroup("General")]
        public int id;
        [VerticalGroup("General")]
        public string gameplayName;
        
        [VerticalGroup("General"), AssetsOnly, PreviewField]
        public GameObject prefab;
        
        [TextArea(3,10), VerticalGroup("Description"), HideLabel]
        public string description;

        [VerticalGroup("Gameplay"), HideLabel] public FactionTag faction;

        [VerticalGroup("SceneGroup"), HideLabel]
        public SceneGroup sceneGroup;
    }
}