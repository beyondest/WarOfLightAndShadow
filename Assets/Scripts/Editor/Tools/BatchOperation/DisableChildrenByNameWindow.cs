using UnityEngine;
using UnityEditor;

namespace Editor
{
    public class DisableChildrenByNameWindow : EditorWindow
    {
        private string targetName = "Target"; // 在GUI里配置的关键字
        private bool shouldDelete;

        [MenuItem("Tools/BatchOperation/Disable Children By Name")]
        public static void ShowWindow()
        {
            GetWindow<DisableChildrenByNameWindow>("Disable Children");
        }

        private void OnGUI()
        {
            GUILayout.Label("Disable Children by name", EditorStyles.boldLabel);
            targetName = EditorGUILayout.TextField("Match Name:", targetName);
            shouldDelete = EditorGUILayout.Toggle("Should Delete", shouldDelete);
            if (GUILayout.Button("Delete or disable children with specific name"))
            {
                DisableOrDelete();
            }
        }

        private void DisableOrDelete()
        {
            if (string.IsNullOrEmpty(targetName))
            {
                Debug.LogWarning("Match name is null or empty");
                return;
            }

            GameObject[] selectedObjects = Selection.gameObjects;
            if (selectedObjects.Length == 0)
            {
                Debug.LogWarning("Selected objects not found！");
                return;
            }

            int count = 0;
            foreach (GameObject go in selectedObjects)
            {
                Transform[] children = go.GetComponentsInChildren<Transform>(true);
                foreach (Transform child in children)
                {
                    if (child != go.transform && child.name.Contains(targetName))
                    {
                        Undo.RecordObject(child.gameObject, "Disable Or Delete Child");
                        if (shouldDelete)
                        {
                            DestroyImmediate(child.gameObject);
                        }
                        else
                        {
                            child.gameObject.SetActive(false);
                        }

                        count++;
                    }
                }
            }

            Debug.Log($"Disable Count {count} ：Match : {targetName}");
        }
    }
}