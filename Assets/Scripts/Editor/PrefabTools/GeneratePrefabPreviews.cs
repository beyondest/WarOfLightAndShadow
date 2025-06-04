using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections.Generic;
using Unity.EditorCoroutines.Editor;

public class PrefabPreviewExporter : EditorWindow
{
    private string inputFolder = "Assets/Prefabs";
    private string outputFolder = "Assets/PreviewIcons";
    private string filePrefix = "";
    private string fileSuffix = "";

    private readonly List<GameObject> prefabs = new();

    [MenuItem("Tools/PrefabTools/Export Prefab Previews (with Naming)")]
    public static void ShowWindow()
    {
        GetWindow<PrefabPreviewExporter>("Export Prefab Previews");
    }

    private void OnGUI()
    {
        EditorGUILayout.LabelField("Input & Output", EditorStyles.boldLabel);

        inputFolder = EditorGUILayout.TextField("Prefab Folder", inputFolder);
        outputFolder = EditorGUILayout.TextField("Output Folder", outputFolder);

        EditorGUILayout.Space();

        EditorGUILayout.LabelField("PNG File Name Options", EditorStyles.boldLabel);
        filePrefix = EditorGUILayout.TextField("Prefix", filePrefix);
        fileSuffix = EditorGUILayout.TextField("Suffix", fileSuffix);

        EditorGUILayout.Space();

        if (GUILayout.Button("Load Prefabs"))
        {
            LoadPrefabsFromFolder(inputFolder);
            Debug.Log($"Found {prefabs.Count} prefabs.");
        }

        if (GUILayout.Button("Export Previews"))
        {
            if (!Directory.Exists(outputFolder))
                Directory.CreateDirectory(outputFolder);

            
            EditorCoroutineUtility.StartCoroutine(ExportAllPreviews(), this);
        }
    }

    private void LoadPrefabsFromFolder(string folder)
    {
        prefabs.Clear();

        string[] guids = AssetDatabase.FindAssets("t:Prefab", new[] { folder });
        foreach (string guid in guids)
        {
            string assetPath = AssetDatabase.GUIDToAssetPath(guid);
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
            if (prefab != null)
                prefabs.Add(prefab);
        }
    }

    private System.Collections.IEnumerator ExportAllPreviews()
    {
        int successCount = 0;
        foreach (var prefab in prefabs)
        {
            Texture2D preview = AssetPreview.GetAssetPreview(prefab);

            // 等待预览图生成
            float timer = 0f;
            while (preview == null && timer < 5f)
            {
                yield return new EditorWaitForSeconds(0.1f);
                preview = AssetPreview.GetAssetPreview(prefab);
                timer += 0.1f;
            }

            if (preview == null)
            {
                Debug.LogWarning($"Failed to generate preview for {prefab.name}");
                continue;
            }

            string filename = filePrefix + prefab.name + fileSuffix + ".png";
            string path = Path.Combine(outputFolder, filename);
            File.WriteAllBytes(path, preview.EncodeToPNG());

            successCount++;
        }

        AssetDatabase.Refresh();
        Debug.Log($"Export complete. {successCount}/{prefabs.Count} previews generated.");
    }
}
