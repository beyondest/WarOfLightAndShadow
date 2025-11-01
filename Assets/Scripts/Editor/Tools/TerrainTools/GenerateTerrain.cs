using UnityEngine;
using UnityEditor;
using System.IO;

namespace Editor
{
    public class TerrainGridGenerator : EditorWindow
    {
        private int gridSize = 3; // 3x3、5x5 等奇数
        private int terrainSize = 512;
        private int terrainHeight = 600;
        private TerrainData terrainDataTemplate;

        private string saveFolder = "Assets/Resources/TerrainDatas";

        [MenuItem("Tools/TerrainTools/Terrain Grid Generator")]
        private static void ShowWindow()
        {
            GetWindow<TerrainGridGenerator>("Terrain Grid Generator");
        }

        private void OnGUI()
        {
            GUILayout.Label("中心对齐 Terrain 网格生成器", EditorStyles.boldLabel);

            gridSize = EditorGUILayout.IntSlider("网格大小（奇数）", gridSize, 1, 11);
            if (gridSize % 2 == 0) gridSize += 1;

            terrainSize = EditorGUILayout.IntField("每块 Terrain 宽度", terrainSize);
            terrainHeight = EditorGUILayout.IntField("Terrain 高度", terrainHeight);
            terrainDataTemplate = (TerrainData)EditorGUILayout.ObjectField("可选 TerrainData 模板", terrainDataTemplate,
                typeof(TerrainData), false);

            EditorGUILayout.Space(5);
            GUILayout.Label("保存设置", EditorStyles.boldLabel);
            saveFolder = EditorGUILayout.TextField("TerrainData 保存路径", saveFolder);

            if (GUILayout.Button("生成 Terrain 网格"))
            {
                GenerateTerrainGrid();
            }
        }

        private void GenerateTerrainGrid()
        {
            if (!Directory.Exists(saveFolder))
            {
                Directory.CreateDirectory(saveFolder);
            }

            float totalSize = gridSize * terrainSize;
            float startX = -totalSize / 2f;
            float startZ = -totalSize / 2f;

            for (int x = 0; x < gridSize; x++)
            {
                for (int z = 0; z < gridSize; z++)
                {
                    Vector3 position = new Vector3(
                        startX + x * terrainSize,
                        0,
                        startZ + z * terrainSize
                    );

                    // 创建 TerrainData
                    TerrainData terrainData = terrainDataTemplate != null
                        ? Instantiate(terrainDataTemplate)
                        : new TerrainData();
                    terrainData.name = $"TerrainData_{x}_{z}";
                    terrainData.heightmapResolution = 513;
                    terrainData.size = new Vector3(terrainSize, terrainHeight, terrainSize);

                    // 设置 Detail Resolution（否则无法 paint）
                    int detailResolution = 512;
                    int resolutionPerPatch = 16;
                    terrainData.SetDetailResolution(detailResolution, resolutionPerPatch);

                    // 保存 TerrainData 为 .asset 文件
                    string assetPath = $"{saveFolder}/TerrainData_{x}_{z}.asset";
                    AssetDatabase.CreateAsset(terrainData, assetPath);

                    // 创建 Terrain 对象
                    GameObject terrainGO = Terrain.CreateTerrainGameObject(terrainData);
                    terrainGO.transform.position = position;
                    terrainGO.name = $"Terrain_{x}_{z}";
                }
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"成功生成 {gridSize}x{gridSize} 的 Terrain，并将所有 TerrainData 保存到：{saveFolder}");
        }
    }
}