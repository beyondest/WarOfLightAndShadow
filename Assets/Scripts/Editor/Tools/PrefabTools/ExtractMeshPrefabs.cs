using UnityEngine;
using UnityEditor;
using System.IO;

namespace Editor
{
    public class DetailMeshExtractor : EditorWindow
    {
        private string sourceFolder;
        private string outputFolder = "Assets/DetailMeshPrefabs";

        [MenuItem("Tools/PrefabTools/Extract Detail Mesh From Prefabs")]
        static void Init()
        {
            GetWindow<DetailMeshExtractor>("Detail Mesh Extractor");
        }

        void OnGUI()
        {
            GUILayout.Label("从 Prefab 提取子 Mesh 并生成可用 Detail Prefab", EditorStyles.boldLabel);

            sourceFolder = EditorGUILayout.TextField("InputPath", sourceFolder);
            outputFolder = EditorGUILayout.TextField("输出路径", outputFolder);

            if (GUILayout.Button("开始提取"))
            {
                ExtractAllPrefabs();
            }
        }

        void ExtractAllPrefabs()
        {
            if (sourceFolder == null)
            {
                Debug.LogError("请先选择包含 Prefab 的文件夹！");
                return;
            }

            string sourcePath = sourceFolder;
            string[] prefabPaths = Directory.GetFiles(sourcePath, "*.prefab", SearchOption.AllDirectories);

            if (!Directory.Exists(outputFolder))
                Directory.CreateDirectory(outputFolder);

            foreach (var path in prefabPaths)
            {
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (prefab == null) continue;

                // 实例化用于查找子物体
                GameObject instance = PrefabUtility.InstantiatePrefab(prefab) as GameObject;

                Transform target = FindFirstMeshObject(instance.transform);
                if (target != null)
                {
                    GameObject meshGO = Object.Instantiate(target.gameObject);
                    meshGO.name = prefab.name + "_Detail";

                    string savePath = $"{outputFolder}/{meshGO.name}.prefab";
                    PrefabUtility.SaveAsPrefabAsset(meshGO, savePath);
                    Debug.Log($"✅ 创建 Detail Mesh Prefab: {savePath}");

                    DestroyImmediate(meshGO);
                }
                else
                {
                    Debug.LogWarning($"⚠️ prefab {prefab.name} 没有找到带 Mesh 的子物体");
                }

                DestroyImmediate(instance);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        // 找到第一个带有 MeshFilter + MeshRenderer 的子物体
        Transform FindFirstMeshObject(Transform root)
        {
            foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
            {
                if (child.GetComponent<MeshFilter>() && child.GetComponent<MeshRenderer>())
                    return child;
            }

            return null;
        }
    }
}