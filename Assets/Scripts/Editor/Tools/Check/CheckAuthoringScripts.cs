using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Editor
{
    public class UnusedAuthoringFinder : EditorWindow
    {
        private List<string> _unusedScripts = new();
        private string _searchFolder = "Assets/Scripts"; 
        private Vector2 _scroll;

        [MenuItem("Tools/Check/Check Unused Authoring Scripts")]
        static void ShowWindow()
        {
            GetWindow<UnusedAuthoringFinder>("Unused Authoring Checker");
        }

        void OnGUI()
        {
            GUILayout.Label("Authoring Script Checker", EditorStyles.boldLabel);

            // 路径选择
            EditorGUILayout.BeginHorizontal();
            _searchFolder = EditorGUILayout.TextField("Search Folder", _searchFolder);
            if (GUILayout.Button("Select", GUILayout.Width(60)))
            {
                string path = EditorUtility.OpenFolderPanel("Select Folder", "Assets", "");
                if (!string.IsNullOrEmpty(path))
                {
                    if (path.StartsWith(Application.dataPath))
                    {
                        _searchFolder = "Assets" + path.Substring(Application.dataPath.Length);
                    }
                    else
                    {
                        Debug.LogWarning("Please select a folder within the Assets directory.");
                    }
                }
            }

            EditorGUILayout.EndHorizontal();

            if (GUILayout.Button("Check Unused Authoring Scripts"))
            {
                _unusedScripts = FindUnusedAuthoringScripts(_searchFolder);
            }

            GUILayout.Space(10);
            if (_unusedScripts != null && _unusedScripts.Count > 0)
            {
                GUILayout.Label($"Unused Scripts in {_searchFolder}:", EditorStyles.boldLabel);
                _scroll = GUILayout.BeginScrollView(_scroll, GUILayout.Height(300));
                foreach (var path in _unusedScripts)
                {
                    EditorGUILayout.BeginHorizontal();
                    GUILayout.Label(path);
                    if (GUILayout.Button("Ping", GUILayout.Width(50)))
                    {
                        var asset = AssetDatabase.LoadAssetAtPath<MonoScript>(path);
                        EditorGUIUtility.PingObject(asset);
                    }

                    EditorGUILayout.EndHorizontal();
                }

                GUILayout.EndScrollView();
            }
            else if (Event.current.type == EventType.Repaint)
            {
                // GUILayout.Label("No unused scripts found or not checked yet.");
            }
        }

        List<string> FindUnusedAuthoringScripts(string folder)
        {
            string[] guids = AssetDatabase.FindAssets("t:MonoScript", new[] { folder });
            var matchingScripts = guids
                .Select(AssetDatabase.GUIDToAssetPath)
                .Where(path =>
                {
                    string fileName = Path.GetFileNameWithoutExtension(path).ToLower();
                    return (fileName.Contains("author") || fileName.Contains("authoring"))
                           && path.EndsWith(".cs")
                           && !path.Contains("/Editor/");
                })
                .ToList();

            var behaviours = FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None);
            var usedTypeNames = new HashSet<string>(behaviours.Select(b => b.GetType().Name));

            var unused = matchingScripts
                .Where(path => !usedTypeNames.Contains(Path.GetFileNameWithoutExtension(path)))
                .ToList();

            return unused;
        }
    }
}