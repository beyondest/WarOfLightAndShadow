using System;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using SparFlame.Components.General;
using SparFlame.Components.MainGameplay;
using SparFlame.Core.Utils;
using UnityEngine;

namespace SparFlame.Database
{
    [CreateAssetMenu(fileName = "EcoDatabase", menuName = "GameData/EcoDatabase", order = 0)]
    public class EcoDatabaseSo : ScriptableObject
    {
        [TableList] public List<EcoDataItem> items;

        public EcoDataItem GetEcoDataItemByEcoType(EcoType ecoType)
        {
            foreach (var item in items)
            {
                if (item.ecoType == ecoType)
                {
                    return item;
                }
            }

            return new EcoDataItem
            {
                ecoType = EcoType.Unknown,
                locationDescription = $"Not find eco type {ecoType} in eco database",
                buffDescription = "",
                name = $"Wrong eco type {ecoType}"
            };
        }

        /*private class LoadingGridInfoWithIndex
        {
            public int Index;
            public LoadingGridInfo Info;
        }*/

        public float mapSize = 300f;

        /*
        [Button("生成九宫格中心点", ButtonSizes.Large)]
        public void GenerateGridCenters()
        {
            if (items == null || items.Count == 0)
            {
                Debug.LogError("Eco database is empty. Please add eco data items first.");
                return;
            }

            var tmpInfos = new List<LoadingGridInfoWithIndex>();

            var loadingGridInfos = items[0].loadingGridInfos;
            loadingGridInfos.Clear();

            float cellSize = mapSize / 3f;

            // 生成 3x3 格子的中心点坐标
            Vector3[,] centers = new Vector3[3, 3];
            for (int row = 0; row < 3; row++) // Z 方向
            {
                for (int col = 0; col < 3; col++) // X 方向
                {
                    float centerX = (col + 0.5f) * cellSize;
                    float centerZ = (row + 0.5f) * cellSize;
                    centers[row, col] = new Vector3(centerZ, 0f, centerX);
                }
            }

            // 按照 左上角=0，正上方=1，顺时针 排列
            // 序号分布（3x3格子）：
            //  0  1  2
            //  7  4  3
            //  6  5  ?（从左上角开始顺时针，其实中间是 4，剔除）

            int[,] indexMap =
            {
                { 0, 1, 2 },
                { 7, -1, 3 },
                { 6, 5, 4 }
            };

            for (int row = 0; row < 3; row++)
            {
                for (int col = 0; col < 3; col++)
                {
                    int idx = indexMap[row, col];
                    if (idx >= 0) // 跳过中心 -1
                    {
                        tmpInfos.Add(new LoadingGridInfoWithIndex
                        {
                            Index = idx,
                            Info = new LoadingGridInfo
                            {
                                outerCenter = centers[row, col],
                                innerCenter = centers[row, col],
                                outerSize = cellSize,
                                innerSize = cellSize,
                            }
                        });
                    }
                }
            }

            // 确保 list 按 index 排序
            tmpInfos.Sort((a, b) => a.Index.CompareTo(b.Index));
            foreach (var info in tmpInfos)
            {
                loadingGridInfos.Add(info.Info);
            }
        }

        [Button("Check config validity")]
        public void CheckConfigValidity()
        {
            for (int i = 0; i < items.Count; i++)
            {
                var item = items[i];
                if (item.loadingGridInfos is not { Count: 8 })
                    throw new ArgumentException($"Loading grid infos must be 8. Wrong : {i}");
                foreach (var info in item.loadingGridInfos)
                {
                    if (math.abs(info.innerSize) < 0.001f || math.abs(info.outerSize) < 0.001f)
                    {
                        throw new ArgumentException(
                            $"Inner or outer size of loading grid info must be greater than 0. Wrong : {i}");
                    }
                }
            }
        }*/
    }

    [Serializable]
    public class EcoDataItem
    {
        [VerticalGroup("General")] public string name;

        [VerticalGroup("General")] public EcoType ecoType;

        [VerticalGroup("Description")] [TextArea(3, 10)]
        public string locationDescription;

        [TextArea(3, 10)] public string buffDescription;

        
        [VerticalGroup("Gameplay")] 
        public float camMaxCoordinate;
        
        [VerticalGroup("Gameplay")]
        public float camMinCoordinate;
        
        [VerticalGroup("Loading Positions")] public LoadingPositionInfo loadingPositionInfo;

        public SceneGroup ecoEnvSceneGroup;
    }
}