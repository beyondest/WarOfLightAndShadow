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
        [Header("Gameplay UI Panel")]
        [SerializeField] private GameObject subGameplayUI;
        [SerializeField] private GameObject mainGameplayUI;
        
        [Header("Menus")]
        [SerializeField] private GameObject pauseMenu;
        [SerializeField] private GameObject mainMenu;
        [SerializeField] private GameObject gameOverMenu;
        [SerializeField] private GameObject selectMenu;
        [SerializeField] private GameObject selectMenuElements;
        [Header("Loading Screen")]
        [SerializeField] private GameObject loadingLight;
        [SerializeField] private GameObject loadingDark;
        [SerializeField] private Image loadingFillLight;
        [SerializeField] private Image loadingFillDark;
        
        [Header("Settings")]
        [SerializeField] private GameObject settings;

        [Header("GameOver Menu")]
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
            GameController.Instance.ResumeGame(false);
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
            selectMenuElements.SetActive(true);
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
            GameController.Instance.OnPause += PauseGame;
            GameController.Instance.OnResume += ResumeGame;
            GameController.Instance.OnWinnerWin += WinnerWin;
            GameController.Instance.OnSubGameStartForPlayer += SubGameStartForPlayer;
            GameController.Instance.OnMainGameStartForPlayer += MainGameStartForPlayer;
            GameController.Instance.OnPlayerChooseFaction += PlayerChooseFaction;
            // Init loading screen
            GeneralResourceManager.Instance.OnLoadAllResources += () =>
                ShowLoadingScreen(GeneralResourceManager.Instance.LoadingProgress);
            
            GameController.Instance.OnSwitchStatusLoadingProgress += ShowLoadingScreen;
            // Switch gameplay loading screen
            
            mainMenu.SetActive(true);
            loadingLight.SetActive(false);
            loadingDark.SetActive(false);
            selectMenu.SetActive(false);
            selectMenuElements.SetActive(false);
            pauseMenu.SetActive(false);
            gameOverMenu.SetActive(false);
            settings.SetActive(false);
            mainGameplayUI.SetActive(false);
            subGameplayUI.SetActive(false);
        }

        #endregion


        private void PauseGame(bool isSwitching)
        {
            if(isSwitching)return;  
            pauseMenu.SetActive(true);
        }

        private void ResumeGame(bool isSwitching)
        {
            if(isSwitching)return;
            pauseMenu.SetActive(false);
        }
        private void SubGameStartForPlayer()
        {
            HideLoadingScreen();
            
            subGameplayUI.SetActive(true);
            mainGameplayUI.SetActive(false);
        }

        private void MainGameStartForPlayer()
        {
            HideLoadingScreen();
            selectMenu.SetActive(false);
            selectMenuElements.SetActive(false);
            mainGameplayUI.SetActive(true);
            subGameplayUI.SetActive(false);
        }

        private void WinnerWin(FactionTag winner)
        {
            gameOverImage.sprite = BasicUIResourceManager.Instance.FactionGameOverSprites[winner];
            gameOverMenu.SetActive(true);
            subGameplayUI.SetActive(false);
            gameOverText.text = winner == _playerFaction ? "Victory" : "Defeat";
        }
    
        private void ShowLoadingScreen(ResourceLoadingUtils.LoadingProgress progress)
        {
            mainGameplayUI.SetActive(false);
            subGameplayUI.SetActive(false);
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
        private void PlayerChooseFaction(FactionTag faction)
        {
            _playerFaction = faction;
            selectMenu.SetActive(false);
            selectMenuElements.SetActive(false);
            loadingLight.SetActive(faction == FactionTag.Ally);
            loadingDark.SetActive(faction == FactionTag.Enemy);
        }
    }
}