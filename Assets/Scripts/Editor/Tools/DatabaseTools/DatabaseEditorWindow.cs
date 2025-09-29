using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using SparFlame.Database;
using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using SparFlame.Database.DatabaseDefinition;

namespace Editor
{
    public class DatabaseEditorWindow : OdinEditorWindow
    {
        [Unity.Collections.ReadOnly, LabelText("Building Databases"), ShowInInspector]
        public List<BuildingDatabaseSo> buildingDatabases = new();
 
         [Unity.Collections.ReadOnly, LabelText("Unit Databases"), ShowInInspector]
        public List<UnitDatabaseSo> unitDatabases = new();

        [Unity.Collections.ReadOnly, LabelText("Resource Databases"), ShowInInspector]
        public List<ResourceDatabaseSo> resourceDatabases = new();


      
        [Unity.Collections.ReadOnly, LabelText("Env Databases"), ShowInInspector]
        public List<EnvDatabaseSo> envDatabases = new();

        
        [Unity.Collections.ReadOnly, LabelText("Env Type To Spawn Tiles Databases"), ShowInInspector]
        public List<EnvTypeSpawnDatabaseSo> envTypeSpawnDatabases = new();
        
        [Unity.Collections.ReadOnly, LabelText("VFX Databases"), ShowInInspector]
        public List<VFXDatabaseSo> vfxDatabases = new();
        
        
        [Unity.Collections.ReadOnly, LabelText("Buff Databases"), ShowInInspector]
        public List<BuffDatabaseSo> buffDatabases = new();
        
        [Unity.Collections.ReadOnly, LabelText("Hint Databases"), ShowInInspector]
        public List<HintDatabaseSo> hintDatabases = new();
        
        [Unity.Collections.ReadOnly, LabelText("City Databases"), ShowInInspector]
        public List<CityDatabaseSo> cityDatabases = new();

        [Unity.Collections.ReadOnly, LabelText("Eco Databases"), ShowInInspector]
        public List<EcoDatabaseSo> ecoDatabaseSos = new();
        
        [Unity.Collections.ReadOnly, LabelText("ArmyGroup Databases"), ShowInInspector]
        public List<ArmyGroupDatabaseSo> armyGroupDatabases = new();
        
        [PropertySpace(10)]
        [Button(ButtonSizes.Large), GUIColor(0.4f, 1f, 0.4f)]
        private void RefreshDatabases()
        {
            buildingDatabases = FindAllAssets<BuildingDatabaseSo>("Building Database");
            unitDatabases = FindAllAssets<UnitDatabaseSo>("Unit Database");
            resourceDatabases = FindAllAssets<ResourceDatabaseSo>("Resource Database");
            envDatabases = FindAllAssets<EnvDatabaseSo>("Env Database");
            envTypeSpawnDatabases = FindAllAssets<EnvTypeSpawnDatabaseSo>("Env Type Spawn Database");
            vfxDatabases = FindAllAssets<VFXDatabaseSo>("VFX Database");
            buffDatabases = FindAllAssets<BuffDatabaseSo>("Buff Database");
            hintDatabases = FindAllAssets<HintDatabaseSo>("Hint Database");
            cityDatabases = FindAllAssets<CityDatabaseSo>("City Database");
            ecoDatabaseSos = FindAllAssets<EcoDatabaseSo>("Eco Database");
            armyGroupDatabases = FindAllAssets<ArmyGroupDatabaseSo>("ArmyGroup Database");
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
                if (asset)
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
