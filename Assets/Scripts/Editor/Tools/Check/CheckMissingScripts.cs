using UnityEngine;
using UnityEditor;
using System.IO;


public class FindMissingScriptsWindow : EditorWindow
{
    private enum ScanMode
    {
        Scene,
        PrefabFolder
    }

    private ScanMode scanMode = ScanMode.Scene;

    private DefaultAsset prefabFolder; 

    [MenuItem("Tools/Check/Find Missing Scripts")]
    public static void ShowWindow()
    {
        GetWindow<FindMissingScriptsWindow>("Find Missing Scripts");
    }

    private void OnGUI()
    {
        GUILayout.Label("Missing Script Finder", EditorStyles.boldLabel);

        scanMode = (ScanMode)EditorGUILayout.EnumPopup("Scan Mode", scanMode);

        if (scanMode == ScanMode.PrefabFolder)
        {
            prefabFolder =
                (DefaultAsset)EditorGUILayout.ObjectField("Prefab Folder", prefabFolder, typeof(DefaultAsset), false);
        }

        if (GUILayout.Button("Start Scan"))
        {
            if (scanMode == ScanMode.Scene)
            {
                FindInScene();
            }
            else if (scanMode == ScanMode.PrefabFolder && prefabFolder != null)
            {
                string path = AssetDatabase.GetAssetPath(prefabFolder);
                FindInPrefabs(path);
            }
            else
            {
                Debug.LogWarning("Choose a folder first！");
            }
        }
    }

    private void FindInScene()
    {
        GameObject[] goArray = GameObject.FindObjectsByType<GameObject>(FindObjectsSortMode.None);
        int count = 0;

        foreach (GameObject go in goArray)
        {
            Component[] components = go.GetComponents<Component>();
            for (int i = 0; i < components.Length; i++)
            {
                if (components[i] == null)
                {
                    Debug.LogWarning($"[Scene] Missing script found in GameObject: {GetFullPath(go)}", go);
                    count++;
                }
            }
        }

        Debug.Log($"[Scene] Search complete! Found {count} missing scripts.");
    }

    private void FindInPrefabs(string folderPath)
    {
        string[] prefabPaths = Directory.GetFiles(folderPath, "*.prefab", SearchOption.AllDirectories);
        int count = 0;

        foreach (string path in prefabPaths)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab == null) continue;

            Component[] components = prefab.GetComponentsInChildren<Component>(true);

            foreach (Component c in components)
            {
                if (c == null)
                {
                    Debug.LogWarning($"[Prefab] Missing script found in: {path}", prefab);
                    count++;
                }
            }
        }

        Debug.Log($"[Prefab] Search complete! Found {count} missing scripts in prefabs under {folderPath}.");
    }

    private static string GetFullPath(GameObject go)
    {
        return go.transform.parent == null ? go.name : GetFullPath(go.transform.parent.gameObject) + "/" + go.name;
    }
}