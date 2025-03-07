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
        private void Awake()
        {
            _em = World.DefaultGameObjectInjectionWorld.EntityManager;
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
        }

        private void Update()
        {
            if (!Application.isFocused || Input.GetKeyDown(pauseKey))
            {
                OnPause?.Invoke();
            }

            if (Application.isFocused && Input.GetKeyDown(pauseKey) && _isPaused)
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
            var resumeRequest = _em.CreateEntity();
            _em.AddComponent<ResumeRequest>(resumeRequest);
            _isPaused = false;
        }
    }
}