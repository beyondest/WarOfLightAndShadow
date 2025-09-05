using UnityEngine;
using UnityEditor;
using Unity.Physics.Authoring;

public class PhysicsShapeConverterWindow : EditorWindow
{
    private readonly string[] categoryNames = new string[32];
    private readonly bool[] belongsToMask = new bool[32];
    private readonly bool[] collidesWithMask = new bool[32];

    private PhysicsCategoryNames categoryNamesAsset;

    private Vector2 scrollPos;
    private bool showBelongsTo = true;
    private bool showCollidesWith = true;

    [MenuItem("Tools/TerrainTools/Convert MeshCollider to PhysicsShape (With Category)")]
    public static void ShowWindow()
    {
        var window = GetWindow<PhysicsShapeConverterWindow>(true, "Convert to PhysicsShape");
        window.minSize = new Vector2(400, 600);
        window.Show();
    }

    private void OnEnable()
    {
        LoadCategoryNamesAsset();
        for (int i = 0; i < 32; i++)
        {
            categoryNames[i] = string.IsNullOrEmpty(categoryNamesAsset.CategoryNames[i])
                ? $"Category{i:D2}"
                : categoryNamesAsset.CategoryNames[i];
        }
    }

    private void OnGUI()
    {
        GUILayout.Label("Physics Category Tags", EditorStyles.boldLabel);
        scrollPos = EditorGUILayout.BeginScrollView(scrollPos);

        // ---------------- Belongs To ----------------
        showBelongsTo = EditorGUILayout.Foldout(showBelongsTo, "Belongs To", true);
        if (showBelongsTo)
        {
            DrawToggleGroup(belongsToMask, "BelongsTo");
            DrawSelectButtons(belongsToMask);
        }

        GUILayout.Space(10);

        // ---------------- Collides With ----------------
        showCollidesWith = EditorGUILayout.Foldout(showCollidesWith, "Collides With", true);
        if (showCollidesWith)
        {
            DrawToggleGroup(collidesWithMask, "CollidesWith");
            DrawSelectButtons(collidesWithMask);
        }

        GUILayout.Space(20);
        if (GUILayout.Button("Convert Selected Objects", EditorStyles.miniButton))
        {
            ConvertSelectedObjects();
        }

        EditorGUILayout.EndScrollView();
    }

    private void DrawToggleGroup(bool[] mask, string label)
    {
        const int columns = 2;
        int rows = Mathf.CeilToInt(32f / columns);

        for (int row = 0; row < rows; row++)
        {
            EditorGUILayout.BeginHorizontal();
            for (int col = 0; col < columns; col++)
            {
                int index = row + col * rows;
                if (index < 32)
                {
                    mask[index] = EditorGUILayout.ToggleLeft(categoryNames[index], mask[index], GUILayout.Width(180));
                }
            }
            EditorGUILayout.EndHorizontal();
        }
    }

    private void DrawSelectButtons(bool[] mask)
    {
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Select All", EditorStyles.miniButtonLeft))
        {
            SetAll(mask, true);
        }
        if (GUILayout.Button("Deselect All", EditorStyles.miniButtonRight))
        {
            SetAll(mask, false);
        }
        EditorGUILayout.EndHorizontal();
    }

    private void SetAll(bool[] mask, bool value)
    {
        for (int i = 0; i < mask.Length; i++)
        {
            mask[i] = value;
        }
    }

    private void ConvertSelectedObjects()
    {
        var selectedObjects = Selection.gameObjects;
        if (selectedObjects.Length == 0)
        {
            Debug.LogWarning("No GameObjects selected.");
            return;
        }

        var belongsToTags = new PhysicsCategoryTags();
        var collidesWithTags = new PhysicsCategoryTags();

        for (int i = 0; i < 32; i++)
        {
            belongsToTags[i] = belongsToMask[i];
            collidesWithTags[i] = collidesWithMask[i];
        }

        int convertedCount = 0;
        foreach (var root in selectedObjects)
        {
            foreach (var meshCollider in root.GetComponentsInChildren<MeshCollider>(true))
            {
                if (!meshCollider.sharedMesh)
                    continue;

                var go = meshCollider.gameObject;
                var physicsShape = go.GetComponent<PhysicsShapeAuthoring>();
                if (!physicsShape)
                {
                    physicsShape = Undo.AddComponent<PhysicsShapeAuthoring>(go);
                }

                physicsShape.SetMesh(meshCollider.sharedMesh);
                physicsShape.BelongsTo = belongsToTags;
                physicsShape.CollidesWith = collidesWithTags;

                Undo.DestroyObjectImmediate(meshCollider);
                convertedCount++;
            }
        }

        Debug.Log($"Conversion complete. Converted {convertedCount} MeshColliders.");
    }

    private void LoadCategoryNamesAsset()
    {
        string[] guids = AssetDatabase.FindAssets("t:PhysicsCategoryNames");
        if (guids.Length > 0)
        {
            string path = AssetDatabase.GUIDToAssetPath(guids[0]);
            categoryNamesAsset = AssetDatabase.LoadAssetAtPath<PhysicsCategoryNames>(path);
        }
        else
        {
            Debug.LogWarning("PhysicsCategoryNames asset not found in project.");
            categoryNamesAsset = CreateInstance<PhysicsCategoryNames>();
        }
    }
}
