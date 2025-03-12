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
            if (GameController.instance == null)
            {
                Debug.LogError("BootStrapper scene needs to be placed at first place");
            }
            GameController.instance.OnPause += ShowPauseMenu;
            GameController.instance.OnResume += HidePauseMenu;
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
            GameController.instance.PauseGame();
        }

        public void ButtonResumeClicked()
        {
            HidePauseMenu();
            GameController.instance.ResumeGame();
        }

        public void ButtonExitClicked()
        {
            GameController.instance.ExitGame();
        }

        public void ButtonGoToMainMenuClicked()
        {
            HidePauseMenu();
            ShowMainMenu();
            GameController.instance.EndGameToMainMenu();
        }

        public void ButtonPlayClicked()
        {
            HideMainMenu();
            GameController.instance.StartGame();
        }

        public void ButtonContinueClicked()
        {
            Debug.LogWarning("Continue to last saving is not implemented yet");
        }
        
        #endregion

        
    }
}
