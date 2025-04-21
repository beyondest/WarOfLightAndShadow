using UnityEngine;
using UnityEditor;
using System.IO;

namespace Editor
{
    public class ReplaceMaterialsInPrefabs : EditorWindow
    {
        private Material newMaterial;
        private string folderPath = "Assets/Prefabs";

        [MenuItem("Tools/Custom/Replace Materials In Prefabs")]
        public static void ShowWindow()
        {
            GetWindow<ReplaceMaterialsInPrefabs>("Replace Materials");
        }

        private void OnGUI()
        {
            GUILayout.Label("Replace Materials In Prefabs", EditorStyles.boldLabel);

            newMaterial = (Material)EditorGUILayout.ObjectField("New Material", newMaterial, typeof(Material), false);
            folderPath = EditorGUILayout.TextField("Folder Path", folderPath);

            if (GUILayout.Button("Replace Materials"))
            {
                if (newMaterial == null)
                {
                    Debug.LogError("Please assign a material.");
                    return;
                }

                ReplaceMaterials();
            }
        }

        private void ReplaceMaterials()
        {
            string[] guids = AssetDatabase.FindAssets("t:Prefab", new[] { folderPath });
            int replacedCount = 0;

            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (prefab == null) continue;

                GameObject prefabInstance = PrefabUtility.LoadPrefabContents(path);
                Renderer[] renderers = prefabInstance.GetComponentsInChildren<Renderer>(true);

                bool changed = false;

                foreach (Renderer renderer in renderers)
                {
                    if (renderer.sharedMaterial != newMaterial)
                    {
                        Undo.RecordObject(renderer, "Replace Material");
                        renderer.sharedMaterial = newMaterial;
                        changed = true;
                    }
                }

                if (changed)
                {
                    PrefabUtility.SaveAsPrefabAsset(prefabInstance, path);
                    replacedCount++;
                }

                PrefabUtility.UnloadPrefabContents(prefabInstance);
            }

            AssetDatabase.SaveAssets();
            Debug.Log($"replace complete {replacedCount} prefab。");
        }
    }
}