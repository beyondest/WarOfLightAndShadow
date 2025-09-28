using System;
using SparFlame.Components.General;
using SparFlame.Components.Input;
using SparFlame.Components.MainGameplay;
using SparFlame.Systems.General.Input;
using Unity.Entities;
using Unity.Transforms;
using UnityEngine;
using Random = Unity.Mathematics.Random;

namespace SparFlame.Systems.General.BasicControl
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

        private GameStatus _previousGameStatus;

        protected override void OnCreate()
        {
            var seed = (uint)DateTime.Now.Ticks;
            EntityManager.CreateSingleton(new GeneralRandom
            {
                Rnd = new Random(seed)
            });
            EntityManager.CreateSingleton(new GameStatusData
            {
                Value = GameStatus.NotStarted
            });
            EntityManager.CreateSingleton(new GameTimeData());
            EntityManager.CreateSingleton(new GameTimeScale
            {
                Value = 1f
            });
            EntityManager.CreateSingleton(new WorldTimeData());
            
            EntityManager.CreateSingleton(new SubGameStatusData
            {
                SubGameStatus = SubGameStatus.None,
                City = Entity.Null,
            });
            EntityManager.CreateSingleton(new CurrentSaveSlot());

            EntityManager.CreateSingleton(new GlobalSingIDCounter
            {
                baseValue = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() * 1000,
                addValue =  0
            });
            EntityManager.CreateSingleton(new CheckFocusPlayerRequest
            {
                EnemyCity = Entity.Null,
            });
            EntityManager.CreateSingleton(new PlayerFactionData());
        }

        protected override void OnStartRunning()
        {
            if (!_initialized && GameController.Instance)
            {
                _customInputActions = InputListener.Instance.GetCustomInputActions();
                Application.targetFrameRate = _gameBasicConfig.targetFrameRate;
                _initialized = true;
                GameController.Instance.OnPause += isSwitching => { PauseOrResumeGame(true, isSwitching); };
                GameController.Instance.OnResume += isSwitching => { PauseOrResumeGame(false, isSwitching); };
                GameController.Instance.OnEcsDestroyInitialization += EcsDestroyInitialization;
                GameController.Instance.OnSwitchGameStatus += EcsSwitchSubGameStatus;
                GameController.Instance.OnEcsBeginSystemInit += BeginSystemInit;
                GameController.Instance.OnSetPlayerFactionData += SetPlayerFactionData;
            }
        }

        private void SetPlayerFactionData(PlayerFactionData data)
        {
            SystemAPI.SetSingleton(data);   
        }

        protected override void OnUpdate()
        {
            if (!_initialized) return;
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
                gameBasicState.ValueRW.Value = GameStatus.Pause;
            }

            _gameBasicConfig = SystemAPI.GetSingleton<GameBasicConfig>();

            if (_gameBasicConfig.enablePause)
                CheckPlayerPauseAction();
        }

        private void BeginSystemInit(  )
        {
            _enterSystemInitState = true;
          
        }

      

        private void EcsDestroyInitialization()
        {
            UnityEngine.Time.timeScale = 1;
            SystemAPI.SetSingleton(new GameStatusData
            {
                Value = GameStatus.NotStarted
            });
            SystemAPI.SetSingleton(new SubGameStatusData
            {
                SubGameStatus = SubGameStatus.None,
                City = Entity.Null,
            });
            if (SystemAPI.HasSingleton<SubGamingTag>())
            {
                EntityManager.DestroyEntity(SystemAPI.GetSingletonEntity<SubGamingTag>());
            }
            if (SystemAPI.HasSingleton<MainGamingTag>())
            {
                EntityManager.DestroyEntity(SystemAPI.GetSingletonEntity<MainGamingTag>());
            }
        }


        private void PauseOrResumeGame(bool isPausing, bool isSwitchingGameplay = false)
        {
            if (_isPaused == isPausing) return;
            var gameStatusData = SystemAPI.GetSingletonRW<GameStatusData>();
            if (isPausing)
            {
                if (!isSwitchingGameplay)
                    UnityEngine.Time.timeScale = 0;
                var gamingTag = gameStatusData.ValueRO.Value == GameStatus.MainGaming
                    ? SystemAPI.GetSingletonEntity<MainGamingTag>()
                    : SystemAPI.GetSingletonEntity<SubGamingTag>();

                _previousGameStatus = gameStatusData.ValueRO.Value;
                gameStatusData.ValueRW.Value = GameStatus.Pause;
                EntityManager.DestroyEntity(gamingTag);
                _isPaused = true;
            }
            else
            {
                UnityEngine.Time.timeScale = SystemAPI.GetSingleton<GameTimeScale>().Value;
                gameStatusData.ValueRW.Value = _previousGameStatus;
                if (_previousGameStatus == GameStatus.MainGaming)
                    EntityManager.CreateSingleton<MainGamingTag>();
                else if (_previousGameStatus == GameStatus.SubGaming)
                {
                    EntityManager.CreateSingleton<SubGamingTag>();
                }
                else
                {
                    throw new ArgumentException(
                        "This should never happen, because pause is valid only when enter game");
                }
                _isPaused = false;
            }
        }


        private void CheckPlayerPauseAction()
        {
            if (!_isPaused && (!Application.isFocused || _customInputActions.ModeSwitch.Pause.WasPerformedThisFrame()))
            {
                GameController.Instance.PauseGame(false);
            }
            else if (_isPaused && Application.isFocused && _customInputActions.ModeSwitch.Pause.WasPerformedThisFrame())
            {
                GameController.Instance.ResumeGame(false);
            }
        }

        private void EcsSwitchSubGameStatus( SubGameStatusData targetSubGameStatusData,
            SubGameStatusData currentSubGameStatusData )
        {
            
            
            if (targetSubGameStatusData.SubGameStatus == SubGameStatus.None)
            {
                SystemAPI.SetSingleton(new GameStatusData
                {
                    Value = GameStatus.MainGaming
                });
                if (SystemAPI.HasSingleton<SubGamingTag>())
                {
                    EntityManager.DestroyEntity(SystemAPI.GetSingletonEntity<SubGamingTag>());
                }
                if (!SystemAPI.HasSingleton<MainGamingTag>())
                {
                    EntityManager.CreateSingleton<MainGamingTag>();
                }
            }
            else
            {
                SystemAPI.SetSingleton(new GameStatusData
                {
                    Value = GameStatus.SubGaming
                });
                if (SystemAPI.HasSingleton<MainGamingTag>())
                {
                    EntityManager.DestroyEntity(SystemAPI.GetSingletonEntity<MainGamingTag>());
                }
                if (!SystemAPI.HasSingleton<SubGamingTag>())
                {
                    EntityManager.CreateSingleton<SubGamingTag>();
                }
            }
            SystemAPI.SetSingleton(targetSubGameStatusData);
        }

    

   
        
    }
}