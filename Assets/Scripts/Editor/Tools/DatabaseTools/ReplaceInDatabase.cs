using PlasticGui;
using SparFlame.Components.SubGameplay;
using SparFlame.Database;
using SparFlame.Systems.SubGameplay.RandomSpawn;

namespace Editor
{
    using UnityEngine;
using UnityEditor;

public class ItemDatabaseEditor : EditorWindow
{
    private EnvDatabaseSo database;
    private string fromPrefix = "Gray";
    private string toPrefix = "Blue";
    private int fromType; 
    private int toType;   

    [MenuItem("Tools/Database/Replace Item Prefabs")]
    public static void ShowWindow()
    {
        GetWindow<ItemDatabaseEditor>("Replace Item Prefabs");
    }

    private void OnGUI()
    {
        database = (EnvDatabaseSo)EditorGUILayout.ObjectField("Item Database", database, typeof(EnvDatabaseSo), false);
        fromPrefix = EditorGUILayout.TextField("From Prefix", fromPrefix);
        toPrefix = EditorGUILayout.TextField("To Prefix", toPrefix);
        fromType = EditorGUILayout.IntField("From Type (int)", fromType);
        toType = EditorGUILayout.IntField("To Type (int)", toType);

        if (GUILayout.Button("Replace Prefabs and Type"))
        {
            ReplaceItems();
        }
    }

    private void ReplaceItems()
    {
        if (database == null)
        {
            Debug.LogError("Database is null.");
            return;
        }

        int replacedCount = 0;

        foreach (var item in database.items)
        {
            if ((int)item.type != fromType || item.prefab == null) continue;

            string oldName = item.prefab.name;

            if (!oldName.StartsWith(fromPrefix)) continue;

            string newName = oldName.Replace(fromPrefix, toPrefix);

            string[] guids = AssetDatabase.FindAssets($"{newName} t:prefab");
            if (guids.Length == 0)
            {
                Debug.LogWarning($"Prefab {newName} not found for replacement.");
                continue;
            }

            string path = AssetDatabase.GUIDToAssetPath(guids[0]);
            GameObject newPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);

            if (newPrefab != null)
            {
                item.prefab = newPrefab;
                item.type = (EnvType)toType;
                replacedCount++;
            }
        }

        EditorUtility.SetDirty(database);
        AssetDatabase.SaveAssets();

        Debug.Log($"Replaced {replacedCount} items from {fromPrefix} to {toPrefix}, type {fromType} → {toType}.");
    }
}

}