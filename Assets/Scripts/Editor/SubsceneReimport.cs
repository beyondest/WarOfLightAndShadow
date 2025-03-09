using UnityEditor;
using UnityEngine;

public class SubsceneReimport : EditorWindow
{
    [MenuItem("Tools/Fix SubScene Import")]
    public static void FixSubScene()
    {
        Debug.Log("Forcing SubScene reimport...");

        string[] subScenes = AssetDatabase.FindAssets("l:Subscene");
        foreach (string guid in subScenes)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
            Debug.Log($"Reimported: {path}");
        }

        Debug.Log("SubScene reimport complete.");
    }
}