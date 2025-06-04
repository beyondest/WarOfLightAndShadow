using UnityEngine;
using UnityEditor;
using System.IO;

public class BatchRenameFiles : EditorWindow
{
    private string folderPath = "Assets/";
    private string searchString = "dark";
    private string replaceString = "light";

    [MenuItem("Tools/BatchOperation/ReplaceStringInNames")]
    public static void ShowWindow()
    {
        GetWindow<BatchRenameFiles>("ReplaceStringInNames");
    }

    void OnGUI()
    {
        GUILayout.Label("ReplaceStringInNames", EditorStyles.boldLabel);

        folderPath = EditorGUILayout.TextField("Target folder", folderPath);
        searchString = EditorGUILayout.TextField("String to replace", searchString);
        replaceString = EditorGUILayout.TextField("replace to", replaceString);

        if (GUILayout.Button("Execute"))
        {
            RenameFiles(folderPath, searchString, replaceString);
        }
    }

    private void RenameFiles(string relativeFolderPath, string search, string replace)
    {
        string fullPath = Path.Combine(Application.dataPath, relativeFolderPath.Replace("Assets/", ""));
        if (!Directory.Exists(fullPath))
        {
            Debug.LogError("Not exist：" + fullPath);
            return;
        }

        string[] files = Directory.GetFiles(fullPath, "*.*", SearchOption.AllDirectories);
        int renamedCount = 0;

        foreach (string filePath in files)
        {
            string fileName = Path.GetFileName(filePath);
            if (fileName.Contains(search))
            {
                string newFileName = fileName.Replace(search, replace);
                string newFilePath = Path.Combine(Path.GetDirectoryName(filePath), newFileName);

                // 重命名文件
                File.Move(filePath, newFilePath);
                renamedCount++;

                Debug.Log($"重命名: {fileName} → {newFileName}");
            }
        }

        AssetDatabase.Refresh();
        Debug.Log($"完成重命名，总共修改了 {renamedCount} 个文件。");
    }
}