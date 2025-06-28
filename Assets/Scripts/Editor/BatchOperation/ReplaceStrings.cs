using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using System.IO;

namespace Editor
{
    public class FileNameStringReplacer : EditorWindow
    {
        [System.Serializable]
        public class ReplacePair
        {
            public string oldValue;
            public string newValue;
        }

        private string folderPath = "Assets/";
        private List<ReplacePair> replaceList = new List<ReplacePair>();
        private Vector2 scrollPos;

        [MenuItem("Tools/Custom/Replace Strings")]
        static void Init()
        {
            GetWindow<FileNameStringReplacer>("文件名替换工具");
        }

        private void OnGUI()
        {
            GUILayout.Label("批量替换文件名中的字符串", EditorStyles.boldLabel);

            GUILayout.Label("目标文件夹路径（相对 Assets）:");
            folderPath = EditorGUILayout.TextField(folderPath);

            if (GUILayout.Button("选择文件夹"))
            {
                string selectedPath = EditorUtility.OpenFolderPanel("选择文件夹", Application.dataPath, "");
                if (!string.IsNullOrEmpty(selectedPath) && selectedPath.StartsWith(Application.dataPath))
                {
                    folderPath = "Assets" + selectedPath.Substring(Application.dataPath.Length);
                }
            }

            EditorGUILayout.Space();
            GUILayout.Label("替换规则列表:");
            scrollPos = EditorGUILayout.BeginScrollView(scrollPos, GUILayout.Height(200));

            for (int i = 0; i < replaceList.Count; i++)
            {
                EditorGUILayout.BeginHorizontal();
                replaceList[i].oldValue = EditorGUILayout.TextField(replaceList[i].oldValue);
                replaceList[i].newValue = EditorGUILayout.TextField(replaceList[i].newValue);
                if (GUILayout.Button("X", GUILayout.Width(20)))
                {
                    replaceList.RemoveAt(i);
                    break;
                }

                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.EndScrollView();

            if (GUILayout.Button("添加替换项"))
            {
                replaceList.Add(new ReplacePair());
            }

            EditorGUILayout.Space();

            if (GUILayout.Button("执行文件名替换"))
            {
                if (EditorUtility.DisplayDialog("确认替换", $"将替换 {folderPath} 中所有文件名，是否继续？", "是", "否"))
                {
                    ReplaceFileNames();
                }
            }
        }

        private void ReplaceFileNames()
        {
            string fullPath =
                Path.Combine(Application.dataPath.Substring(0, Application.dataPath.Length - "Assets".Length),
                    folderPath);
            if (!Directory.Exists(fullPath))
            {
                Debug.LogError("路径不存在: " + fullPath);
                return;
            }

            string[] files = Directory.GetFiles(fullPath, "*.*", SearchOption.AllDirectories);
            int renamedCount = 0;

            foreach (string file in files)
            {
                string fileName = Path.GetFileNameWithoutExtension(file);
                string extension = Path.GetExtension(file);
                string directory = Path.GetDirectoryName(file);

                string newFileName = fileName;

                foreach (var pair in replaceList)
                {
                    if (!string.IsNullOrEmpty(pair.oldValue))
                        newFileName = newFileName.Replace(pair.oldValue, pair.newValue);
                }

                if (newFileName != fileName)
                {
                    string newFullPath = Path.Combine(directory, newFileName + extension);

                    // 避免重命名冲突
                    if (!File.Exists(newFullPath))
                    {
                        File.Move(file, newFullPath);
                        Debug.Log($"文件重命名: {fileName}{extension} → {newFileName}{extension}");
                        renamedCount++;
                    }
                    else
                    {
                        Debug.LogWarning($"跳过重命名，目标已存在: {newFullPath}");
                    }
                }
            }

            AssetDatabase.Refresh();
            EditorUtility.DisplayDialog("完成", $"共重命名 {renamedCount} 个文件", "确定");
        }
    }
}