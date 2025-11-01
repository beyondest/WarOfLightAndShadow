using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections.Generic;

public class PrefabDarkToLightReplacer : EditorWindow
{
    private GameObject originalPrefab;
    private string lightPrefabSearchPath = "Assets/Resources/Prefabs";
    private string outputPath = "Assets/Misc/Export";
    private string originalNamePrefix = "Dark";
    private string correspondingNamePrefix = "Light";
    private Dictionary<string, GameObject> lightPrefabDict;

    [MenuItem("Tools/BatchOperation/Replace Dark With Light Prefabs")]
    public static void ShowWindow()
    {
        GetWindow<PrefabDarkToLightReplacer>("Replace Dark With Light Prefabs");
    }

    private void OnGUI()
    {
        originalPrefab = EditorGUILayout.ObjectField("Original Prefab", originalPrefab, typeof(GameObject), false) as GameObject;
        lightPrefabSearchPath = EditorGUILayout.TextField("Light Prefab Search Path", lightPrefabSearchPath);
        outputPath = EditorGUILayout.TextField("Output Path", outputPath);
        originalNamePrefix = EditorGUILayout.TextField("Original Name Prefix", originalNamePrefix);
        correspondingNamePrefix = EditorGUILayout.TextField("Corresponding Name Prefix", correspondingNamePrefix);

        if (GUILayout.Button("Execute"))
        {
            ReplaceDarkWithLight();
        }
    }

    private void ReplaceDarkWithLight()
    {
        if (originalPrefab == null)
        {
            Debug.LogError("Please choose prefab！");
            return;
        }

        if (!AssetDatabase.IsValidFolder(outputPath))
        {
            Directory.CreateDirectory(outputPath);
            AssetDatabase.Refresh();
        }

        // 预加载 Light prefab 映射
        lightPrefabDict = new Dictionary<string, GameObject>();
        string[] guids = AssetDatabase.FindAssets("t:prefab", new[] { lightPrefabSearchPath });

        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            string name = Path.GetFileNameWithoutExtension(path);
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (!lightPrefabDict.ContainsKey(name))
                lightPrefabDict.Add(name, prefab);
        }

        GameObject root = PrefabUtility.InstantiatePrefab(originalPrefab) as GameObject;
        root!.name = correspondingNamePrefix + originalPrefab.name[originalNamePrefix.Length..];

        ReplaceRecursive(root.transform);

        string outputPrefabPath = Path.Combine(outputPath, root.name + ".prefab").Replace("\\", "/");
        PrefabUtility.SaveAsPrefabAssetAndConnect(root, outputPrefabPath, InteractionMode.UserAction);
        Debug.Log("New Prefab created at: " + outputPrefabPath);
    }

    private void ReplaceRecursive(Transform current)
    {
        List<Transform> children = new List<Transform>();
        for (int i = 0; i < current.childCount; i++)
        {
            children.Add(current.GetChild(i));
        }

        foreach (var child in children)
        {
            ReplaceRecursive(child); // 先递归子节点

            if (child.name.StartsWith(originalNamePrefix))
            {
                string trimmedName =correspondingNamePrefix + child.name[originalNamePrefix.Length..]; // 去除前缀
                GameObject bestMatch = null;

                // Fuzzy match
                foreach (var kvp in lightPrefabDict)
                {
                    if (trimmedName.Contains(kvp.Key))
                    {
                        bestMatch = kvp.Value;
                        break; // Find first match then exit
                    }
                }

                if (bestMatch)
                {
                    GameObject newLight = PrefabUtility.InstantiatePrefab(bestMatch) as GameObject;
                    newLight.transform.SetParent(child.parent, false);
                    newLight.transform.localPosition = child.localPosition;
                    newLight.transform.localRotation = child.localRotation;
                    newLight.transform.localScale = child.localScale;
                    newLight.name = bestMatch.name;

                    DestroyImmediate(child.gameObject);
                }
                else
                {
                    Debug.LogWarning($"Fuzzy match false : cannot find {child.name} Light prefab。");
                }
            }
        }
    }

    // private void ReplaceRecursive(Transform current)
    // {
    //     List<Transform> children = new List<Transform>();
    //     for (int i = 0; i < current.childCount; i++)
    //     {
    //         children.Add(current.GetChild(i));
    //     }
    //
    //     foreach (var child in children)
    //     {
    //         ReplaceRecursive(child); // 先递归子节点
    //
    //         if (child.name.StartsWith(originalNamePrefix))
    //         {
    //             string lightName = correspondingNamePrefix + child.name[originalNamePrefix.Length..];
    //
    //             if (lightPrefabDict.TryGetValue(lightName, out GameObject lightPrefab) && lightPrefab != null)
    //             {
    //                 GameObject newLight = PrefabUtility.InstantiatePrefab(lightPrefab) as GameObject;
    //                 newLight.transform.SetParent(child.parent, false);
    //                 newLight.transform.localPosition = child.localPosition;
    //                 newLight.transform.localRotation = child.localRotation;
    //                 newLight.transform.localScale = child.localScale;
    //                 newLight.name = lightPrefab.name;
    //
    //                 GameObject.DestroyImmediate(child.gameObject);
    //             }
    //             else
    //             {
    //                 Debug.LogWarning($"Could not find prefab named '{lightName}' in path.");
    //             }
    //         }
    //     }
    // }

}
