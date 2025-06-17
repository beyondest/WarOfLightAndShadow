using System;
using UnityEngine;
using SparFlame.BootStrapper;
using SparFlame.GamePlaySystem.General;
using SparFlame.UI.SubGameplay;
using TMPro;
using UnityEngine.UI;

namespace SparFlame.UI.General
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
        [SerializeField] private GameObject settings;

        [SerializeField] private Image gameOverImage;
        [SerializeField] private TMP_Text gameOverText;


        // Internal Data
        private FactionTag _playerFaction;

        // Interface
        public static MenuOutController Instance;
        
        public void ShowPauseMenu()
        {
            pauseMenu.SetActive(true);
        }

        public void ShowLoadingScreen()
        {
            if (_playerFaction == FactionTag.Ally)
            {
                loadingLight.SetActive(true);
            }
            else
            {
                loadingDark.SetActive(false);
            }
        }

        public void SetLoadingProgress(float progress)
        {
            
        }
        #region ButtonMethods


        public void OnClickResume()
        {
            pauseMenu.SetActive(false);
            GameController.Instance.ResumeGame();
        }

        public void OnClickExit()
        {
            GameController.Instance.ExitGame();
        }

        public void OnClickGoToMainMenu()
        {
            pauseMenu.SetActive(false);
            mainMenu.SetActive(true);
            gameOverMenu.SetActive(false);
            gamePlayWindow.SetActive(false);
            GameController.Instance.EndGameToMainMenu();
        }

        public void OnClickPlay()
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

        public void OnClickContinue()
        {
            Debug.LogWarning("Continue to last saving is not implemented yet");
        }

        public void OnClickSettings()
        {
            settings.SetActive(true);
        }

        public void OnClickReturn()
        {
            settings.SetActive(false);
        }

        #endregion


        private void Awake()
        {
            if(Instance == null)
                Instance = this;
            else
                Destroy(gameObject);
        }

        private void Start()
        {
            GameController.Instance.OnPause += () =>
            {
                if (!gameOverMenu.activeSelf)
                {
                    pauseMenu.SetActive(true);
                }
            };
            GameController.Instance.OnResume += () => pauseMenu.SetActive(false);
            GameController.Instance.OnGameOver += OnGameOver;
            GameController.Instance.OnSubGameStart += SubGameStart;
            GameController.Instance.OnMainGameStart += MainGameStart;

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
            settings.SetActive(false);
        }

        private void SubGameStart()
        {
            gamePlayWindow.SetActive(true);
            UpRightButtonWindow.Instance.OnClickTutorial();
        }

        private void MainGameStart()
        {
            loadingDark.SetActive(false);
            loadingLight.SetActive(false);
            selectMenu.SetActive(false);
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