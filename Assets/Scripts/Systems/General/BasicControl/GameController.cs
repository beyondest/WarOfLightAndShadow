using System;
using System.Collections;
using System.IO;
using SparFlame.Components.General;
using Unity.Entities;
using UnityEngine;

namespace SparFlame.Systems.General.BasicControl
{
    public class GameController : MonoBehaviour
    {
        [SerializeField] private float checkInitInterval = 0.1f;
        public static GameController Instance;

        #region Events

        public event Action OnPause;
        public event Action OnResume;

        public event Action OnBackToMainMenu;

        // faction is winner
        public event Action<FactionTag> OnGameOver;

        public event Action<FactionTag> OnPlayerChooseFaction;
        // float is start elapsed time

        public event Action OnSubGameStart;
        public event Action OnMainGameStart;

        public event Action<int> EcsLoadCitySaving;
        public event Action<int> EcsSaveSityData;
        public event Action<GameStatusSwitchType> EcsSwitchGameStatus; 

        #endregion


        #region GameControlMethods

        public void GameOver(FactionTag winnerFaction)
        {
            OnGameOver?.Invoke(winnerFaction);
            PauseGame();
        }

        public void PauseGame()
        {
            OnPause?.Invoke();
        }

        public void ResumeGame()
        {
            OnResume?.Invoke();
        }

        public void EndGameToMainMenu()
        {
            ResumeGame();
            OnBackToMainMenu?.Invoke();
        }

        public void ExitGame()
        {
            Application.Quit();
        }

        public void PlayerChooseFaction(FactionTag playerFaction)
        {
            OnPlayerChooseFaction?.Invoke(playerFaction);
        }

        public void SubGameStart()
        {
            OnSubGameStart?.Invoke();
        }

        public void MainGameStart()
        {
            OnMainGameStart?.Invoke();
        }

        public void EnterPlayerCity(int cityId)
        {
            PauseGame();
            _cityId = cityId;
            SceneController.Instance.EnterCityGameplay(cityId);
            var savePath = SaveUtilities.GetCitySavePath(cityId);
            if (File.Exists(savePath))
            {
                EcsLoadCitySaving?.Invoke(cityId);
            }
            StartCoroutine(CheckSceneLoading());
        }

        public void ReturnToMainWorldFromCity()
        {
            PauseGame();
            SceneController.Instance.ReturnToMainGameplayFromSubGameplay();
            SaveCityData();
            _cityId = -1;
            StartCoroutine(CheckSceneLoading());
        }

        public void SaveCityData()
        {
            if (_cityId == -1)
            {
                throw new ArgumentException("This should never happen, you try to save city data when not in city");
            }
            EcsSaveSityData?.Invoke(_cityId);
        }

        #endregion


        // Internal Data
        private int _cityId = -1;
        private GameStatusSwitchType _switchType;

        #region EventFunctions

        private void Awake()
        {
            if (!Instance)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
            }

            Application.targetFrameRate = 60;
        }
        
        

        #endregion

        private IEnumerator CheckSceneLoading()
        {
            while (true)
            {
                if(SceneController.Instance.IsInitialized)
                    break;
                yield return new WaitForSeconds(checkInitInterval);
            }
            switch (_switchType)
            {
                case GameStatusSwitchType.SubGameToMainGame:
                    SubGameStart();
                    break;
                case GameStatusSwitchType.MainGameToSubGame:
                    MainGameStart();
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
            ResumeGame();
            EcsSwitchGameStatus?.Invoke(_switchType);
        }
    }
}