using System;
using UnityEngine;
using SparFlame.BootStrapper;
using UnityEngine.UI;

namespace SparFlame.UI.Menu.Out
{
    public class MenuOutController : MonoBehaviour
    {
        [SerializeField] private GameObject pauseMenu;
        [SerializeField] private GameObject mainMenu;
        [SerializeField] private Button resumeButton;
        [SerializeField] private Button goToMainMenuButton;
        [SerializeField] private Button exitButton;
        // Start is called once before the first execution of Update after the MonoBehaviour is created

        private void OnEnable()
        {
            GameController.OnPause += ShowPauseMenu;
            GameController.OnResume += HidePauseMenu;
            resumeButton.onClick.AddListener((() => GameController.instance.Resume()));
            resumeButton.onClick.AddListener((HidePauseMenu));
            goToMainMenuButton.onClick.AddListener(ShowMainMenu);
            goToMainMenuButton.onClick.AddListener(HidePauseMenu);
            goToMainMenuButton.onClick.AddListener((() => GameController.instance.GoToMainMenu()));
            exitButton.onClick.AddListener(Application.Quit);
        }



        private void ShowPauseMenu()
        {
            pauseMenu.SetActive(true);
        }

        private void HidePauseMenu()
        {
            pauseMenu.SetActive(false);
        }

        private void ShowMainMenu()
        {
            mainMenu.SetActive(true);
        }

        private void HideMainMenu()
        {
            mainMenu.SetActive(false);
        }


    }
}
