using System;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using SparFlame.BootStrapper;
using SparFlame.GamePlaySystem.General;
using SparFlame.GamePlaySystem.Hints;
using SparFlame.GamePlaySystem.Waves;
using SparFlame.UI.General;
using TMPro;
using Unity.Entities;
using UnityEngine;
using UnityEngine.UI;
using Image = UnityEngine.UI.Image;

// ReSharper disable UseIndexFromEndExpression

namespace SparFlame.UI.SubGameplay
{
    public class UpRightButtonWindow : MonoBehaviour
    {
        [Header("Speed Up Scale Button")] [SerializeField]
        private TMP_Text timeScaleText;

        [SerializeField] private List<float> speedUpScaleConfigList = new();


        [Header("Wave")] [SerializeField] private Image waveTimeFilled;
        [SerializeField] private Image waveTimeFilledForBlink;
        [SerializeField] private Image waveTimeBlank;
        [SerializeField] private Button nextWaveButton;
        [SerializeField] private TMP_Text waveIdx;
        [SerializeField] private TMP_Text nextWaveRemainingTime;
        [SerializeField] private List<EnemyCrystalCountToWaveColor> waveColorConfig;

        [Header("Crystal Hp")] [SerializeField]
        private Image playerCrystalFilledHp;

        [SerializeField] private Image playerCrystalBlankHp;
        [SerializeField] private Image enemyCrystalFilledHp;
        [SerializeField] private Image enemyCrystalBlankHp;

        [Header("Tutorial")] [SerializeField] private GameObject tutorialPanel;
        [SerializeField] private GameObject controlPanel;
        [SerializeField] private GameObject infoPanel;

        [Header("Hints Popup Window")] public GameObject hintsPopupWindow;
        [SerializeField] public float hintXWhenConstructWindowOpen;
        [SerializeField, AssetsOnly] private GameObject hintPrefab;
        [SerializeField] private int maxHints = 5; // 最大条数
        [SerializeField] private float maxDuration = 5f; // 残留最大时间（秒）
        [SerializeField] private float fadeDuration = 1f; // 淡出动画时长
        [SerializeField] private List<HintTypeConfig> colorConfigs;
        [SerializeField] private RectTransform topAnchor;
        [SerializeField] private float slideDownTime;
        public static UpRightButtonWindow Instance;

        public void OnClickPause()
        {
            MenuOutController.Instance.ShowPauseMenu();
            GameController.Instance.PauseGame();
        }

        public void OnClickSpeedUp()
        {
            var scale = _timeScale.GetSingletonRW<GameTimeScale>();
            if (_currentSpeedUpIndex == speedUpScaleConfigList.Count - 1)
                _currentSpeedUpIndex = 0;
            else
                _currentSpeedUpIndex++;
            var curScale = speedUpScaleConfigList[_currentSpeedUpIndex];
            Time.timeScale = curScale;
            scale.ValueRW.Value = curScale;
            timeScaleText.text = "x" + curScale.ToString("F1");
        }

        public void OnClickTutorial()
        {
            tutorialPanel.SetActive(true);
            OnClickControlButton();
        }

        public void OnClickControlButton()
        {
            controlPanel.SetActive(true);
            infoPanel.SetActive(false);
        }

        public void OnClickInfoButton()
        {
            infoPanel.SetActive(true);
            controlPanel.SetActive(false);
        }

        public void OnClickTutorialExit()
        {
            tutorialPanel.SetActive(false);
        }

        public void OnClickConstructEnter()
        {
            ConstructWindow.Instance.EnterConstruct();
        }

        public void OnClickConstructExit()
        {
            ConstructWindow.Instance.ExitConstruct();
        }

        public void OnClickNextWave()
        {
            var nextWaveRequest = _em.CreateEntity();
            _em.AddComponent<SubGameplayEntityTag>(nextWaveRequest);
            _em.AddComponent<NextWaveRequest>(nextWaveRequest);
            _em.DestroyEntity(_enemyCrystalInfo.GetSingletonEntity());
        }


        // Internal Data
        private int _currentSpeedUpIndex;
        private WaveColorType _currentColorType;
        private FactionTag _playerFaction;
        private EntityManager _em;
        private EntityQuery _timeScale;
        private EntityQuery _enemyCrystalInfo;
        private EntityQuery _gamingTag;
        private EntityQuery _waveDataQuery;
        private EntityQuery _playerCrystalInfo;
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
            if (Instance == null)
                Instance = this;
            else
            {
                Destroy(gameObject);
                return;
            }

            
        }

        private void Start()
        {
            _em = World.DefaultGameObjectInjectionWorld.EntityManager;
            _gamingTag = _em.CreateEntityQuery(typeof(SubGamingTag));
            _timeScale = _em.CreateEntityQuery(typeof(GameTimeScale));
            _enemyCrystalInfo = _em.CreateEntityQuery(typeof(EnemyCrystalInfo));
            _playerCrystalInfo = _em.CreateEntityQuery(typeof(PlayerCrystalInfo));
            _hintsInfo = _em.CreateEntityQuery(typeof(HintsInfo));
            _waveDataQuery = _em.CreateEntityQuery(typeof(GameWaveData));
            _timeData = _em.CreateEntityQuery(typeof(GameTimeData));
            controlPanel.SetActive(false);
            infoPanel.SetActive(false);
            tutorialPanel.SetActive(false);
            _hintWindowRect = hintsPopupWindow.GetComponent<RectTransform>();
            _hintOriginalX = _hintWindowRect.anchoredPosition.x;
            GameController.Instance.OnPlayerChooseFaction += factionTag => _playerFaction = factionTag;
            GeneralResourceManager.Instance.OnAllResourceLoaded += UpdateStaticInfo;
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
        }


        private void Update()
        {
            if (_gamingTag.IsEmpty) return;
            UpdateDynamicInfo();
        }

        #endregion

        private void UpdateDynamicInfo()
        {
            var enemyInfo = _enemyCrystalInfo.GetSingleton<EnemyCrystalInfo>();
            var playerInfo = _playerCrystalInfo.GetSingleton<PlayerCrystalInfo>();


            // Calculate current wave color type and switch sprite
            for (var i = 0; i < waveColorConfig.Count; i++)
            {
                var color = waveColorConfig[i];
                if (enemyInfo.TotalCount <= color.crystalCount)
                {
                    _currentColorType = color.type;
                    var tmp = waveTimeBlank.sprite;
                    if (i > 0)
                    {
                        tmp = ~_playerFaction == FactionTag.Ally
                            ? BasicUIResourceManager.Instance.DarkWaveColorTypeSprites[waveColorConfig[i - 1].type]
                            : BasicUIResourceManager.Instance.LightWaveColorTypeSprites[waveColorConfig[i - 1].type];
                    }

                    waveTimeBlank.sprite =
                        i == 0 ? BasicUIResourceManager.Instance.FactionWaveTimeBasicSprites[_playerFaction] : tmp;
                    break;
                }
            }

            switch (~_playerFaction)
            {
                case FactionTag.Ally:
                    waveTimeFilled.sprite =
                        BasicUIResourceManager.Instance.LightWaveColorTypeSprites[_currentColorType];
                    break;
                case FactionTag.Enemy:
                    waveTimeFilled.sprite = BasicUIResourceManager.Instance.DarkWaveColorTypeSprites[_currentColorType];
                    break;
            }

            waveTimeFilledForBlink.sprite = waveTimeFilled.sprite;


            // Update wave info
            var waveData = _waveDataQuery.GetSingleton<GameWaveData>();
            if (waveData.CurWaveInterval == 0f)
            {
                waveTimeFilled.fillAmount = 0f;
            }
            else
            {
                waveTimeFilled.fillAmount = 1f - waveData.NextWaveRemainingTime / waveData.CurWaveInterval;
            }

            nextWaveRemainingTime.text = UIMathMethods.FormatTime((int)waveData.NextWaveRemainingTime);
            waveIdx.text = waveData.CurWaveIndex.ToString();
            nextWaveButton.enabled = enemyInfo.TotalCount == 0;
            waveTimeFilledForBlink.enabled = enemyInfo.TotalCount == 0;


            // Update hp info
            enemyCrystalFilledHp.enabled = enemyInfo.InSightValidCount != 0;
            enemyCrystalBlankHp.enabled = enemyInfo.InSightValidCount != 0;
            // Update crystal hp info
            if (playerInfo.MaxTotalHp != 0f)
                playerCrystalFilledHp.fillAmount = playerInfo.CurTotalHp / playerInfo.MaxTotalHp;
            if (enemyInfo.MaxTotalHp != 0f)
                enemyCrystalFilledHp.fillAmount = enemyInfo.CurTotalHp / enemyInfo.MaxTotalHp;


            // Update hints info
            if (ConstructWindow.Instance.IsOpened())
            {
                _hintWindowRect.anchoredPosition =
                    new Vector2(hintXWhenConstructWindowOpen, _hintWindowRect.anchoredPosition.y);
            }
            else
            {
                _hintWindowRect.anchoredPosition = new Vector2(_hintOriginalX, _hintWindowRect.anchoredPosition.y);
            }

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
            for (int i = 0; i < _hintEntries.Count; i++)
            {
                var entry = _hintEntries[i];
                if (!entry.IsFading)
                {
                    Vector2 desiredPos = topAnchor.anchoredPosition - new Vector2(0, i * topAnchor.rect.height);
                    entry.TargetPos = desiredPos;
                    // 平滑移动（Lerp 或 SmoothDamp 都行）
                    entry.HintRect.anchoredPosition = Vector2.Lerp(entry.HintRect.anchoredPosition, desiredPos, Time.deltaTime * 15f);
                }

                // 超时后淡出
                if (!entry.IsFading && curTime - entry.UpdateTime > maxDuration)
                {
                    StartCoroutine(FadeAndRemove(entry));
                }
            }
        }

        private void UpdateStaticInfo()
        {
            playerCrystalFilledHp.sprite =
                BasicUIResourceManager.Instance.FactionCrystalHpFilledSprites[_playerFaction];
            playerCrystalBlankHp.sprite = BasicUIResourceManager.Instance.FactionCrystalHpBlankSprites[_playerFaction];

            enemyCrystalFilledHp.sprite =
                BasicUIResourceManager.Instance.FactionCrystalHpFilledSprites[~_playerFaction];
            enemyCrystalBlankHp.sprite = BasicUIResourceManager.Instance.FactionCrystalHpBlankSprites[~_playerFaction];
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

        [Serializable]
        public struct EnemyCrystalCountToWaveColor
        {
            public WaveColorType type;
            public int crystalCount;
        }

        public enum WaveColorType
        {
            Easy,
            Normal,
            Hard,
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
    }
}