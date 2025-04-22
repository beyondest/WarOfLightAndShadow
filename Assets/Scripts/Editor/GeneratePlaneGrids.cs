using SparFlame.GamePlaySystem.Movement;
using UnityEditor;
using UnityEngine;

namespace Editor
{
    public class PlaneGridGenerator : EditorWindow
    {
        private GameObject _planePrefab;
        private int _rows = 4;
        private int _columns = 4;
        private float _tileSize = 30f;
        private string _parentName = "PlaneGrid";

        [MenuItem("Tools/Generate Plane Grid")]
        public static void ShowWindow()
        {
            GetWindow<PlaneGridGenerator>("Plane Grid Generator");
        }

        private void OnGUI()
        {
            GUILayout.Label("Grid Settings", EditorStyles.boldLabel);
            _planePrefab = (GameObject)EditorGUILayout.ObjectField("Plane Prefab", _planePrefab, typeof(GameObject), false);
            _rows = EditorGUILayout.IntField("Rows", _rows);
            _columns = EditorGUILayout.IntField("Columns", _columns);
            _tileSize = EditorGUILayout.FloatField("Tile Size", _tileSize);
            _parentName = EditorGUILayout.TextField("Parent Object Name", _parentName);

            if (GUILayout.Button("Generate Grid"))
            {
                if (_planePrefab == null)
                {
                    Debug.LogError("Please assign a plane prefab.");
                    return;
                }

                GenerateGrid();
            }
        }

        private void GenerateGrid()
        {
            GameObject parent = new GameObject(_parentName);

            for (int x = 0; x < _rows; x++)
            {
                for (int z = 0; z < _columns; z++)
                {
                    Vector3 pos = new Vector3(x * _tileSize, 0, z * _tileSize);
                    GameObject plane = (GameObject)PrefabUtility.InstantiatePrefab(_planePrefab);
                    plane.transform.position = pos;
                    plane.transform.SetParent(parent.transform);
                    plane.name = $"Plane_{x}_{z}";
                }
            }

            Debug.Log($"Generated {_rows * _columns} planes under '{_parentName}'");

            CenterBoxColliderOnGrid(_rows, _columns, _tileSize);
        }

        private void CenterBoxColliderOnGrid(int rows, int columns, float tileSize)
        {
            var marker = FindAnyObjectByType<NavMeshController>();
            if (marker == null)
            {
                Debug.LogWarning("No GameObject with CenterMarkerComponent found in scene.");
                return;
            }

            // 计算中心点
            float centerX = ((rows - 1) * tileSize) / 2f;
            float centerZ = ((columns - 1) * tileSize) / 2f;
            Vector3 centerPos = new Vector3(centerX, 0, centerZ);
            marker.transform.position = centerPos;

            foreach (var collider in marker.GetComponentsInChildren<BoxCollider>())
            {
                // var collider = marker.GetComponent<BoxCollider>();
                // if (collider == null)
                // {
                //     collider = marker.gameObject.AddComponent<BoxCollider>();
                // }

                collider.size = new Vector3(rows * tileSize, 1f, columns * tileSize);
                collider.center = Vector3.zero;
            }
            

            Debug.Log("Center object positioned and collider resized.");
        }
    }
}
