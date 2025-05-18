using System;
using System.Collections.Generic;
using SparFlame.BootStrapper;
using SparFlame.GamePlaySystem.General;
using SparFlame.GamePlaySystem.Waves;
using SparFlame.UI.General;
using SparFlame.UI.Menu.Out;
using TMPro;
using Unity.Entities;
using UnityEngine;
using UnityEngine.UI;
using Image = UnityEngine.UI.Image;

namespace SparFlame.UI.GamePlay
{
    public class UpRightButtonWindow : MonoBehaviour
    {
        [Header("Speed Up Scale Button")]
        [SerializeField] private TMP_Text timeScaleText;

        
        [Header("Wave")]
        [SerializeField] private Image waveTimeFilled;
        [SerializeField] private Image waveTimeFilledForBlink;
        [SerializeField] private Image waveTimeBlank;
        [SerializeField] private Button nextWaveButton;
        [SerializeField] private TMP_Text waveIdx;
        [SerializeField] private TMP_Text nextWaveRemainingTime;

        [Header("Crystal Hp")] 
        [SerializeField] private Image playerCrystalFilledHp;
        [SerializeField] private Image playerCrystalBlankHp;

        [SerializeField] private Image enemyCrystalFilledHp;
        [SerializeField] private Image enemyCrystalBlankHp;
        
        
        [Header("config")]
        [SerializeField] private List<float> speedUpScaleConfigList = new();
        [SerializeField] private List<EnemyCrystalCountToWaveColor> waveColorConfig;

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
            timeScaleText.text = "x" + curScale.ToString("F2");
        }

        public void OnClickTutorial()
        {
            Debug.Log("Tutorial not implemented");
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
            _em.AddComponent<GameplayEntityTag>(nextWaveRequest);
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

        #region EventFunctions

        private void Awake()
        {
            GameController.Instance.OnPlayerChooseFaction += factionTag => _playerFaction = factionTag;
            GeneralResourceManager.Instance.OnAllResourceLoaded += UpdateStaticInfo;
        }

        private void Start()
        {
            _em = World.DefaultGameObjectInjectionWorld.EntityManager;
            _gamingTag = _em.CreateEntityQuery(typeof(GamingTag));
            _timeScale = _em.CreateEntityQuery(typeof(GameTimeScale));
            _enemyCrystalInfo = _em.CreateEntityQuery(typeof(EnemyCrystalInfo));
            _playerCrystalInfo = _em.CreateEntityQuery(typeof(PlayerCrystalInfo));

            _waveDataQuery = _em.CreateEntityQuery(typeof(GameWaveData));
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
                    waveTimeFilled.sprite = BasicUIResourceManager.Instance.LightWaveColorTypeSprites[_currentColorType];
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
                waveTimeFilled.fillAmount =1f - waveData.NextWaveRemainingTime / waveData.CurWaveInterval;
            }
            nextWaveRemainingTime.text = UIMathMethods.FormatTime((int)waveData.NextWaveRemainingTime);
            waveIdx.text = waveData.CurWaveIndex.ToString();
            nextWaveButton.enabled = enemyInfo.TotalCount == 0;
            waveTimeFilledForBlink.enabled = enemyInfo.TotalCount == 0;
            

            
            // Update hp info
            enemyCrystalFilledHp.enabled = enemyInfo.TotalCount != 0;
            enemyCrystalBlankHp.enabled = enemyInfo.TotalCount != 0;
            // Update crystal hp info
            if (playerInfo.MaxTotalHp != 0f)
                playerCrystalFilledHp.fillAmount = playerInfo.CurTotalHp / playerInfo.MaxTotalHp;
            if(enemyInfo.MaxTotalHp != 0f)
                enemyCrystalFilledHp.fillAmount = enemyInfo.CurTotalHp/ enemyInfo.MaxTotalHp;

        }

        private void UpdateStaticInfo()
        {
            playerCrystalFilledHp.sprite = BasicUIResourceManager.Instance.FactionCrystalHpFilledSprites[_playerFaction];
            playerCrystalBlankHp.sprite = BasicUIResourceManager.Instance.FactionCrystalHpBlankSprites[_playerFaction];
            
            enemyCrystalFilledHp.sprite = BasicUIResourceManager.Instance.FactionCrystalHpFilledSprites[~_playerFaction];
            enemyCrystalBlankHp.sprite = BasicUIResourceManager.Instance.FactionCrystalHpBlankSprites[~_playerFaction];
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
    }
}