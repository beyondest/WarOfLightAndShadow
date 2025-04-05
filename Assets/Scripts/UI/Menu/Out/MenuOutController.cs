using UnityEngine;
using SparFlame.BootStrapper;
namespace SparFlame.UI.Menu.Out
{
    public class MenuOutController : MonoBehaviour
    {
        [SerializeField] private GameObject pauseMenu;
        [SerializeField] private GameObject mainMenu;
   
        // Start is called once before the first execution of Update after the MonoBehaviour is created

        private void OnEnable()
        {
            if (GameController.Instance == null)
            {
                Debug.LogError("BootStrapper scene needs to be placed at first place");
            }
            GameController.Instance.OnPause += ShowPauseMenu;
            GameController.Instance.OnResume += HidePauseMenu;
        }

        #region MenuMethods

        

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
