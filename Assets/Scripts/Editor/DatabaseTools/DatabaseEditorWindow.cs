using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using SparFlame.Database;
using SparFlame.Database.DatabaseDefinition;
using SparFlame.GamePlaySystem.Building;
using UnityEditor;
using UnityEngine;
using System.Collections.Generic;

namespace Editor
{
    public class DatabaseEditorWindow : OdinEditorWindow
    {
        [Unity.Collections.ReadOnly, LabelText("Building Databases"), ShowInInspector]
        public List<BuildingDatabaseSo> buildingDatabases = new List<BuildingDatabaseSo>();

        [Unity.Collections.ReadOnly, LabelText("Unit Databases"), ShowInInspector]
        public List<UnitDatabaseSo> unitDatabases = new List<UnitDatabaseSo>();

        [Unity.Collections.ReadOnly, LabelText("Resource Databases"), ShowInInspector]
        public List<ResourceDatabaseSo> resourceDatabases = new List<ResourceDatabaseSo>();

        [Unity.Collections.ReadOnly, LabelText("Resource Spawn Databases"), ShowInInspector]
        public List<ResourceSpawnDatabaseSo> resourceSpawnDatabases = new List<ResourceSpawnDatabaseSo>();

        [PropertySpace(10)]
        [Button(ButtonSizes.Large), GUIColor(0.4f, 1f, 0.4f)]
        private void RefreshDatabases()
        {
            buildingDatabases = FindAllAssets<BuildingDatabaseSo>("Building Database");
            unitDatabases = FindAllAssets<UnitDatabaseSo>("Unit Database");
            resourceDatabases = FindAllAssets<ResourceDatabaseSo>("Resource Database");
            resourceSpawnDatabases = FindAllAssets<ResourceSpawnDatabaseSo>("Resource Spawn Database");
        }

        private List<T> FindAllAssets<T>(string label) where T : ScriptableObject
        {
            List<T> assets = new List<T>();
            string[] guids = AssetDatabase.FindAssets($"t:{typeof(T).Name}");

            if (guids.Length == 0)
            {
                Debug.LogWarning($"Not finding any {label}（{typeof(T).Name}）！");
                return assets;
            }

            foreach (var guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var asset = AssetDatabase.LoadAssetAtPath<T>(path);
                if (asset != null)
                {
                    assets.Add(asset);
                }
            }

            Debug.Log($"Found {assets.Count} {label}(s).");
            return assets;
        }

        [MenuItem("Tools/Database/Database Quick Finder")]
        private static void OpenWindow()
        {
            var window = GetWindow<DatabaseEditorWindow>();
            window.titleContent = new GUIContent("Database Finder");
            window.RefreshDatabases();
            window.Show();
        }
    }
}
