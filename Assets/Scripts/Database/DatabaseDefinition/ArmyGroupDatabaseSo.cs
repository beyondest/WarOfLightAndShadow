using System;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using SparFlame.Components.General;
using SparFlame.Components.MainGameplay;
using SparFlame.Systems.MainGameplay.ArmyGroup;
using UnityEditor;
using UnityEngine;

namespace SparFlame.Database.DatabaseDefinition
{
    [CreateAssetMenu(fileName = "ArmyGroupDatabase", menuName = "GameData/ArmyGroupDatabase", order = 0)]
    public class ArmyGroupDatabaseSo : ScriptableObject,IIDBasedDatabase<ArmyGroupDataItem>
    {
        public int idStart;
        [ReadOnly] public int idEnd;
        [TableList]
        public List<ArmyGroupDataItem> items;


        public ArmyGroupDataItem GetItemById(int id)
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
                authoring = go.GetComponent<ArmyGroupAuthoring>();
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
    public class ArmyGroupDataItem
    {
        [VerticalGroup("General")] public string gameplayName = "New Army Group";
        [VerticalGroup("General")] public int id;
        [VerticalGroup("General"), HideLabel] public FactionTag faction;
        [VerticalGroup("General"), HideLabel] public SubFactionTag subFaction;
        [VerticalGroup("General"), HideLabel] public ArmyGroupIconType iconType;
        
        [VerticalGroup("Enemy AI Config")] public bool isEnemyArmyGroup;
        [ShowIf(nameof(isEnemyArmyGroup))]
        public List<UnitCompositionData> enemyArmyGroupCompositionDatas;

        [VerticalGroup("Gameplay"), AssetsOnly]
        public GameObject prefab;
        [VerticalGroup("Gameplay"),AssetsOnly] 
        public GameObject sightPrefab;

        [VerticalGroup("Gameplay"),Tooltip("This value should be set according to its composition")] public float initSpeed;


    }
    
    
    [Serializable]
    public class UnitCompositionData
    {
        public GameObject unitPrefab;
        public int count;
        public int level;
    }

}