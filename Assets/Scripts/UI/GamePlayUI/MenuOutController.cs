using System;
using UnityEngine;
using SparFlame.BootStrapper;
using SparFlame.GamePlaySystem.General;
using SparFlame.UI.GamePlay;
using TMPro;
using Unity.Entities;
using UnityEngine.UI;

namespace SparFlame.UI.Menu.Out
{
    public class MenuOutController : MonoBehaviour
    {
        [SerializeField] private GameObject pauseMenu;
        [SerializeField] private GameObject mainMenu;
        [SerializeField] private GameObject gameOverMenu;
        [SerializeField] private Image gameOverImage;
        [SerializeField] private TMP_Text gameOverText; 
        // Start is called once before the first execution of Update after the MonoBehaviour is created


        private EntityManager _em;
        private EntityQuery _notPauseTag;
        private EntityQuery _gameOverRequest;
        private bool _isPaused;
        
        private void OnEnable()
        {
            if (GameController.Instance == null)
            {
                Debug.LogError("BootStrapper scene needs to be placed at first place");
            }
            GameController.Instance.OnPause += ShowPauseMenu;
            GameController.Instance.OnResume += HidePauseMenu;
            GameController.Instance.OnGameOver += GameOver;
        }

        private void Start()
        {
            pauseMenu.SetActive(false);
            gameOverMenu.SetActive(false);
        }


        #region MenuMethods

        public void GameOver(FactionTag winner)
        {
            gameOverImage.sprite = BasicResourceManager.Instance.FactionGameOverSprites[winner];
            gameOverMenu.SetActive(true);
            gameOverText.text = winner == FactionTag.Ally ? "Victory" : "Defeat";
        }
        

        public void ShowPauseMenu()
        {
            pauseMenu.SetActive(true);
        }

        public void HidePauseMenu()
        {
            pauseMenu.SetActive(false);
        }

        public void ShowMainMenu()
        {
            mainMenu.SetActive(true);
        }

        public void HideMainMenu()
        {
            mainMenu.SetActive(false);
        }
        #endregion


        #region ButtonMethods

        public void ButtonPauseClicked()
        {
            ShowPauseMenu();
            GameController.Instance.PauseGame();
        }

        public void ButtonResumeClicked()
        {
            HidePauseMenu();
            GameController.Instance.ResumeGame();
        }

        public void ButtonExitClicked()
        {
            GameController.Instance.ExitGame();
        }

        public void ButtonGoToMainMenuClicked()
        {
            HidePauseMenu();
            ShowMainMenu();
            gameOverMenu.SetActive(false);
            GameController.Instance.EndGameToMainMenu();
        }

        public void ButtonPlayClicked()
        {
            HideMainMenu();
            GameController.Instance.StartGame();
        }

        public void ButtonContinueClicked()
        {
            Debug.LogWarning("Continue to last saving is not implemented yet");
        }
        
        #endregion

        
    }
}
