using System;
using UnityEngine;
using SparFlame.GamePlaySystem.General;
namespace SparFlame.BootStrapper
{
    public class GameController : MonoBehaviour
    {
        public static GameController Instance;
        
        public event Action OnPause;
        public event Action OnResume;
        public event Action OnBackToMainMenu;
        // faction is winner
        public event Action<FactionTag> OnGameOver;

        public event Action<FactionTag> OnPlayerChooseFaction;
        // float is start elapsed time

        public event Action OnGameStart; 
        
        public void GameOver(FactionTag winnerFaction)
        {
            OnGameOver?.Invoke(winnerFaction);
            PauseGame();
        }
        
        public void PauseGame()
        {
            OnPause?.Invoke();
        }

        public void ResumeGame()
        {
            OnResume?.Invoke();
        }
        
        public void EndGameToMainMenu()
        {
            ResumeGame();
            OnBackToMainMenu?.Invoke();
        }

        public void ExitGame()
        {
            Debug.LogWarning("Exit game.");
            Application.Quit();
        }

        public void PlayerChooseFaction(FactionTag playerFaction)
        {
            OnPlayerChooseFaction?.Invoke(playerFaction);
        }

        public void GameStart()
        {
            OnGameStart?.Invoke();
        }

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
            }
        }

        
        
    }
}