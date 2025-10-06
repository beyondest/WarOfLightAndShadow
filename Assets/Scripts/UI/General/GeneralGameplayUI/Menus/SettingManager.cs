using System;
using SparFlame.Components.General;
using SparFlame.Systems.General.Audio;
using TMPro;
using Unity.Entities;
using UnityEngine;
using UnityEngine.UI;

namespace SparFlame.UI.General
{
    public class SettingManager : MonoBehaviour
    {
        [SerializeField] private Toggle fullscreenToggle;
        [SerializeField] private Toggle muteToggle;
        [SerializeField] private Toggle muteMusicOnly;
        [SerializeField] private Toggle hintToggle;
        
        
        [SerializeField] private Slider playerTakenDamageScaleSlider;
        [SerializeField] private TMP_Text text1;
        [SerializeField] private Slider enemyTakenDamageScaleSlider;
        [SerializeField] private TMP_Text text2;

        [SerializeField] private float damageTakenSliderMax = 3f;
        [SerializeField] private float damageTakenSliderMin;
        [SerializeField] private Slider armyGroupConjureTimeScale;
        [SerializeField] private TMP_Text text3;

        [SerializeField] private float conjureTimeScaleMax = 30f;
        [SerializeField] private float conjureTimeScaleMin = 0.1f;
        
        [SerializeField] private Slider conjureTimeScaleSlider;
        [SerializeField] private TMP_Text text4;
        [SerializeField] private float conjureTimeScaleSliderMax = 10f;
        [SerializeField] private float conjureTimeScaleSliderMin = 0.1f;
        
        [SerializeField] private Slider spawnTimeScaleSlider;
        [SerializeField] private TMP_Text text5;
        [SerializeField] private float spawnTimeScaleSliderMax = 10f;
        [SerializeField] private float spawnTimeScaleSliderMin = 0.01f;
        
        
        
        [SerializeField] private Button applyDamageTakeButton;
        [SerializeField] private Button applyConjureSpeedButton;
        [SerializeField] private Button applyConjureHourButton;
        [SerializeField] private Button applySpawnHourButton;
        public Action<bool> OnToggleHintWindow;

        public static SettingManager Instance;

        private void Awake()
        {
            if (!Instance)
                Instance = this;
            else
                Destroy(gameObject);
            playerTakenDamageScaleSlider.maxValue = damageTakenSliderMax;
            playerTakenDamageScaleSlider.minValue = damageTakenSliderMin;
            enemyTakenDamageScaleSlider.maxValue = damageTakenSliderMax;
            enemyTakenDamageScaleSlider.minValue = damageTakenSliderMin;
            armyGroupConjureTimeScale.maxValue = conjureTimeScaleMax;
            armyGroupConjureTimeScale.minValue = conjureTimeScaleMin;
            conjureTimeScaleSlider.maxValue = conjureTimeScaleSliderMax;
            conjureTimeScaleSlider.minValue = conjureTimeScaleSliderMin;
            spawnTimeScaleSlider.maxValue = spawnTimeScaleSliderMax;
            spawnTimeScaleSlider.minValue = spawnTimeScaleSliderMin;
        }

        void Start()
        {
            SetFullscreen(true);
            HintsEnable(true);

            fullscreenToggle.isOn = Screen.fullScreen;
            fullscreenToggle.onValueChanged.AddListener(SetFullscreen);

            muteToggle.isOn = AudioListener.volume == 0f;
            muteToggle.onValueChanged.AddListener(Mute);

            muteMusicOnly.isOn = AudioManager.Instance.bgm.mute;
            muteMusicOnly.onValueChanged.AddListener(MuteMusicOnly);

           
            hintToggle.isOn = true;
            hintToggle.onValueChanged.AddListener(HintsEnable);
            applyDamageTakeButton.onClick.AddListener(ApplyDamageScale);
            applyConjureSpeedButton.onClick.AddListener(ApplyConjureSpeedScale);
            applyConjureHourButton.onClick.AddListener(ApplyConjureHour);
            applySpawnHourButton.onClick.AddListener(ApplySpawnHourScale);
            text1.text = playerTakenDamageScaleSlider.value.ToString("F2");
            text2.text = enemyTakenDamageScaleSlider.value.ToString("F2");
            text3.text = armyGroupConjureTimeScale.value.ToString("F2");
            text4.text = conjureTimeScaleSlider.value.ToString("F2");
            text5.text = spawnTimeScaleSlider.value.ToString("F2");
            playerTakenDamageScaleSlider.onValueChanged.AddListener(arg0 =>
            {
                text1.text = arg0.ToString("F2");
            });
            enemyTakenDamageScaleSlider.onValueChanged.AddListener(arg0 =>
            {
                text2.text = arg0.ToString("F2");
            });
            armyGroupConjureTimeScale.onValueChanged.AddListener(arg0 =>
            {
                text3.text = arg0.ToString("F2");
            });
            conjureTimeScaleSlider.onValueChanged.AddListener(arg0 =>
            {
                text4.text = arg0.ToString("F2");
            });
            spawnTimeScaleSlider.onValueChanged.AddListener(arg0 =>
            {
                text5.text = arg0.ToString("F2");
            });
        }

        private void ApplySpawnHourScale()
        {
            using var query = World.DefaultGameObjectInjectionWorld.EntityManager.CreateEntityQuery(typeof(ResourceDebug));
            if(query.IsEmpty)return;
            var debug = query.GetSingletonRW<ResourceDebug>();
            debug.ValueRW.spawnHoursScale = spawnTimeScaleSlider.value;
        }

        private void ApplyConjureHour()
        {
            using var query = World.DefaultGameObjectInjectionWorld.EntityManager.CreateEntityQuery(typeof(ConjureDebug));
            if(query.IsEmpty)return;
            var debug = query.GetSingletonRW<ConjureDebug>();
            debug.ValueRW.conjureHoursScale = conjureTimeScaleSlider.value;
        }
        private void ApplyDamageScale()
        {
            using var query = World.DefaultGameObjectInjectionWorld.EntityManager.CreateEntityQuery(typeof(StatDebug));
            if(query.IsEmpty)return;
            var debug = query.GetSingletonRW<StatDebug>();
            debug.ValueRW.playerSideDamageTakenScale = playerTakenDamageScaleSlider.value;
            debug.ValueRW.enemySideDamageTakenScale = enemyTakenDamageScaleSlider.value;
        }

        private void ApplyConjureSpeedScale()
        {
            using var query = World.DefaultGameObjectInjectionWorld.EntityManager.CreateEntityQuery(typeof(EnemyAIMainGameplayDebug));
            if(query.IsEmpty)return;
            var debug = query.GetSingletonRW<EnemyAIMainGameplayDebug>();
            debug.ValueRW.armyGroupConjureTimeScale = armyGroupConjureTimeScale.value;
        }

        void SetFullscreen(bool isFullscreen)
        {
            Screen.SetResolution(1920, 1080, isFullscreen);
        }

        void Mute(bool isMuted)
        {
            AudioListener.volume = isMuted ? 0f : 1f;
        }

        void MuteMusicOnly(bool isMuted)
        {
            AudioManager.Instance.bgm.mute = isMuted;
        }

        public void HintsEnable(bool enable)
        {
            OnToggleHintWindow?.Invoke(enable);
        }
    }
}