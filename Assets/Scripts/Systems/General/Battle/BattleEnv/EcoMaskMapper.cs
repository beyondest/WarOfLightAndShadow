using System.Collections.Generic;
using Sirenix.OdinInspector;
using SparFlame.Components.MainGameplay;
using UnityEngine;

namespace SparFlame.Systems.General.Battle
{
    [ExecuteAlways]
    public class EcoMaskMapper : MonoBehaviour
    {
        [Header("Mask & Map settings")] [SerializeField]
        private Texture2D ecoMask; // 导入时 Read/Write 启用，Filter Mode = Point

        [SerializeField] private float mapSize = 1000f; // 地图在世界单位中的边长（正方形）
        [SerializeField] private Vector3 mapOrigin = Vector3.zero; // mask 左下角对应的 world pos (0,0,0)

        [Header("Color mapping (assign exact colors or use the defaults)")] [SerializeField]
        private List<EcoColorEntry> mappings = new List<EcoColorEntry>();

        // 容错阈值（0 = 精确匹配；>0 时会找最近颜色）
        [SerializeField] private int colorTolerance = 0; // 推荐 0


        public static EcoMaskMapper Instance;

 
        /// <summary>
        /// 主函数：输入世界坐标 (x,y,z) ，返回 EcoType
        /// 假定 mask 左下像素对应 mapOrigin，mask 覆盖一个正方形 mapSize x mapSize（XZ 平面）
        /// </summary>
        public EcoType GetEcoAtPosition(Vector3 worldPos)
        {
            if (ecoMask == null) return EcoType.Unknown;

            // 将 worldPos 映射到 [0,1] UV
            float u = (worldPos.x - mapOrigin.x) / mapSize;
            float v = (worldPos.z - mapOrigin.z) / mapSize;

            // 超出地图范围时返回 Unknown（可以改成 clamp）
            if (u < 0f || u > 1f || v < 0f || v > 1f)
                return EcoType.Unknown;

            int px = Mathf.Clamp(Mathf.FloorToInt(u * ecoMask.width), 0, ecoMask.width - 1);
            int py = Mathf.Clamp(Mathf.FloorToInt(v * ecoMask.height), 0, ecoMask.height - 1);

            Color32 pcol = ecoMask.GetPixel(px, py); // 精确像素读取（Filter = Point 推荐）

            // 精确匹配
            if (_color2Eco.TryGetValue(pcol, out EcoType eco))
            {
                return eco;
            }

            // 如果启用容错，寻找最近颜色
            if (colorTolerance > 0)
            {
                EcoType nearest = EcoType.Unknown;
                int bestDist = int.MaxValue;
                foreach (var kv in _color2Eco)
                {
                    int dist = ColorDistanceSquared(pcol, kv.Key);
                    if (dist < bestDist)
                    {
                        bestDist = dist;
                        nearest = kv.Value;
                    }
                }

                // 根据阈值决定是否接受 nearest（阈值为 colorTolerance^2）
                if (bestDist <= colorTolerance * colorTolerance)
                    return nearest;
            }

            return EcoType.Unknown;
        }


        [Button]
        public List<Color32> ValidateMaskColors()
        {
            List<Color32> unknownColors = new List<Color32>();
            if (ecoMask == null) return unknownColors;

            // 遍历像素（注意大图会消耗时间）
            Color32[] pixels = ecoMask.GetPixels32();
            HashSet<Color32> seen = new HashSet<Color32>();
            foreach (var p in pixels)
            {
                if (seen.Contains(p)) continue;
                seen.Add(p);
                if (!_color2Eco.ContainsKey(p))
                {
                    unknownColors.Add(p);
                    if (unknownColors.Count >= 64) break; // 限制输出
                }
            }

            // 在 Editor log 打印部分信息
            Debug.Log(
                $"EcoMaskMapper ValidateMaskColors: unique colors in mask = {seen.Count}, unknown colors = {unknownColors.Count}");
            foreach (var c in unknownColors)
                Debug.Log($"  Unknown color: #{c.r:X2}{c.g:X2}{c.b:X2}");
            return unknownColors;
        }

        #region EventFunctions

        private void Awake()
        {
            if (!Instance)
            {
                Instance = this;
            }
            else
            {
                Destroy(gameObject);
                return;
            }
            BuildDictionary();
            
        }

        private void OnValidate()
        {
            BuildDictionary();
        }

        #endregion
        private void BuildDictionary()
        {
            _color2Eco.Clear();
            if (mappings == null) return;
            foreach (var e in mappings)
            {
                Color32 c32 = (Color32)e.color;
                if (!_color2Eco.ContainsKey(c32))
                    _color2Eco.Add(c32, e.eco);
            }
        }

        private int ColorDistanceSquared(Color32 a, Color32 b)
        {
            int dr = a.r - b.r;
            int dg = a.g - b.g;
            int db = a.b - b.b;
            return dr * dr + dg * dg + db * db;
        }


        // 内部字典：Color32 -> EcoType
        private readonly Dictionary<Color32, EcoType> _color2Eco = new Dictionary<Color32, EcoType>();
    }
}