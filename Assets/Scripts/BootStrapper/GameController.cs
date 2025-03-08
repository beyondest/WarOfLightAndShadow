using System;
using UnityEngine;
using Unity.Entities;
using SparFlame.GamePlaySystem.General;
namespace SparFlame.BootStrapper
{
    public class GameController : MonoBehaviour
    {
        public static GameController instance;
        
        [SerializeField] private KeyCode pauseKey = KeyCode.Escape;
        public static event Action OnPause;
        public static event Action OnResume;


        private EntityManager _em;
        private bool _isPaused;
        private bool _isGaming = true;
        private void Awake()
        {
            if (instance == null)
            {
                instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
            }
        }
        

        private void OnEnable()
        {
            OnPause += Pause;
            OnResume += Resume;
            _em = World.DefaultGameObjectInjectionWorld.EntityManager;
        }

       

        private void Update()
        {
            if (!_isPaused && _isGaming && (!Application.isFocused || Input.GetKeyDown(pauseKey)))
            {
                OnPause?.Invoke();
            }

            else if (_isPaused&&_isGaming && Application.isFocused && Input.GetKeyDown(pauseKey))
            {
                OnResume?.Invoke();
            }
        }

        public void Pause()
        {
            if (_isPaused)return;
            var pauseRequest = _em.CreateEntity();
            _em.AddComponent<PauseRequest>(pauseRequest);
            _isPaused = true;
        }

        public void Resume()
        {
            if (!_isPaused)return;
            // TODO : why this line need to be added, it will throw em is deallocated if you do not add this
            _em = World.DefaultGameObjectInjectionWorld.EntityManager;
            var resumeRequest = _em.CreateEntity();
            _em.AddComponent<ResumeRequest>(resumeRequest);
            _isPaused = false;
        }

        public void GoToMainMenu()
        {
            Debug.LogWarning("You haven't taken any measures to ensure the game can safely transition from the paused state to the main menu.");
            _isGaming = false;
        }
    }
}