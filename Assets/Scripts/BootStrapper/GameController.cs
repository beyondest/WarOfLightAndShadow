using System;
using UnityEngine;
using Unity.Entities;
using SparFlame.GamePlaySystem.General;
namespace SparFlame.BootStrapper
{
    public class GameController : MonoBehaviour
    {
        public static GameController Instance;
        
        [SerializeField] private KeyCode pauseKey = KeyCode.Escape;
        [SerializeField] private string mainMenuGroupName = "MainMenuGroup";
        [SerializeField] private string gamingGroupName = "GamingGroup";
        
        public event Action OnPause;
        public event Action OnResume;


        private EntityManager _em;
        private bool _isPaused;
        private bool _isReadyForPlayer;
        private bool _isGaming ;
        
        
        
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

        private void OnEnable()
        {
            _em = World.DefaultGameObjectInjectionWorld.EntityManager;
        }

        private void Start()
        {
            SceneController.Instance.OnSceneGroupLoaded += CheckLoadingState;
            SceneController.Instance.OnSceneGroupUnloaded += CheckUnloadingState;
            SceneController.Instance.LoadSceneGroup(mainMenuGroupName);
        }

        


        private void Update()
        {
            if(!_isReadyForPlayer) return;
            if (!_isPaused && _isGaming && (!Application.isFocused || Input.GetKeyDown(pauseKey)))
            {
                OnPause?.Invoke();
                PauseGame();
            }
            else if (_isPaused&&_isGaming && Application.isFocused && Input.GetKeyDown(pauseKey))
            {
                OnResume?.Invoke();
                ResumeGame();
            }
        }

        public void PauseGame()
        {
            if (_isPaused)return;
            var pauseRequest = _em.CreateEntity();
            _em.AddComponent<PauseRequest>(pauseRequest);
            _isPaused = true;
        }

        public void ResumeGame()
        {
            if (!_isPaused)return;
            // TODO : why this line need to be added, it will throw em is deallocated if you do not add this
            _em = World.DefaultGameObjectInjectionWorld.EntityManager;
            var resumeRequest = _em.CreateEntity();
            _em.AddComponent<ResumeRequest>(resumeRequest);
            _isPaused = false;
        }

        public void EndGameToMainMenu()
        {
            ResumeGame();
            _isReadyForPlayer = false;
            SceneController.Instance.UnloadSceneGroup(gamingGroupName);
            Debug.LogWarning("Go to main menu without saving.");
            _isGaming = false;
        }

        public void ExitGame()
        {
            Debug.LogWarning("Exit without saving the game.");
            _isGaming = false;
            Application.Quit();
        }

        public void StartGame()
        {
            if (!_isReadyForPlayer) return;
            Debug.Log("Starting game.");
            SceneController.Instance.LoadSceneGroup(gamingGroupName);
        }
        
        
        private void CheckLoadingState(SceneGroup sceneGroup)
        {
            if(sceneGroup.groupName == mainMenuGroupName)
                _isReadyForPlayer = true;
            if(sceneGroup.groupName == gamingGroupName)
                _isGaming = true;
        }

        private void CheckUnloadingState(SceneGroup sceneGroup)
        {
            if(sceneGroup.groupName == gamingGroupName)
                _isReadyForPlayer = true;
        }
    }
}