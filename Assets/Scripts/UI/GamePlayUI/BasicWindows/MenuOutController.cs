using System;
using UnityEngine;
using SparFlame.BootStrapper;
using SparFlame.GamePlaySystem.General;
using SparFlame.UI.GamePlay;
using TMPro;
using UnityEngine.UI;

namespace SparFlame.UI.Menu.Out
{
    public class MenuOutController : MonoBehaviour
    {
        [SerializeField] private GameObject gamePlayWindow;
        [SerializeField] private GameObject pauseMenu;
        [SerializeField] private GameObject mainMenu;
        [SerializeField] private GameObject gameOverMenu;
        [SerializeField] private GameObject selectMenu;
        [SerializeField] private GameObject loadingLight;
        [SerializeField] private GameObject loadingDark;
        [SerializeField] private Image loadingFillLight;
        [SerializeField] private Image loadingFillDark;

        [SerializeField] private Image gameOverImage;
        [SerializeField] private TMP_Text gameOverText;


        // Internal Data
        private FactionTag _playerFaction;

        #region ButtonMethods

        public void ButtonPauseClicked()
        {
            pauseMenu.SetActive(true);
            GameController.Instance.PauseGame();
        }

        public void ButtonResumeClicked()
        {
            pauseMenu.SetActive(false);
            GameController.Instance.ResumeGame();
        }

        public void ButtonExitClicked()
        {
            GameController.Instance.ExitGame();
        }

        public void ButtonGoToMainMenuClicked()
        {
            pauseMenu.SetActive(false);
            mainMenu.SetActive(true);
            gameOverMenu.SetActive(false);
            gamePlayWindow.SetActive(false);
            GameController.Instance.EndGameToMainMenu();
        }

        public void ButtonPlayClicked()
        {
            mainMenu.SetActive(false);
            selectMenu.SetActive(true);
        }

        public void OnClickLightFaction()
        {
            _playerFaction = FactionTag.Ally;
            selectMenu.SetActive(false);
            loadingLight.SetActive(true);
            GameController.Instance.PlayerChooseFaction(FactionTag.Ally);
        }

        public void OnClickDarkFaction()
        {
            _playerFaction = FactionTag.Enemy;
            selectMenu.SetActive(false);
            loadingDark.SetActive(true);
            GameController.Instance.PlayerChooseFaction(FactionTag.Enemy);
        }

        public void ButtonContinueClicked()
        {
            Debug.LogWarning("Continue to last saving is not implemented yet");
        }

        #endregion


        private void Start()
        {
            GameController.Instance.OnPause += () => pauseMenu.SetActive(true);
            GameController.Instance.OnResume += () => pauseMenu.SetActive(false);
            GameController.Instance.OnGameOver += OnGameOver;
            GameController.Instance.OnGameStart += OnGameStart;

            GeneralResourceManager.Instance.OnInitProgress += f =>
            {
                switch (_playerFaction)
                {
                    case FactionTag.Ally:
                        loadingFillLight.fillAmount = f;
                        break;
                    case FactionTag.Enemy:
                        loadingFillDark.fillAmount = f;
                        break;
                    case FactionTag.Neutral:
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            };
            mainMenu.SetActive(true);
            loadingLight.SetActive(false);
            loadingDark.SetActive(false);
            selectMenu.SetActive(false);
            pauseMenu.SetActive(false);
            gameOverMenu.SetActive(false);
        }

        private void OnGameStart()
        {
            loadingDark.SetActive(false);
            loadingLight.SetActive(false);
            selectMenu.SetActive(false);
            gamePlayWindow.SetActive(true);
        }

        private void OnGameOver(FactionTag winner)
        {
            gameOverImage.sprite = BasicUIResourceManager.Instance.FactionGameOverSprites[winner];
            gameOverMenu.SetActive(true);
            gamePlayWindow.SetActive(false);
            gameOverText.text = winner == _playerFaction ? "Victory" : "Defeat";
        }
    }
}