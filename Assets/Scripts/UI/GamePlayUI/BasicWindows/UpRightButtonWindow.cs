using System.Collections.Generic;
using SparFlame.BootStrapper;
using SparFlame.GamePlaySystem.General;
using SparFlame.UI.Menu.Out;
using TMPro;
using Unity.Entities;
using UnityEngine;

namespace SparFlame.UI.GamePlay
{
    public class UpRightButtonWindow : MonoBehaviour
    {
        [SerializeField] private List<float> scaleList = new();
        [SerializeField] private TMP_Text timeScaleText;
        private int _currentIndex;
        
        
        private EntityManager _em;
        private EntityQuery _timeScale;

        private void Start()
        {
            _em = World.DefaultGameObjectInjectionWorld.EntityManager;
            _timeScale = _em.CreateEntityQuery(typeof(GameTimeScale));
        }

        public void OnClickPause()
        {
            MenuOutController.Instance.ShowPauseMenu();
            GameController.Instance.PauseGame();
        }

        public void OnClickSpeedUp()
        {
            var scale = _timeScale.GetSingletonRW<GameTimeScale>();
            if(_currentIndex == scaleList.Count - 1)
                _currentIndex = 0;
            else
                _currentIndex++;
            var curScale = scaleList[_currentIndex];
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
    }
}