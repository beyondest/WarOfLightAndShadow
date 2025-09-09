using SparFlame.Systems.Map;
using SparFlame.Systems.SubGameplay.Movement;
using UnityEditor;
using UnityEngine;

namespace Editor
{
    public class PlaneGridGenerator : EditorWindow
    {
        private GameObject _planePrefab;
        private GameObject _slopePrefab;
        private GameObject _cornerPrefab; // 新增：用于角落的 Prefab

        private int _rows = 100;
        private int _columns = 100;
        private float _tileSize = 6f;
        private string _parentName = "PlaneGrid";
        private bool _ifGenerateSlopesAndCorners = true;
        private float _slopeTileSize = 30;
        private int _edgeCount = 1;

        [MenuItem("Tools/TerrainTools/Generate Edge And Corners")]
        public static void ShowWindow()
        {
            GetWindow<PlaneGridGenerator>("Plane Grid Generator");
        }

        private void OnGUI()
        {
            GUILayout.Label("Grid Settings", EditorStyles.boldLabel);
            _planePrefab =
                (GameObject)EditorGUILayout.ObjectField("Plane Prefab", _planePrefab, typeof(GameObject), false);
            _slopePrefab =
                (GameObject)EditorGUILayout.ObjectField("Slope Prefab", _slopePrefab, typeof(GameObject), false);
            _cornerPrefab =
                (GameObject)EditorGUILayout.ObjectField("Corner Prefab", _cornerPrefab, typeof(GameObject), false);
            _ifGenerateSlopesAndCorners =
                EditorGUILayout.Toggle("If Generate Slopes And Corners", _ifGenerateSlopesAndCorners);

            _rows = EditorGUILayout.IntField("Rows", _rows);
            _columns = EditorGUILayout.IntField("Columns", _columns);
            _tileSize = EditorGUILayout.FloatField("Main Tile Size", _tileSize);
            if (_ifGenerateSlopesAndCorners)
            {
                _slopeTileSize = EditorGUILayout.FloatField("Slope Tile Size", _slopeTileSize);
                _edgeCount = EditorGUILayout.IntField("Edge Width (in Tiles)", _edgeCount);
            }

            _parentName = EditorGUILayout.TextField("Parent Object Name", _parentName);

            if (GUILayout.Button("Generate Grid"))
            {
                if (!_planePrefab ||
                    (_ifGenerateSlopesAndCorners && (!_slopePrefab || !_cornerPrefab)))
                {
                    Debug.LogError("Please assign all required prefabs.");
                    return;
                }

                // 验证尺寸合法性
                if (_ifGenerateSlopesAndCorners)
                {
                    float totalMainWidth = _columns * _tileSize;
                    float totalMainHeight = _rows * _tileSize;
                    float edgeTotalSize = _edgeCount * _slopeTileSize;

                    if (!Mathf.Approximately(totalMainWidth % _slopeTileSize, 0f) ||
                        !Mathf.Approximately(totalMainHeight % _slopeTileSize, 0f))
                    {
                        Debug.LogError("SlopeTileSize must evenly divide total main grid width and height.");
                        return;
                    }

                    if (!Mathf.Approximately(edgeTotalSize * 2 + totalMainWidth,
                            Mathf.Round(edgeTotalSize * 2 + totalMainWidth)) ||
                        !Mathf.Approximately(edgeTotalSize * 2 + totalMainHeight,
                            Mathf.Round(edgeTotalSize * 2 + totalMainHeight)))
                    {
                        Debug.LogError(
                            "Combined slope tiles must exactly surround the main grid. Check EdgeCount and SlopeTileSize.");
                        return;
                    }
                }

                GenerateGrid();
            }
        }


        private void GenerateGrid()
        {
            GameObject parent = new GameObject(_parentName);

            // 主格子
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

            if (_ifGenerateSlopesAndCorners)
            {
                GenerateSlopesAndCorners(parent.transform);
            }

            Debug.Log($"Generated {_rows * _columns} planes with optional slopes/corners under '{_parentName}'");

            CenterBoxColliderOnGrid(_rows, _columns, _tileSize); // 若你有碰撞器居中逻辑
        }

        private void GenerateSlopesAndCorners(Transform parent)
        {
            float halfMainTile = 0.5f * _tileSize;
            float halfSlopeTile = 0.5f * _slopeTileSize;

            float mainWidth = _columns * _tileSize;
            float mainHeight = _rows * _tileSize;

            float mainStartX = 0f;
            float mainStartZ = 0f;

            // 计算边缘 slope 起始位置（基于中心对齐逻辑）
            float slopeStartX = mainStartX - halfMainTile - halfSlopeTile;
            float slopeStartZ = mainStartZ - halfMainTile - halfSlopeTile;
            float slopeEndX = mainStartX + mainWidth - halfMainTile + halfSlopeTile;
            float slopeEndZ = mainStartZ + mainHeight - halfMainTile + halfSlopeTile;

            for (int i = 0; i < _edgeCount; i++)
            {
                float offset = i * _slopeTileSize;

                // 左右边
                for (int row = 0; row < _rows; row++)
                {
                    float z = mainStartZ + row * _tileSize;

                    float xLeft = slopeStartX - offset;
                    float xRight = slopeEndX + offset;

                    CreateSlope(new Vector3(xLeft, 0, z), Quaternion.Euler(0, 90, 0), parent);
                    CreateSlope(new Vector3(xRight, 0, z), Quaternion.Euler(0, -90, 0), parent);
                }

                // 上下边
                for (int col = 0; col < _columns; col++)
                {
                    float x = mainStartX + col * _tileSize;

                    float zBottom = slopeStartZ - offset;
                    float zTop = slopeEndZ + offset;

                    CreateSlope(new Vector3(x, 0, zBottom), Quaternion.identity, parent);
                    CreateSlope(new Vector3(x, 0, zTop), Quaternion.Euler(0, 180, 0), parent);
                }
            }

            // 四角
            for (int dx = 0; dx < _edgeCount; dx++)
            {
                for (int dz = 0; dz < _edgeCount; dz++)
                {
                    float xL = slopeStartX - dx * _slopeTileSize;
                    float xR = slopeEndX + dx * _slopeTileSize;
                    float zB = slopeStartZ - dz * _slopeTileSize;
                    float zT = slopeEndZ + dz * _slopeTileSize;

                    CreateCorner(new Vector3(xL, 0, zB), parent); // Bottom-left
                    CreateCorner(new Vector3(xR, 0, zB), parent); // Bottom-right
                    CreateCorner(new Vector3(xL, 0, zT), parent); // Top-left
                    CreateCorner(new Vector3(xR, 0, zT), parent); // Top-right
                }
            }
        }


        /*
        private void GenerateSlopesAndCorners(Transform parent)
        {
            float halfMainTile = 0.5f * _tileSize;
            float halfSlopeTile = 0.5f * _slopeTileSize;

            float mainWidth = _columns * _tileSize;
            float mainHeight = _rows * _tileSize;

// Main grid 左上角 tile 的边缘坐标
            float mainStartX = 0 - halfMainTile;
            float mainStartZ = 0 - halfMainTile;

// slope 区域从 main grid 边缘外开始，一直扩展到外圈 edgeCount
            float startX = mainStartX - _edgeCount * _slopeTileSize + halfSlopeTile;
            float startZ = mainStartZ - _edgeCount * _slopeTileSize + halfSlopeTile;
            float endX = mainStartX + mainWidth + _edgeCount * _slopeTileSize - halfSlopeTile;
            float endZ = mainStartZ + mainHeight + _edgeCount * _slopeTileSize - halfSlopeTile;


            // 四个边
            for (int i = 0; i < _edgeCount; i++)
            {
                float offset = i * _slopeTileSize;

                // Left / Right Columns
                for (float z = 0; z < mainHeight; z += _slopeTileSize)
                {
                    Vector3 posLeft = new Vector3(startX + offset, 0, z);
                    Vector3 posRight = new Vector3(endX - offset - _slopeTileSize, 0, z);

                    CreateSlope(posLeft, Quaternion.Euler(0, 90, 0), parent);
                    CreateSlope(posRight, Quaternion.Euler(0, -90, 0), parent);
                }

                // Top / Bottom Rows
                for (float x = 0; x < mainWidth; x += _slopeTileSize)
                {
                    Vector3 posBottom = new Vector3(x, 0, startZ + offset);
                    Vector3 posTop = new Vector3(x, 0, endZ - offset - _slopeTileSize);

                    CreateSlope(posBottom, Quaternion.identity, parent);
                    CreateSlope(posTop, Quaternion.Euler(0, 180, 0), parent);
                }
            }

            // 四角
            for (int dx = 0; dx < _edgeCount; dx++)
            {
                for (int dz = 0; dz < _edgeCount; dz++)
                {
                    float xL = startX + dx * _slopeTileSize;
                    float xR = endX - (dx + 1) * _slopeTileSize;
                    float zB = startZ + dz * _slopeTileSize;
                    float zT = endZ - (dz + 1) * _slopeTileSize;

                    CreateCorner(new Vector3(xL, 0, zB), parent); // Bottom-left
                    CreateCorner(new Vector3(xR, 0, zB), parent); // Bottom-right
                    CreateCorner(new Vector3(xL, 0, zT), parent); // Top-left
                    CreateCorner(new Vector3(xR, 0, zT), parent); // Top-right
                }
            }
        }
        */

        private void CreateSlope(Vector3 pos, Quaternion rotation, Transform parent)
        {
            GameObject slope = (GameObject)PrefabUtility.InstantiatePrefab(_slopePrefab);
            slope.transform.position = pos;
            slope.transform.rotation = rotation;
            slope.transform.SetParent(parent);
        }

        private void CreateCorner(Vector3 pos, Transform parent)
        {
            GameObject corner = (GameObject)PrefabUtility.InstantiatePrefab(_cornerPrefab);
            corner.transform.position = pos;
            corner.transform.rotation = Quaternion.identity;
            corner.transform.SetParent(parent);
        }


        private void CenterBoxColliderOnGrid(int rows, int columns, float tileSize)
        {
            // Replace nav mesh controller
            var marker = FindAnyObjectByType<NavMeshController>();
            if (!marker)
            {
                Debug.LogWarning("No GameObject with CenterMarkerComponent found in scene.");
                return;
            }

            float centerX = ((rows - 1) * tileSize) / 2f;
            float centerZ = ((columns - 1) * tileSize) / 2f;
            Vector3 centerPos = new Vector3(centerX, 0, centerZ);
            marker.transform.position = centerPos;

            foreach (var collider in marker.GetComponentsInChildren<BoxCollider>())
            {
                collider.size = new Vector3(rows * tileSize, 1f, columns * tileSize);
                collider.center = Vector3.zero;
            }

            Debug.Log("NavMeshController object positioned and collider resized.");
            
            // Reset map info authoring
            var mapInfoAuthoring = FindAnyObjectByType<MiniMapSystemAuthoring>();
            if (mapInfoAuthoring)
            {
                var original = mapInfoAuthoring.mapInfo;
                original.tileSize = tileSize;
                original.outerSquareSize = rows * tileSize;
                mapInfoAuthoring.mapInfo = original;
                EditorUtility.SetDirty(mapInfoAuthoring);
                UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(mapInfoAuthoring.gameObject.scene);
                Debug.Log("MapInfoAuthoring reset and reBake");
            }
            else
            {
                Debug.LogWarning("You should manually change MapInfoAuthoring in subscene");
            }

        }
    }
}