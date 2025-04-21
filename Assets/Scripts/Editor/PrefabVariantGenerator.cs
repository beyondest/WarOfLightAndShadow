using System;
using UnityEngine;
using UnityEditor;
using System.IO;

namespace Editor
{
    public class PrefabVariantGenerator : EditorWindow
    {
        private string _sourceFolder = "Assets/SourcePrefabs";
        private string _allyTargetFolder = "Assets/AllyPrefabs";
        private string _enemyTargetFolder = "Assets/EnemyPrefabs";
        private string _allyPrefix = "Ally";
        private string _enemyPrefix = "Enemy";

        [MenuItem("Tools/Custom/Generate Prefab Variants")]
        public static void ShowWindow()
        {
            GetWindow<PrefabVariantGenerator>("Prefab Variant Generator");
        }

        private void OnGUI()
        {
            GUILayout.Label("Config", EditorStyles.boldLabel);

            _sourceFolder = EditorGUILayout.TextField("Source Folder (C)", _sourceFolder);
            _allyTargetFolder = EditorGUILayout.TextField("Ally Target Folder (A)", _allyTargetFolder);
            _enemyTargetFolder = EditorGUILayout.TextField("Enemy Target Folder (B)", _enemyTargetFolder);
            _allyPrefix = EditorGUILayout.TextField("Ally Prefix", _allyPrefix);
            _enemyPrefix = EditorGUILayout.TextField("Enemy Prefix", _enemyPrefix);

            if (GUILayout.Button("Begin Generate Variant"))
            {
                GenerateVariants();
            }
        }

        private void GenerateVariants()
        {
            var guids = AssetDatabase.FindAssets("t:Prefab", new[] { _sourceFolder });

            foreach (var guid in guids)
            {
                var sourcePath = AssetDatabase.GUIDToAssetPath(guid);
                var originalPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(sourcePath);

                if (originalPrefab == null) continue;

                // var relativePath = Path.GetDirectoryName(sourcePath)!.Replace(_sourceFolder, "").TrimStart('/');
                
                var relativePath = GetRelativePath(Path.GetDirectoryName(sourcePath), _sourceFolder);

                var prefabName = Path.GetFileNameWithoutExtension(sourcePath);

                var allyPath = Path.Combine(_allyTargetFolder, relativePath);
                var enemyPath = Path.Combine(_enemyTargetFolder, relativePath);
                Directory.CreateDirectory(allyPath);
                Directory.CreateDirectory(enemyPath);

                // 创建 Variant 并保存
                var allyVariantPath = Path.Combine(allyPath, _allyPrefix + prefabName + ".prefab").Replace("\\", "/");
                var enemyVariantPath =
                    Path.Combine(enemyPath, _enemyPrefix + prefabName + ".prefab").Replace("\\", "/");

                CreatePrefabVariant(originalPrefab, allyVariantPath);
                CreatePrefabVariant(originalPrefab, enemyVariantPath);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("Prefab Variants Generating Complete！");
        }

        private static void CreatePrefabVariant(GameObject basePrefab, string variantPath)
        {
            var tempInstance = (GameObject)PrefabUtility.InstantiatePrefab(basePrefab);
            var variant = PrefabUtility.SaveAsPrefabAsset(tempInstance, variantPath);
            DestroyImmediate(tempInstance);
        }

        // private static string GetRelativePath(string fullPath, string rootPath)
        // {
        //     var fullUri = new Uri(fullPath + Path.DirectorySeparatorChar);
        //     var rootUri = new Uri(rootPath + Path.DirectorySeparatorChar);
        //     var relativeUri = rootUri.MakeRelativeUri(fullUri);
        //     return Uri.UnescapeDataString(relativeUri.ToString()).Replace("\\", "/");
        // }

        private static string GetRelativePath(string fullPath, string rootPath)
        {
            var absoluteFullPath = Path.GetFullPath(fullPath);
            var absoluteRootPath = Path.GetFullPath(rootPath);

            var fullUri = new Uri(absoluteFullPath + Path.DirectorySeparatorChar);
            var rootUri = new Uri(absoluteRootPath + Path.DirectorySeparatorChar);
            var relativeUri = rootUri.MakeRelativeUri(fullUri);
            return Uri.UnescapeDataString(relativeUri.ToString()).Replace("\\", "/");
        }

    }
}