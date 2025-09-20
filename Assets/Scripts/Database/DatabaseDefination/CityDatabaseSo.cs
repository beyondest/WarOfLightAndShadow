using System;
using System.Collections.Generic;
using GamePlaySystem.Functionality.MainGameplay.City;
using Sirenix.OdinInspector;
using SparFlame.Components.General;
using SparFlame.Components.MainGameplay;
using SparFlame.Core.Utils;
using UnityEditor;
using UnityEngine;

namespace SparFlame.Database
{
    [CreateAssetMenu(fileName = "CityDatabase", menuName = "GameData/CityDatabase", order = 0)]
    public class CityDatabaseSo : ScriptableObject
    {
        public int idStart;
        [ReadOnly] public int idEnd;
        [TableList] public List<CityDataItem> items;


        public CityDataItem GetItemById(int id)
        {
            return items[id - idStart];
        }
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
        [VerticalGroup("General")] public int id;
        [VerticalGroup("General")] public string gameplayName;

        [VerticalGroup("General"), AssetsOnly, PreviewField]
        public GameObject prefab;

        [VerticalGroup("General")] public int lightModelIndex;
        [VerticalGroup("General")] public int darkModelIndex;

        [VerticalGroup("General")] public float camMaxCoordinate;
        [VerticalGroup("General")] public float camMinCoordinate;
        
        [TextArea(3, 10), VerticalGroup("General"), HideLabel]
        public string description;

        [VerticalGroup("AI")] public bool isSupportCity;
        [VerticalGroup("AI")] public bool isFocusOnPlayerAtBeginning;
        [VerticalGroup("AI"), HideLabel] public EnemyCityStrategy strategy;
        [VerticalGroup("AI"), HideLabel, LabelText("Defend"), AssetsOnly] public List<EnemyArmyGroupPrefabData> defendArmyGroupPrefabs = new();
        [VerticalGroup("AI"), HideLabel, LabelText("Attack"), AssetsOnly] public List<EnemyArmyGroupPrefabData> attackArmyGroupPrefabs = new();
        [VerticalGroup("AI"), HideLabel,LabelText("CheckCity")] public List<int> checkCityIds = new();
        
        
        [VerticalGroup("Gameplay"), HideLabel] public FactionTag faction;

        [VerticalGroup("Gameplay"), HideLabel] public SubFactionTag subFactionTag;

        [VerticalGroup("Gameplay"), HorizontalGroup("Gameplay/MaxGarrisonArmyGroupCount")]
        public int maxGarrisonArmyCount;

        // Init resources will turn to all 0 when convert to player city
        [VerticalGroup("Gameplay"), HideLabel,LabelText("EnemyInitResources")] public List<ResourceData> initResources; 

        [VerticalGroup("SceneGroup"), HideLabel, LabelText("Env")]
        public SceneGroup envSceneGroup;
        
        [VerticalGroup("SceneGroup"), HideLabel, FoldoutGroup("SceneGroup/Player light"),LabelText("Invade")]
        public SceneGroup lightInvadeSceneGroup;

        [VerticalGroup("SceneGroup"), HideLabel, FoldoutGroup("SceneGroup/Player light"),LabelText("Support")]
        public SceneGroup lightSupportSceneGroup;

        [VerticalGroup("SceneGroup"), HideLabel, FoldoutGroup("SceneGroup/Player dark"),LabelText("Invade")]
        public SceneGroup darkInvadeSceneGroup;

        [VerticalGroup("SceneGroup"), HideLabel, FoldoutGroup("SceneGroup/Player dark"),LabelText("Support")]
        public SceneGroup darkSupportSceneGroup;
       
        
    }

    [Serializable]
    public class EnemyArmyGroupPrefabData
    {
        [AssetsOnly]public GameObject prefab;
        [HideLabel, LabelText("NeedHours")]
        public float conjureTotalHours;
    }
}