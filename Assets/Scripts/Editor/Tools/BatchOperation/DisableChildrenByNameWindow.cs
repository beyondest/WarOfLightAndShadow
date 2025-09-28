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
            GUILayout.Label("批量禁用子物体", EditorStyles.boldLabel);
            targetName = EditorGUILayout.TextField("匹配名称包含:", targetName);
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
                Debug.LogWarning("请输入匹配字符串！");
                return;
            }

            GameObject[] selectedObjects = Selection.gameObjects;
            if (selectedObjects.Length == 0)
            {
                Debug.LogWarning("请先在层级面板中选择至少一个物体！");
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

            Debug.Log($"已禁用 {count} 个子物体（匹配：{targetName}）");
        }
    }
}