using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System;
using System.IO;
using System.Reflection;
using UnityEngine.Serialization;

[CustomEditor(typeof(Readme))]
[InitializeOnLoad]
public class ReadmeEditor : Editor
{
    static string _sShowedReadmeSessionStateName = "ReadmeEditor.showedReadme";
    
    static string _sReadmeSourceDirectory = "Assets/TutorialInfo";

    const float KSpace = 16f;

    static ReadmeEditor()
    {
        EditorApplication.delayCall += SelectReadmeAutomatically;
    }

    static void RemoveTutorial()
    {
        if (EditorUtility.DisplayDialog("Remove Readme Assets",
            
            $"All contents under {_sReadmeSourceDirectory} will be removed, are you sure you want to proceed?",
            "Proceed",
            "Cancel"))
        {
            if (Directory.Exists(_sReadmeSourceDirectory))
            {
                FileUtil.DeleteFileOrDirectory(_sReadmeSourceDirectory);
                FileUtil.DeleteFileOrDirectory(_sReadmeSourceDirectory + ".meta");
            }
            else
            {
                Debug.Log($"Could not find the Readme folder at {_sReadmeSourceDirectory}");
            }

            var readmeAsset = SelectReadme();
            if (readmeAsset != null)
            {
                var path = AssetDatabase.GetAssetPath(readmeAsset);
                FileUtil.DeleteFileOrDirectory(path + ".meta");
                FileUtil.DeleteFileOrDirectory(path);
            }

            AssetDatabase.Refresh();
        }
    }

    static void SelectReadmeAutomatically()
    {
        if (!SessionState.GetBool(_sShowedReadmeSessionStateName, false))
        {
            var readme = SelectReadme();
            SessionState.SetBool(_sShowedReadmeSessionStateName, true);

            if (readme && !readme.loadedLayout)
            {
                LoadLayout();
                readme.loadedLayout = true;
            }
        }
    }

    static void LoadLayout()
    {
        var assembly = typeof(EditorApplication).Assembly;
        var windowLayoutType = assembly.GetType("UnityEditor.WindowLayout", true);
        var method = windowLayoutType.GetMethod("LoadWindowLayout", BindingFlags.Public | BindingFlags.Static);
        method.Invoke(null, new object[] { Path.Combine(Application.dataPath, "TutorialInfo/Layout.wlt"), false });
    }

    static Readme SelectReadme()
    {
        var ids = AssetDatabase.FindAssets("Readme t:Readme");
        if (ids.Length == 1)
        {
            var readmeObject = AssetDatabase.LoadMainAssetAtPath(AssetDatabase.GUIDToAssetPath(ids[0]));

            Selection.objects = new UnityEngine.Object[] { readmeObject };

            return (Readme)readmeObject;
        }
        else
        {
            Debug.Log("Couldn't find a readme");
            return null;
        }
    }

    protected override void OnHeaderGUI()
    {
        var readme = (Readme)target;
        Init();

        var iconWidth = Mathf.Min(EditorGUIUtility.currentViewWidth / 3f - 20f, 128f);

        GUILayout.BeginHorizontal("In BigTitle");
        {
            if (readme.icon != null)
            {
                GUILayout.Space(KSpace);
                GUILayout.Label(readme.icon, GUILayout.Width(iconWidth), GUILayout.Height(iconWidth));
            }
            GUILayout.Space(KSpace);
            GUILayout.BeginVertical();
            {

                GUILayout.FlexibleSpace();
                GUILayout.Label(readme.title, TitleStyle);
                GUILayout.FlexibleSpace();
            }
            GUILayout.EndVertical();
            GUILayout.FlexibleSpace();
        }
        GUILayout.EndHorizontal();
    }

    public override void OnInspectorGUI()
    {
        var readme = (Readme)target;
        Init();

        foreach (var section in readme.sections)
        {
            if (!string.IsNullOrEmpty(section.heading))
            {
                GUILayout.Label(section.heading, HeadingStyle);
            }

            if (!string.IsNullOrEmpty(section.text))
            {
                GUILayout.Label(section.text, BodyStyle);
            }

            if (!string.IsNullOrEmpty(section.linkText))
            {
                if (LinkLabel(new GUIContent(section.linkText)))
                {
                    Application.OpenURL(section.url);
                }
            }

            GUILayout.Space(KSpace);
        }

        if (GUILayout.Button("Remove Readme Assets", ButtonStyle))
        {
            RemoveTutorial();
        }
    }

    bool _mInitialized;

    GUIStyle LinkStyle
    {
        get { return mLinkStyle; }
    }

    [FormerlySerializedAs("m_LinkStyle")] [SerializeField]
    GUIStyle mLinkStyle;

    GUIStyle TitleStyle
    {
        get { return mTitleStyle; }
    }

    [FormerlySerializedAs("m_TitleStyle")] [SerializeField]
    GUIStyle mTitleStyle;

    GUIStyle HeadingStyle
    {
        get { return mHeadingStyle; }
    }

    [FormerlySerializedAs("m_HeadingStyle")] [SerializeField]
    GUIStyle mHeadingStyle;

    GUIStyle BodyStyle
    {
        get { return mBodyStyle; }
    }

    [FormerlySerializedAs("m_BodyStyle")] [SerializeField]
    GUIStyle mBodyStyle;

    GUIStyle ButtonStyle
    {
        get { return mButtonStyle; }
    }

    [FormerlySerializedAs("m_ButtonStyle")] [SerializeField]
    GUIStyle mButtonStyle;

    void Init()
    {
        if (_mInitialized)
            return;
        mBodyStyle = new GUIStyle(EditorStyles.label);
        mBodyStyle.wordWrap = true;
        mBodyStyle.fontSize = 14;
        mBodyStyle.richText = true;

        mTitleStyle = new GUIStyle(mBodyStyle);
        mTitleStyle.fontSize = 26;

        mHeadingStyle = new GUIStyle(mBodyStyle);
        mHeadingStyle.fontStyle = FontStyle.Bold;
        mHeadingStyle.fontSize = 18;

        mLinkStyle = new GUIStyle(mBodyStyle);
        mLinkStyle.wordWrap = false;

        // Match selection color which works nicely for both light and dark skins
        mLinkStyle.normal.textColor = new Color(0x00 / 255f, 0x78 / 255f, 0xDA / 255f, 1f);
        mLinkStyle.stretchWidth = false;

        mButtonStyle = new GUIStyle(EditorStyles.miniButton);
        mButtonStyle.fontStyle = FontStyle.Bold;

        _mInitialized = true;
    }

    bool LinkLabel(GUIContent label, params GUILayoutOption[] options)
    {
        var position = GUILayoutUtility.GetRect(label, LinkStyle, options);

        Handles.BeginGUI();
        Handles.color = LinkStyle.normal.textColor;
        Handles.DrawLine(new Vector3(position.xMin, position.yMax), new Vector3(position.xMax, position.yMax));
        Handles.color = Color.white;
        Handles.EndGUI();

        EditorGUIUtility.AddCursorRect(position, MouseCursor.Link);

        return GUI.Button(position, label, LinkStyle);
    }
}
