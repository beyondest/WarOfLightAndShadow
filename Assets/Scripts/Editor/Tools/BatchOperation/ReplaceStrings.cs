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
        private readonly List<ReplacePair> replaceList = new();
        private Vector2 scrollPos;

        [MenuItem("Tools/BatchOperation/Replace Strings")]
        static void Init()
        {
            GetWindow<FileNameStringReplacer>("Replace Strings");
        }

        private void OnGUI()
        {
            GUILayout.Label("Batch Replace Strings ", EditorStyles.boldLabel);

            GUILayout.Label("Target folder（ Assets）:");
            folderPath = EditorGUILayout.TextField(folderPath);

            if (GUILayout.Button("Choose folder"))
            {
                string selectedPath = EditorUtility.OpenFolderPanel("Choose folder", Application.dataPath, "");
                if (!string.IsNullOrEmpty(selectedPath) && selectedPath.StartsWith(Application.dataPath))
                {
                    folderPath = "Assets" + selectedPath.Substring(Application.dataPath.Length);
                }
            }

            EditorGUILayout.Space();
            GUILayout.Label("Target strings : Replace into strings:");
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

            if (GUILayout.Button("Add rule"))
            {
                replaceList.Add(new ReplacePair());
            }

            EditorGUILayout.Space();

            if (GUILayout.Button("Execute"))
            {
                if (EditorUtility.DisplayDialog("Confirm", $"Change file names in {folderPath} ？", "Yes", "No"))
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
                Debug.LogError("Path not exits: " + fullPath);
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
                        Debug.Log($"File name replaced: {fileName}{extension} → {newFileName}{extension}");
                        renamedCount++;
                    }
                    else
                    {
                        Debug.LogWarning($"Target file already exists: {newFullPath}");
                    }
                }
            }

            AssetDatabase.Refresh();
            EditorUtility.DisplayDialog("Complete", $"Renamed {renamedCount} files", "Confirm");
        }
    }
}