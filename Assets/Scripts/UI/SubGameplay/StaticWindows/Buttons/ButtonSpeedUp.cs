using System.Collections.Generic;
using SparFlame.Components.General;
using SparFlame.UI.General;
using TMPro;
using Unity.Entities;
using UnityEngine;
using UnityEngine.UI;

namespace SparFlame.UI.SubGameplay.StaticWindows.Buttons
{
    [RequireComponent(typeof(Button))]
    public class ButtonSpeedUp : ButtonUtils.SelfButton
    {
        [Header("Speed Up Scale Button")] [SerializeField]
        private TMP_Text timeScaleText;

        [SerializeField] private List<float> speedUpScaleConfigList = new();
        
        
        private EntityQuery _timeScale;
        private int _currentSpeedUpIndex;

        private void Start()
        {
            _timeScale = World.DefaultGameObjectInjectionWorld.EntityManager.CreateEntityQuery(typeof(GameTimeScale));
            var currentTimeScale = _timeScale.GetSingleton<GameTimeScale>().Value;
            timeScaleText.text = currentTimeScale.ToString("F1");
        }

        public override void OnClick()
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
    }
}