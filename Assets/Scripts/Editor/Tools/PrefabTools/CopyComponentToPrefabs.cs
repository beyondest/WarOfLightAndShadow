using UnityEngine;
using UnityEditor;

namespace Editor
{
 
    public class CopyComponentToPrefabs : EditorWindow
    {
        private GameObject sourcePrefab;
        private string folderPath = "Assets/Prefabs";
        private string componentTypeName = "YourComponent"; // Replace with your default type name

        [MenuItem("Tools/PrefabTools/Copy Component To Prefabs")]
        public static void ShowWindow()
        {
            GetWindow<CopyComponentToPrefabs>("Copy Component");
        }

        private void OnGUI()
        {
            GUILayout.Label("Copy Component To Prefabs", EditorStyles.boldLabel);

            sourcePrefab =
                (GameObject)EditorGUILayout.ObjectField("Source Prefab", sourcePrefab, typeof(GameObject), false);
            componentTypeName = EditorGUILayout.TextField("Component Type (e.g. MyScript)", componentTypeName);
            folderPath = EditorGUILayout.TextField("Folder Path", folderPath);

            if (GUILayout.Button("Copy Component to All Prefabs"))
            {
                if (sourcePrefab == null)
                {
                    Debug.LogError("Source Prefab is not assigned.");
                    return;
                }

                CopyComponentToPrefabsInFolder();
            }
        }

        private void CopyComponentToPrefabsInFolder()
        {
            Component sourceComponent = sourcePrefab.GetComponent(componentTypeName);
            if (sourceComponent == null)
            {
                Debug.LogError($"Component of type {componentTypeName} not found on source prefab.");
                return;
            }

            string[] guids = AssetDatabase.FindAssets("t:Prefab", new[] { folderPath });
            int modifiedCount = 0;

            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (prefab == null) continue;

                GameObject prefabInstance = PrefabUtility.LoadPrefabContents(path);
                Component targetComponent = prefabInstance.GetComponent(componentTypeName);
                if (targetComponent == null)
                {
                    targetComponent = prefabInstance.AddComponent(sourceComponent.GetType());
                }

                // Copy serialized fields
                SerializedObject sourceSerialized = new SerializedObject(sourceComponent);
                SerializedObject targetSerialized = new SerializedObject(targetComponent);

                SerializedProperty prop = sourceSerialized.GetIterator();
                while (prop.NextVisible(true))
                {
                    if (prop.name == "m_Script") continue; // skip script reference
                    targetSerialized.CopyFromSerializedProperty(prop);
                }

                targetSerialized.ApplyModifiedProperties();

                // Save changes
                PrefabUtility.SaveAsPrefabAsset(prefabInstance, path);
                PrefabUtility.UnloadPrefabContents(prefabInstance);

                modifiedCount++;
            }

            AssetDatabase.SaveAssets();
            Debug.Log($"Finished updating {modifiedCount} prefabs with component {componentTypeName}.");
        }
    }
}
