using System;
using UnityEngine;
using SparFlame.Components.General;
using SparFlame.Systems.General.BasicControl;
using SparFlame.Systems.General.BasicControl.GlobalMonos;
using TMPro;
using Unity.Entities;
using UnityEngine.UI;

namespace SparFlame.UI.General
{
    public class MenuOutController : MonoBehaviour
    {
        [Header("Menus")] [SerializeField] private GameObject pauseMenu;
        [SerializeField] private GameObject mainMenu;
        [SerializeField] private GameObject gameOverMenu;
        [SerializeField] private GameObject selectMenu;
        [SerializeField] private GameObject selectMenuElements;

        [Header("Loading Screen")] [SerializeField]
        private GameObject loadingLight;

        [SerializeField] private GameObject loadingDark;
        [SerializeField] private Image loadingFillLight;
        [SerializeField] private Image loadingFillDark;

        [Header("Settings")] [SerializeField] private GameObject settings;

        [Header("GameOver Menu")] [SerializeField]
        private Image gameOverImage;

        [SerializeField] private TMP_Text gameOverText;
        [SerializeField] private bool ifLockToLight = true;

        // Internal Data

        private FactionTag _playerFaction;
        private Image _loadingImage;
        private EntityManager _em;
        private EntityQuery _subGameStatusQuery;


        // Interface
        public static MenuOutController Instance;

        public void ShowPauseMenu()
        {
            pauseMenu.SetActive(true);
        }

        public void HideMainMenu()
        {
            mainMenu.SetActive(false);
        }

        #region ButtonMethods

        public void OnClickResume()
        {
            pauseMenu.SetActive(false);
            GameController.Instance.ResumeGame(false);
        }

        public void OnClickSaveGame()
        {
            var subGameStatusData = _subGameStatusQuery.GetSingleton<SubGameStatusData>();
            if (GameStatusUtils.IsInBattle(subGameStatusData))
            {
                ConfirmWindow.Instance.Show("You cannot save game while in battle",
                    OnClickResume, showCancelButton: false);
            }
            else
            {
                SaveLoadMenu.Instance.SetModeAndShow(SaveLoadMenu.WindowMode.Save);
            }
        }

        public void OnClickExit()
        {
            ConfirmWindow.Instance.Show(
                "Are you sure you want to exit the game? You will lose non saving progress",
                () => { GameController.Instance.ExitGame(); });
        }

        public void OnClickGoToMainMenu()
        {
            ConfirmWindow.Instance.Show(
                "Are you sure you want to go back to the main menu? You will lose non saving progress",
                () =>
                {
                    pauseMenu.SetActive(false);
                    mainMenu.SetActive(true);
                    gameOverMenu.SetActive(false);
                    CustomCoroutineRunner.Instance.StartCoroutine(GameController.Instance.EndGameToMainMenu());
                });
        }

        public void OnClickNewGame()
        {
            mainMenu.SetActive(false);
            if (ifLockToLight)
            {
                OnClickFactionButton((int)FactionTag.Light);
            }
            else
            {
                selectMenu.SetActive(true);
                selectMenuElements.SetActive(true);
            }
        }

        public void OnClickFactionButton(int faction)
        {
            _playerFaction = (FactionTag)faction;
            StartCoroutine(
                GameController.Instance.StartGameFirstTime(_playerFaction, true, SaveUtilities.NewGameSaveSlot, 0));
            selectMenu.SetActive(false);
            selectMenuElements.SetActive(false);
        }

        public void OnClickLoadGameInMainMenu()
        {
            SaveLoadMenu.Instance.SetModeAndShow(SaveLoadMenu.WindowMode.LoadInMainMenu);
        }

        public void OnClickLoadGameInGame()
        {
            SaveLoadMenu.Instance.SetModeAndShow(SaveLoadMenu.WindowMode.LoadInGame);
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


        #region EventFunctions

        private void Awake()
        {
            if (!Instance)
                Instance = this;
            else
                Destroy(gameObject);
        }

        private void Start()
        {
            GameController.Instance.OnPause += PauseGame;
            GameController.Instance.OnResume += ResumeGame;
            GameController.Instance.LoadingProgress.OnProgressChanged += UpdateLoadingScreen;
            GameController.Instance.OnShowLoadingScreen += ShowLoadingScreen;
            GameController.Instance.OnHideLoadingScreen += HideLoadingScreen;
            GameController.Instance.OnSetPlayerFactionData += data =>
            {
                _playerFaction = data.faction;
            };
            // Switch gameplay loading screen

            mainMenu.SetActive(true);
            loadingLight.SetActive(false);
            loadingDark.SetActive(false);
            selectMenu.SetActive(false);
            selectMenuElements.SetActive(false);
            pauseMenu.SetActive(false);
            gameOverMenu.SetActive(false);
            settings.SetActive(false);

            _em = World.DefaultGameObjectInjectionWorld.EntityManager;
            _subGameStatusQuery = _em.CreateEntityQuery(typeof(SubGameStatusData));
        }

        private void OnDestroy()
        {
            try
            {
                if (_subGameStatusQuery != default)
                    _subGameStatusQuery.Dispose();
            }
            catch (Exception)
            {
                // ignored
            }
        }

        #endregion


        private void PauseGame(bool isSwitching)
        {
            if (isSwitching) return;
            pauseMenu.SetActive(true);
        }

        private void ResumeGame(bool isSwitching)
        {
            if (isSwitching) return;
            pauseMenu.SetActive(false);
        }


        private void ShowLoadingScreen()
        {
            if (_playerFaction == FactionTag.Light)
            {
                loadingLight.SetActive(true);
                _loadingImage = loadingFillLight;
            }
            else
            {
                loadingDark.SetActive(true);
                _loadingImage = loadingFillDark;
            }
        }

        private void UpdateLoadingScreen(float progress)
        {
            _loadingImage.fillAmount = progress;
        }

        private void HideLoadingScreen()
        {
            loadingLight.SetActive(false);
            loadingDark.SetActive(false);
        }
    }
}