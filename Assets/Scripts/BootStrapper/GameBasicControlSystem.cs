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
    [UpdateBefore(typeof(GameTimeSystem))]
    public partial class GameBasicControlSystem : SystemBase
    {
        private bool _initialized;
        private bool _isPaused;
        private bool _enterSystemInitState;
        private GameBasicConfig _gameBasicConfig;
        private CustomInputActions _customInputActions;
        private bool _alreadyToWin;

        protected override void OnCreate()
        {
            var seed = (uint)System.DateTime.Now.Ticks;
            EntityManager.CreateSingleton(new GeneralRandom
            {
                Rnd = new Random(seed)
            });
            EntityManager.CreateSingleton(new GameStatusData
            {
                Value = GameStatus.NotStarted
            });
            EntityManager.CreateSingleton(new GameTimeData
            {
                DeltaTime = 0f,
                ElapsedTime = 0f
            });
            EntityManager.CreateSingleton(new GameTimeScale
            {
                Value = 1f
            });
        }

        protected override void OnStartRunning()
        {
            if(GameController.Instance == null)return;
            if (!_initialized)
            {
                _customInputActions = InputListener.Instance.GetCustomInputActions();
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
            if(!_initialized) return;
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
                _alreadyToWin = false;
                gameBasicState.ValueRW.Value = GameStatus.Gaming;
                var gaming = EntityManager.CreateEntity();
                EntityManager.AddComponent<GamingTag>(gaming);
                InputListener.Instance.EnableNecessaryMaps();
                GameController.Instance.GameStart();
            }

            _gameBasicConfig = SystemAPI.GetSingleton<GameBasicConfig>();
            var playerFaction = SystemAPI.GetSingleton<PlayerFactionData>().Value;

            var enemyCrystalInfo = SystemAPI.GetSingleton<EnemyCrystalInfo>();
            var playerCrystalInfo = SystemAPI.GetSingleton<PlayerCrystalInfo>();
            if (!_alreadyToWin)
            {
                if (playerCrystalInfo.TotalCount != 0 && enemyCrystalInfo.TotalCount != 0)
                    _alreadyToWin = true;
                else
                {
                    return;
                }
            }

            if (_gameBasicConfig.enablePause)
                CheckPlayerPauseAction();
            if (enemyCrystalInfo.TotalCount == 0)
            {
                SystemAPI.SetSingleton(new GameStatusData
                {
                    Value = GameStatus.NotStarted
                });
                GameController.Instance.GameOver(playerFaction);
                _alreadyToWin = false;
            }
            else if(playerCrystalInfo.TotalCount == 0)
            {
                SystemAPI.SetSingleton(new GameStatusData
                {
                    Value = GameStatus.NotStarted
                });
                GameController.Instance.GameOver(~playerFaction);
                _alreadyToWin = false;
            }
        }

        private void BeginSystemInit()
        {
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

            if (SystemAPI.HasSingleton<PlayerFactionData>())
            {
                EntityManager.DestroyEntity(SystemAPI.GetSingletonEntity<PlayerFactionData>());
            }

            if (SystemAPI.HasSingleton<GamingTag>())
            {
                EntityManager.DestroyEntity(SystemAPI.GetSingletonEntity<GamingTag>());
            }

            var clearRequest = EntityManager.CreateEntity();
            EntityManager.AddComponent<ClearGameplayEntities>(clearRequest);
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