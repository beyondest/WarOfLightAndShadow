using System;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using SparFlame.Components.General;
using SparFlame.UI.General;
using TMPro;
using Unity.Entities;
using UnityEngine;
using UnityEngine.UI;

// ReSharper disable UseIndexFromEndExpression

namespace SparFlame.UI.SubGameplay
{
    public class HintWindow : MonoBehaviour, MultiSlotWindowUtils.IUIWindow
    {
        // Config


        [SerializeField] private GameObject panel;
        [SerializeField] private float hintXWhenConstructWindowOpen;
        [SerializeField, AssetsOnly] private GameObject hintPrefab;
        [SerializeField] private int maxHints = 5; // 最大条数
        [SerializeField] private float maxDuration = 5f; // 残留最大时间（秒）
        [SerializeField] private float fadeDuration = 1f; // 淡出动画时长
        [SerializeField] private List<HintTypeConfig> colorConfigs;
        [SerializeField] private RectTransform topAnchor;
        [SerializeField] private float slideDownTime;


        public static HintWindow Instance;

        

        // Internal Data
        private EntityManager _em;
        private EntityQuery _gamingTag;
        private EntityQuery _hintsInfo;
        private EntityQuery _timeData;
        private RectTransform _hintWindowRect;
        private float _hintOriginalX;

        private readonly List<HintEntry> _hintEntries = new();
        private readonly Dictionary<HintType, Color> _hintColors = new();
        private readonly Dictionary<HintType, Toggle> _hintTypeToggles = new();
        private readonly HashSet<HintType> activeTypes = new();

        #region EventFunctions

        private void Awake()
        {
            if (!Instance)
                Instance = this;
            else
            {
                Destroy(gameObject);
            }
        }

        private void Start()
        {
            _em = World.DefaultGameObjectInjectionWorld.EntityManager;
            _gamingTag = _em.CreateEntityQuery(typeof(SubGamingTag));
            _hintsInfo = _em.CreateEntityQuery(typeof(HintsInfo));
            _timeData = _em.CreateEntityQuery(typeof(GameTimeData));

            _hintWindowRect = panel.GetComponent<RectTransform>();
            _hintOriginalX = _hintWindowRect.anchoredPosition.x;

            foreach (var config in colorConfigs)
            {
                _hintColors.Add(config.type, config.color);
                _hintTypeToggles.Add(config.type, config.toggle);
            }

            foreach (var pair in _hintTypeToggles)
            {
                pair.Value.isOn = true;
                pair.Value.onValueChanged.AddListener((on) =>
                {
                    if (on) activeTypes.Add(pair.Key);
                    else activeTypes.Remove(pair.Key);
                });
                activeTypes.Add(pair.Key);
            }

            Show();
            SettingManager.Instance.OnToggleHintWindow +=
                enable =>
                {
                    if (enable) Show();
                    else Hide();
                };
        }


        private void Update()
        {
            if (_gamingTag.IsEmpty) return;
            UpdateDynamicInfo();
        }

        #endregion


        private void UpdateDynamicInfo()
        {
            // Update hints info
            _hintWindowRect.anchoredPosition = ConstructWindow.Instance.IsOpened()
                ? new Vector2(hintXWhenConstructWindowOpen, _hintWindowRect.anchoredPosition.y)
                : new Vector2(_hintOriginalX, _hintWindowRect.anchoredPosition.y);

            var infos = _hintsInfo.GetSingletonBuffer<HintsInfo>();
            var curTime = _timeData.GetSingleton<GameTimeData>().ElapsedTime;
            foreach (var info in infos)
            {
                if (activeTypes.Contains(info.HintType))
                {
                    AddHint(info.Content.ToString(), info.UpdateTime, info.HintType);
                }
            }

            // 每帧更新所有 hint 的目标位置
            for (var i = 0; i < _hintEntries.Count; i++)
            {
                var entry = _hintEntries[i];
                if (!entry.IsFading)
                {
                    Vector2 desiredPos = topAnchor.anchoredPosition - new Vector2(0, i * topAnchor.rect.height);
                    entry.TargetPos = desiredPos;
                    // 平滑移动（Lerp 或 SmoothDamp 都行）
                    entry.HintRect.anchoredPosition = Vector2.Lerp(entry.HintRect.anchoredPosition, desiredPos,
                        Time.deltaTime * 15f);
                }

                // 超时后淡出
                if (!entry.IsFading && curTime - entry.UpdateTime > maxDuration)
                {
                    StartCoroutine(FadeAndRemove(entry));
                }
            }
        }

        private void AddHint(string content, float updateTime, HintType type)
        {
            // 如果内容与最后一条相同且时间没变，就跳过
            if (_hintEntries.Count > 0)
            {
                var last = _hintEntries[0];
                if (last.TMP.text == content && Mathf.Approximately(last.UpdateTime, updateTime))
                    return;
            }

            // 创建新提示条
            var go = Instantiate(hintPrefab, _hintWindowRect);
            go.GetComponent<RectTransform>().anchoredPosition = topAnchor.anchoredPosition;
            var tmp = go.GetComponent<TextMeshProUGUI>();
            tmp.text = content;
            tmp.alpha = 1f;
            tmp.color = _hintColors[type];

            var entry = new HintEntry
            {
                Go = go,
                TMP = tmp,
                UpdateTime = updateTime,
                IsFading = false,
                Type = type,
                HintRect = go.GetComponent<RectTransform>()
            };
            _hintEntries.Insert(0, entry);

            // 移除超过最大数量的提示
            if (_hintEntries.Count > maxHints)
            {
                var oldEntry = _hintEntries[_hintEntries.Count - 1];
                _hintEntries.RemoveAt(_hintEntries.Count - 1);
                Destroy(oldEntry.Go);
            }
        }

        private System.Collections.IEnumerator FadeAndRemove(HintEntry entry)
        {
            entry.IsFading = true;
            float elapsed = 0f;
            float startAlpha = entry.TMP.alpha;

            while (elapsed < fadeDuration)
            {
                elapsed += Time.deltaTime;
                float alpha = Mathf.Lerp(startAlpha, 0f, elapsed / fadeDuration);
                entry.TMP.alpha = alpha;
                yield return null;
            }

            _hintEntries.Remove(entry);
            Destroy(entry.Go);
        }


        private class HintEntry
        {
            public GameObject Go;
            public TextMeshProUGUI TMP;
            public float UpdateTime;
            public bool IsFading;
            public HintType Type;
            public RectTransform HintRect;
            public Vector2 TargetPos;
        }

        [Serializable]
        public struct HintTypeConfig
        {
            public HintType type;
            public Color color;
            public Toggle toggle;
        }

        public void Show(Vector2? pos = null)
        {
            panel.SetActive(true);
        }

        public void Hide()
        {
            panel.SetActive(false);
        }

        public bool IsOpened()
        {
            return panel.activeSelf;
        }
    }
}