using UnityEditor;
using UnityEngine;
using System.IO;

namespace Editor
{
    public class BatchRenameAssets : EditorWindow
    {
        private string _folderPath = "Assets/";
        private string _prefix = "";
        private string _suffix = "";

        [MenuItem("Tools/BatchOperation/Batch Rename Assets")]
        public static void ShowWindow()
        {
            GetWindow<BatchRenameAssets>("Batch Rename Assets");
        }

        private void OnGUI()
        {
            GUILayout.Label("Batch Rename Settings", EditorStyles.boldLabel);

            _folderPath = EditorGUILayout.TextField("Folder Path (Assets/)", _folderPath);
            _prefix = EditorGUILayout.TextField("Prefix", _prefix);
            _suffix = EditorGUILayout.TextField("Suffix", _suffix);

            if (GUILayout.Button("Batch Rename"))
            {
                RenameAssetsInFolder(_folderPath, _prefix, _suffix);
            }
        }

        private static void RenameAssetsInFolder(string folderPath, string prefix, string suffix)
        {
            if (!AssetDatabase.IsValidFolder(folderPath))
            {
                Debug.LogError("Invalid folder path: " + folderPath);
                return;
            }

            string[] guids = AssetDatabase.FindAssets("", new[] { folderPath });

            foreach (string guid in guids)
            {
                string assetPath = AssetDatabase.GUIDToAssetPath(guid);

                // 排除文件夹自身
                if (Directory.Exists(assetPath))
                    continue;

                string fileName = Path.GetFileNameWithoutExtension(assetPath);
                string extension = Path.GetExtension(assetPath);
                string directory = Path.GetDirectoryName(assetPath);

                string newFileName = $"{prefix}{fileName}{suffix}{extension}";
                if (directory != null)
                {
                    string newPath = Path.Combine(directory, newFileName).Replace("\\", "/");

                    if (AssetDatabase.RenameAsset(assetPath, $"{prefix}{fileName}{suffix}") != "")
                    {
                        Debug.LogError($"Failed to rename {assetPath}");
                    }
                    else
                    {
                        Debug.Log($"Renamed: {assetPath} → {newPath}");
                    }
                }
            }

            AssetDatabase.Refresh();
        }
    }
}