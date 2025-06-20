using System;
using System.Collections;
using System.IO;
using SparFlame.Components.General;
using SparFlame.Systems.General.Audio;
using SparFlame.Systems.General.Input;
using UnityEngine;

namespace SparFlame.Systems.General.BasicControl
{
    public class GameController : MonoBehaviour
    {
        [SerializeField] private float checkInitInterval = 0.1f;
        public static GameController Instance;

        #region Events

        public event Action<bool> OnPause;
        public event Action<bool> OnResume;

        public event Action OnBackToMainMenu;

        // faction is winner
        public event Action<FactionTag> OnGameOver;

        public event Action<FactionTag> OnPlayerChooseFaction;
        // float is start elapsed time

        public event Action OnSubGameStart;
        public event Action OnMainGameStart;

        public event Action<int> OnEcsLoadCitySaving;
        public event Action<int> OnEcsSaveCityData;
        public event Action<GameStatusSwitchType, SubGameStatus> OnEcsSwitchGameStatus;

        public event Action<int> OnEcsChooseSavingSlot; 
        #endregion


        #region GameControlMethods

        public void GameOver(FactionTag winnerFaction)
        {
            OnGameOver?.Invoke(winnerFaction);
            PauseGame(false);
        }

        public void PauseGame(bool isSwitchingGameplay)
        {
            OnPause?.Invoke(isSwitchingGameplay);
        }

        public void ResumeGame(bool isSwitchingGameplay)
        {
            OnResume?.Invoke(isSwitchingGameplay);
        }

        public void EndGameToMainMenu()
        {
            InputListener.Instance.DisableAllMaps();
            AudioManager.Instance.EnableGlobalAudioListener(true);
            ResumeGame(false);
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

        public void PlayerChooseSavingSlot(int slot)
        {
            _savingSlot = slot;
            OnEcsChooseSavingSlot?.Invoke(slot);
        }
        public void SubGameStart()
        {
            InputListener.Instance.EnableSubGameMaps();
            OnSubGameStart?.Invoke();
        }

        public void MainGameStart()
        {
            InputListener.Instance.EnableMainGameMaps();
            OnMainGameStart?.Invoke();
        }

        public void EnterPlayerCity(int cityId)
        {
            _switchType = GameStatusSwitchType.MainGameToSubGame;
            _targetSubGameStatus = SubGameStatus.PlayerCity;
            // AudioManager.Instance.SetAudioListener(true);
            PauseGame(true);
            _cityId = cityId;
            SceneController.Instance.EnterCityGameplay(cityId);
            var savePath = SaveUtilities.GetCitySavePath(cityId,_savingSlot);
            if (File.Exists(savePath))
            {
                OnEcsLoadCitySaving?.Invoke(cityId);
            }
            StartCoroutine(CheckSceneLoading());
        }

        public void ReturnToMainWorldFromCity()
        {
            _switchType = GameStatusSwitchType.SubGameToMainGame;
            // AudioManager.Instance.SetAudioListener(true);
            PauseGame(true);
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
            OnEcsSaveCityData?.Invoke(_cityId);
        }

        #endregion

        #region TestButtonMethods

        public void OnClickSwitchToSubGame()
        {
            OnEcsSwitchGameStatus?.Invoke(GameStatusSwitchType.MainGameToSubGame, SubGameStatus.PlayerCity);
            SubGameStart();
        }
        

        #endregion

        // Internal Data
        private int _cityId = -1;
        private GameStatusSwitchType _switchType;
        private SubGameStatus _targetSubGameStatus;
        private int _savingSlot;
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
                case GameStatusSwitchType.MainGameToSubGame:
                    SubGameStart();
                    break;
                case GameStatusSwitchType.SubGameToMainGame:
                    _targetSubGameStatus = SubGameStatus.None;
                    MainGameStart();
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
            ResumeGame(true);
            OnEcsSwitchGameStatus?.Invoke(_switchType, _targetSubGameStatus);
        }
    }
}