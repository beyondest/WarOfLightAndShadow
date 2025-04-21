using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using SparFlame.Database;
using SparFlame.GamePlaySystem.Building;
using UnityEditor;
using UnityEngine;

namespace Editor
{
    public class DatabaseEditorWindow : OdinEditorWindow
    {
        [Unity.Collections.ReadOnly, LabelText("Building Database"), ShowInInspector]
        public BuildingDatabaseSo buildingDatabase;

        [Unity.Collections.ReadOnly, LabelText("Unit Database"), ShowInInspector]
        public UnitDatabaseSo unitDatabase;

        [Unity.Collections.ReadOnly, LabelText("Resource Database"), ShowInInspector]
        public ResourceDatabaseSo resourceDatabase;

        [PropertySpace(10)]
        [Button(ButtonSizes.Large), GUIColor(0.4f, 1f, 0.4f)]
        private void RefreshDatabases()
        {
            buildingDatabase = FindUniqueAsset<BuildingDatabaseSo>("Building Database");
            unitDatabase = FindUniqueAsset<UnitDatabaseSo>("Unit Database");
            resourceDatabase = FindUniqueAsset<ResourceDatabaseSo>("Resource Database");
        }

        private T FindUniqueAsset<T>(string label) where T : ScriptableObject
        {
            string[] guids = AssetDatabase.FindAssets($"t:{typeof(T).Name}");

            if (guids.Length == 0)
            {
                Debug.LogError($"❌ Not finding {label}（{typeof(T).Name}）！");
                return null;
            }

            if (guids.Length > 1)
            {
                Debug.LogError($"⚠️ Find multiple {label}（{typeof(T).Name}），make sure there is only singleton database in one kind！");
                foreach (var guid in guids)
                {
                    Debug.LogError($"Duplicated databases are {AssetDatabase.GUIDToAssetPath(guid)}");
                }
                return null;
            }

            string path = AssetDatabase.GUIDToAssetPath(guids[0]);
            var asset = AssetDatabase.LoadAssetAtPath<T>(path);
            return asset;
        }

        [MenuItem("Tools/Custom/Database Quick Finder")]
        private static void OpenWindow()
        {
            var window = GetWindow<DatabaseEditorWindow>();
            window.titleContent = new GUIContent("Database Finder");
            window.RefreshDatabases(); 
            window.Show();
        }
    }
}