using SparFlame.GamePlaySystem.CustomInput;
using SparFlame.GamePlaySystem.General;
using Unity.Entities;
using UnityEngine;
using Random = Unity.Mathematics.Random;

namespace SparFlame.BootStrapper
{
    /// <summary>
    /// The only system that should not rely on anything, and keep running all the time
    /// This system runs the first of all custom systems
    /// </summary>
    [UpdateInGroup(typeof(InitializationSystemGroup))]
    public partial class GameBasicControlSystem : SystemBase
    {
        private bool _initialized;
        private bool _isPaused;
        private bool _enterSystemInitState;
        private EntityQuery _playerCrystalQuery;
        private GameBasicConfig _gameBasicConfig;
        private CustomInputActions _customInputActions;

        protected override void OnCreate()
        {
            var seed = (uint)System.DateTime.Now.Ticks;
            EntityManager.CreateSingleton(new GeneralRandom
            {
                Rnd = new Random(seed)
            });
            RequireForUpdate<GameBasicConfig>();
            _playerCrystalQuery = SystemAPI.QueryBuilder().WithAll<CoreCrystalTag>().WithAll<PlayerTag>().Build();
        }

        protected override void OnStartRunning()
        {
            if (!_initialized )
            {
                _customInputActions = InputListener.Instance.GetCustomInputActions();
                _gameBasicConfig = SystemAPI.GetSingleton<GameBasicConfig>();
                Application.targetFrameRate = _gameBasicConfig.targetFrameRate;
                _initialized = true;
                GameController.Instance.OnPause += () => { PauseGame(true); };
                GameController.Instance.OnResume += () => { PauseGame(false); };
                GameController.Instance.OnBackToMainMenu += DestroyInitialization;
                GameController.Instance.OnPlayerChooseFaction += SetPlayerFaction;
                GeneralResourceManager.Instance.OnAllResourceLoaded += BeginSystemInit;
            }
        }

        protected override void OnUpdate()
        {
            var gameBasicState = SystemAPI.GetSingletonRW<GameStatusData>();
            if (_enterSystemInitState)
            {
                _enterSystemInitState = false;
                gameBasicState.ValueRW.Value = GameStatus.Init;
                return;
            }
            if (gameBasicState.ValueRW.Value == GameStatus.NotStarted) return;
            
            if (gameBasicState.ValueRW.Value == GameStatus.Init) // This is the time all systems init complete
            {
                gameBasicState.ValueRW.Value = GameStatus.Gaming;
                EntityManager.CreateSingleton(new GamingTag());
                InputListener.Instance.EnableNecessaryMaps();
                GameController.Instance.GameStart();
            }
            var playerFaction = SystemAPI.GetSingleton<PlayerFactionData>().Value;
            if (!_gameBasicConfig.enablePause)
                CheckPlayerPauseAction();
            if (!_playerCrystalQuery.IsEmpty)
                GameController.Instance.GameOver(~playerFaction);
        }

        private void BeginSystemInit(float startGameTime)
        {
            EntityManager.CreateSingleton(new GameStartTime
            {
                Value = startGameTime
            });
            _enterSystemInitState = true;
        }

        private void SetPlayerFaction(FactionTag factionTag)
        {
            EntityManager.CreateSingleton(new PlayerFactionData
            {
                Value = factionTag
            });
        }

        private void DestroyInitialization()
        {
            InputListener.Instance.DisableAllMaps();
            SystemAPI.SetSingleton(new GameStatusData
            {
                Value = GameStatus.NotStarted
            });

            if (SystemAPI.HasSingleton<GameStartTime>())
            {
                EntityManager.DestroyEntity(SystemAPI.GetSingletonEntity<GameStartTime>());
            }

            if (SystemAPI.HasSingleton<PlayerFactionData>())
            {
                EntityManager.DestroyEntity(SystemAPI.GetSingletonEntity<PlayerFactionData>());
            }

            if (SystemAPI.HasSingleton<GamingTag>())
            {
                EntityManager.DestroyEntity(SystemAPI.GetSingletonEntity<GamingTag>());
            }
            
        }

        private void PauseGame(bool isPausing)
        {
            if (_isPaused == isPausing) return;
            if (isPausing)
            {
                UnityEngine.Time.timeScale = 0;
                var gamingTag = SystemAPI.GetSingletonEntity<GamingTag>();
                SystemAPI.SetSingleton(new GameStatusData
                {
                    Value = GameStatus.Pause
                });
                EntityManager.DestroyEntity(gamingTag);
                _isPaused = true;
                
            }
            else
            {
                UnityEngine.Time.timeScale = 1;
                EntityManager.CreateSingleton<GamingTag>();
                SystemAPI.SetSingleton(new GameStatusData
                {
                    Value = GameStatus.Gaming
                });
                _isPaused = false;
            }
        }


        private void CheckPlayerPauseAction()
        {
            if (!_isPaused && (!Application.isFocused || _customInputActions.ModeSwitch.Pause.WasPerformedThisFrame()))
            {
                GameController.Instance.PauseGame();
            }
            else if (_isPaused && Application.isFocused && _customInputActions.ModeSwitch.Pause.WasPerformedThisFrame())
            {
                GameController.Instance.ResumeGame();
            }
        }
    }
}