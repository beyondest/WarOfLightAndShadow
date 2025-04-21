namespace Editor
{
    using UnityEngine;
    using UnityEditor;
    using TMPro;

    public class TMPFontReplacer : EditorWindow
    {
        TMP_FontAsset _newFont;

        [MenuItem("Tools/Custom/Replace TMP Fonts")]
        static void Init()
        {
            TMPFontReplacer window = (TMPFontReplacer)GetWindow(typeof(TMPFontReplacer));
            window.titleContent = new GUIContent("TMP Font Replacer");
            window.Show();
        }

        void OnGUI()
        {
            GUILayout.Label("Replace TMP Fonts in All Scenes", EditorStyles.boldLabel);
            _newFont = (TMP_FontAsset)EditorGUILayout.ObjectField("New Font", _newFont, typeof(TMP_FontAsset), false);

            if (GUILayout.Button("Replace All"))
            {
                if (_newFont == null)
                {
                    Debug.LogWarning("Please select a TMP_FontAsset first!");
                    return;
                }

                ReplaceAllFonts(_newFont);
            }
        }

        void ReplaceAllFonts(TMP_FontAsset fontAsset)
        {
            int count = 0;
            TextMeshProUGUI[] allTextComponents = FindObjectsByType<TextMeshProUGUI>(sortMode: FindObjectsSortMode.None); // Include hidden objects
            foreach (var text in allTextComponents)
            {
                Undo.RecordObject(text, "Replace TMP Font");
                text.font = fontAsset;
                count++;
            }

            Debug.Log($"Replaced {count} TextMeshProUGUI fonts.");

            // If you also use TextMeshPro (3D World Text), add this section
            TextMeshPro[] allText3D = FindObjectsByType<TextMeshPro>(sortMode: FindObjectsSortMode.None);
            foreach (var text in allText3D)
            {
                Undo.RecordObject(text, "Replace TMP Font");
                text.font = fontAsset;
                count++;
            }

            Debug.Log($"Replaced TextMeshPro fonts: {count} objects in total.");
        }
    }
}