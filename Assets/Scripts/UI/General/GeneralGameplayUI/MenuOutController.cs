using UnityEngine;
using SparFlame.Components.General;
using SparFlame.Core.Utils;
using SparFlame.Systems.General.BasicControl;
using TMPro;
using UnityEngine.UI;

namespace SparFlame.UI.General
{
    public class MenuOutController : MonoBehaviour
    {
        [SerializeField] private GameObject subGameplayUI;
        [SerializeField] private GameObject mainGameplayUI;
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
        private Image _loadingImage;
        
        // Cache
        private ResourceLoadingUtils.LoadingProgress _progress;
        
        // Interface
        public static MenuOutController Instance;
        
        public void ShowPauseMenu()
        {
            pauseMenu.SetActive(true);
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
            subGameplayUI.SetActive(false);
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


        #region EventFunctions

        

        private void Awake()
        {
            if(!Instance)
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
            // Init loading screen
            GeneralResourceManager.Instance.OnLoadAllResources += () =>
                ShowLoadingScreen(GeneralResourceManager.Instance.LoadingProgress);
            
            // Switch gameplay loading screen
            
            mainMenu.SetActive(true);
            loadingLight.SetActive(false);
            loadingDark.SetActive(false);
            selectMenu.SetActive(false);
            pauseMenu.SetActive(false);
            gameOverMenu.SetActive(false);
            settings.SetActive(false);
        }

        #endregion

        private void SubGameStart()
        {
            HideLoadingScreen();
            
            subGameplayUI.SetActive(true);
            mainGameplayUI.SetActive(false);
        }

        private void MainGameStart()
        {
            HideLoadingScreen();
            selectMenu.SetActive(false);
            
            mainGameplayUI.SetActive(true);
            subGameplayUI.SetActive(false);
        }

        private void OnGameOver(FactionTag winner)
        {
            gameOverImage.sprite = BasicUIResourceManager.Instance.FactionGameOverSprites[winner];
            gameOverMenu.SetActive(true);
            subGameplayUI.SetActive(false);
            gameOverText.text = winner == _playerFaction ? "Victory" : "Defeat";
        }
    
        private void ShowLoadingScreen(ResourceLoadingUtils.LoadingProgress progress)
        {
            if (_playerFaction == FactionTag.Ally)
            {
                loadingLight.SetActive(true);
                _loadingImage = loadingFillLight;
            }
            else
            {
                loadingDark.SetActive(false);
                _loadingImage = loadingFillDark;
            }
            _progress = progress;
            progress.ProgressChanged += UpdateLoadingScreen;
        }

        private void UpdateLoadingScreen(float progress)
        {
            _loadingImage.fillAmount = progress;   
        }

        private void HideLoadingScreen()
        {
            loadingLight.SetActive(false);
            loadingDark.SetActive(false);
            _progress.ProgressChanged -= UpdateLoadingScreen;
        }
        
    }
}