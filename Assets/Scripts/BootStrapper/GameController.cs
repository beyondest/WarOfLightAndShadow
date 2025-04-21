using System;
using SparFlame.GamePlaySystem.CustomInput;
using UnityEngine;
using Unity.Entities;
using SparFlame.GamePlaySystem.General;
namespace SparFlame.BootStrapper
{
    public class GameController : MonoBehaviour
    {
        public static GameController Instance;
        
        // [SerializeField] private KeyCode pauseKey = KeyCode.Escape;
        [SerializeField] private string mainMenuGroupName = "MainMenuGroup";
        [SerializeField] private string gamingGroupName = "GamingGroup";
        
        
        
        public event Action OnPause;
        public event Action OnResume;
        public event Action<FactionTag> OnGameOver;

        public bool IsGameStarted() => _isGaming;
        
        
        // Internal Data
        private bool _isPaused;
        private bool _isReadyForPlayer;
        private bool _isGaming ;
        private bool _isCrystalDetected;
        private CustomInputActions _customInputActions;
        
        // ECS
        private EntityManager _em;
        private EntityQuery _notPauseTag;
        private EntityQuery _allyTag;
        private EntityQuery _enemyTag;
        
        
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

       

        private void Start()
        {
            _em = World.DefaultGameObjectInjectionWorld.EntityManager;
            SceneController.Instance.OnSceneGroupLoaded += CheckLoadingState;
            SceneController.Instance.OnSceneGroupUnloaded += CheckUnloadingState;
            // SceneController.Instance.LoadSceneGroup(mainMenuGroupName);
            _customInputActions = InputListener.Instance.GetCustomInputActions();
            _notPauseTag = _em.CreateEntityQuery(typeof(NotPauseTag));
            _allyTag = _em.CreateEntityQuery(typeof(AllyCoreCrystalTag));
            _enemyTag = _em.CreateEntityQuery(typeof(EnemyCoreCrystalTag));
        }

        


        private void Update()
        {
            // TODO : Check resources prepared here
            // if(!_isReadyForPlayer) return;
            // Check Player Pause Action
            if(!_isGaming)return;
            
            CheckPlayerPauseAction();
            if (!_isCrystalDetected)
            {
                if(_allyTag.IsEmpty || _enemyTag.IsEmpty)return;
                _isCrystalDetected = true;
            }
            if (_allyTag.IsEmpty)
            {
                OnGameOver?.Invoke(FactionTag.Enemy);
                _isGaming = false;
                return;
            }
            if (_enemyTag.IsEmpty)
            {
                OnGameOver?.Invoke(FactionTag.Ally);
                _isGaming = false;
                return;
            }
        }

        private void CheckPlayerPauseAction()
        {
            if (!_isPaused  && (!Application.isFocused ||_customInputActions.ModeSwitch.Pause.WasPerformedThisFrame()))
            {
                OnPause?.Invoke();
                PauseGame();
            }
            else if (_isPaused && Application.isFocused && _customInputActions.ModeSwitch.Pause.WasPerformedThisFrame())
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
            _isGaming = false;
        }

        public void ExitGame()
        {
            Debug.LogWarning("Exit game.");
            _isGaming = false;
            Application.Quit();
        }

        public void StartGame()
        {
            Debug.Log("Starting game.");
            SceneController.Instance.LoadSceneGroup(gamingGroupName);
        }
        
        
        private void CheckLoadingState(SceneGroup sceneGroup)
        {
            // if(sceneGroup.groupName == mainMenuGroupName)
            //     _isReadyForPlayer = true;
            
            if (sceneGroup.groupName == gamingGroupName)
            {
                
                _isGaming = true;
                InputListener.Instance.EnableNessesaryMaps();
            }
            
        }

        private void CheckUnloadingState(SceneGroup sceneGroup)
        {
            if(sceneGroup.groupName == gamingGroupName)
                _isReadyForPlayer = false;
        }
    }
}