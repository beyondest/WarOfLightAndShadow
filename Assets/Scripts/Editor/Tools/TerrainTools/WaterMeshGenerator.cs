using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

namespace Editor
{
    public class IrregularCircleGeneratorEditor : EditorWindow
    {
        int count = 100;
        Vector2 radiusRange = new Vector2(2f, 5f);
        int segments = 32;
        float noiseStrength = 0.5f;
        Material previewMaterial;
        string parentName = "Generated_Circles";
        string meshSavePath = "Assets//Misc/Export/";

        [MenuItem("Tools/TerrainTools/Irregular Circle Generator")]
        static void Init()
        {
            GetWindow<IrregularCircleGeneratorEditor>("Circle Mesh Generator");
        }

        void OnGUI()
        {
            count = EditorGUILayout.IntField("Count", count);
            radiusRange = EditorGUILayout.Vector2Field("Radius Range", radiusRange);
            segments = EditorGUILayout.IntSlider("Segments", segments, 8, 128);
            noiseStrength = EditorGUILayout.Slider("Noise Strength", noiseStrength, 0f, 2f);
            previewMaterial =
                (Material)EditorGUILayout.ObjectField("Material (Optional)", previewMaterial, typeof(Material), false);
            meshSavePath = EditorGUILayout.TextField("Mesh Save Path", meshSavePath);
            parentName = EditorGUILayout.TextField("Parent Object Name", parentName);

            if (GUILayout.Button("Generate"))
            {
                GenerateCircles();
            }
        }

        void GenerateCircles()
        {
            GameObject parent = new GameObject(parentName);

            for (int i = 0; i < count; i++)
            {
                float radius = Random.Range(radiusRange.x, radiusRange.y);
                Mesh mesh = GenerateMesh(radius, segments, noiseStrength);

                GameObject obj = new GameObject("Circle_" + i);
                obj.transform.SetParent(parent.transform);
                obj.transform.position = new Vector3(i % 10 * 10, 0, i / 10 * 10); // 排列位置

                var mf = obj.AddComponent<MeshFilter>();
                var mr = obj.AddComponent<MeshRenderer>();
                mf.sharedMesh = mesh;
                if (previewMaterial != null) mr.sharedMaterial = previewMaterial;

                // 保存 Mesh Asset
                string filename = $"{meshSavePath}circle_{i}.asset";
                AssetDatabase.CreateAsset(mesh, filename);
            }

            AssetDatabase.SaveAssets();
            Selection.activeGameObject = parent;
        }

        Mesh GenerateMesh(float radius, int segs, float noise)
        {
            Mesh mesh = new Mesh();
            List<Vector3> verts = new List<Vector3> { Vector3.zero };
            List<int> tris = new List<int>();

            for (int i = 0; i <= segs; i++)
            {
                float angle = 2 * Mathf.PI * i / segs;
                float r = radius + Random.Range(-noise, noise);
                verts.Add(new Vector3(Mathf.Cos(angle) * r, 0, Mathf.Sin(angle) * r));
            }

            for (int i = 1; i < segs; i++)
            {
                tris.Add(0);
                tris.Add(i);
                tris.Add(i + 1);
            }

            tris.Add(0);
            tris.Add(segs);
            tris.Add(1);

            mesh.SetVertices(verts);
            mesh.SetTriangles(tris, 0);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }
    }
}