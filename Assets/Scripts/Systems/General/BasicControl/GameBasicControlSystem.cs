using System;
using System.IO;
using SparFlame.Components.General;
using SparFlame.Components.Input;
using SparFlame.Core.Utils;
using SparFlame.Systems.General.Input;
using Unity.Entities;
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

        // private bool _alreadyToWin;
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
            EntityManager.CreateSingleton(new GameTimeData
            {
            });
            EntityManager.CreateSingleton(new GameTimeScale
            {
                Value = 1f
            });
            EntityManager.CreateSingleton(new WorldTimeData());
            
            EntityManager.CreateSingleton(new SubGameStatusData
            {
                SubGameStatus = SubGameStatus.None,
                City = Entity.Null,
                BattleTriggerRequest = default
            });
            EntityManager.CreateSingleton(new PlayerSaveSlot());

            EntityManager.CreateSingleton(new LastUniqueId());
            EntityManager.CreateSingleton(new SaveCityId());
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
                GameController.Instance.OnBackToMainMenu += DestroyInitialization;
                GameController.Instance.OnPlayerChooseFaction += SetPlayerFaction;
                GeneralResourceManager.Instance.OnAllResourceLoaded += BeginSystemInit;
                GameController.Instance.OnSwitchGameStatusForSystems += SwitchGameStatusForSystems;
                GameController.Instance.OnPlayerChooseSavingSlot += ChooseSavingSlot;
            }
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
                // _alreadyToWin = false;
                gameBasicState.ValueRW.Value = GameStatus.MainGaming;
                var gaming = EntityManager.CreateEntity();
                EntityManager.AddComponent<MainGamingTag>(gaming);
              
                GameController.Instance.MainGameStartForPlayer();
            }

            _gameBasicConfig = SystemAPI.GetSingleton<GameBasicConfig>();
            // var playerFaction = SystemAPI.GetSingleton<PlayerFactionData>().Value;
            //
            // var enemyCrystalInfo = SystemAPI.GetSingleton<EnemyCrystalInfo>();
            // var playerCrystalInfo = SystemAPI.GetSingleton<PlayerCrystalInfo>();
            // if (!_alreadyToWin)
            // {
            //     if (playerCrystalInfo.TotalCount != 0 && enemyCrystalInfo.TotalCount != 0)
            //         _alreadyToWin = true;
            //     else
            //     {
            //         return;
            //     }
            // }

            if (_gameBasicConfig.enablePause)
                CheckPlayerPauseAction();
            // if (enemyCrystalInfo.TotalCount == 0)
            // {
            //     SystemAPI.SetSingleton(new GameStatusData
            //     {
            //         Value = GameStatus.NotStarted
            //     });
            //     GameController.Instance.GameOver(playerFaction);
            //     _alreadyToWin = false;
            // }
            // else if(playerCrystalInfo.TotalCount == 0)
            // {
            //     SystemAPI.SetSingleton(new GameStatusData
            //     {
            //         Value = GameStatus.NotStarted
            //     });
            //     GameController.Instance.GameOver(~playerFaction);
            //     _alreadyToWin = false;
            // }
        }

        private void BeginSystemInit()
        {
            _enterSystemInitState = true;
        }

        private void SetPlayerFaction(FactionTag factionTag)
        {
            EntityManager.CreateSingleton(new PlayerFactionData
            {
                faction = factionTag,
                subFaction = SubFactionTag.None
            });
        }

        private void DestroyInitialization()
        {
            InputListener.Instance.DisableAllMaps();
            SystemAPI.SetSingleton(new GameStatusData
            {
                Value = GameStatus.NotStarted
            });
            SystemAPI.SetSingleton(new SubGameStatusData
            {
                SubGameStatus = SubGameStatus.None,
                City = Entity.Null,
                BattleTriggerRequest = default
            });
           
            EntityManager.DestroyEntity(SystemAPI.GetSingletonEntity<PlayerFactionData>());

            if (SystemAPI.HasSingleton<SubGamingTag>())
            {
                EntityManager.DestroyEntity(SystemAPI.GetSingletonEntity<SubGamingTag>());
            }

            if (SystemAPI.HasSingleton<MainGamingTag>())
            {
                EntityManager.DestroyEntity(SystemAPI.GetSingletonEntity<MainGamingTag>());
            }
            GameController.Instance.DestroyGameplayEntities(ClearGameplayEntitiesType.All);
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

        private void SwitchGameStatusForSystems( SubGameStatusData targetSubGameStatusData)
        {
            if (targetSubGameStatusData.SubGameStatus == SubGameStatus.None)
            {
                SystemAPI.SetSingleton(new GameStatusData
                {
                    Value = GameStatus.MainGaming
                });
                EntityManager.DestroyEntity(SystemAPI.GetSingletonEntity<SubGamingTag>());
                EntityManager.CreateSingleton<MainGamingTag>();
            }
            else
            {
                SystemAPI.SetSingleton(new GameStatusData
                {
                    Value = GameStatus.SubGaming
                });
                EntityManager.DestroyEntity(SystemAPI.GetSingletonEntity<MainGamingTag>());
                EntityManager.CreateSingleton<SubGamingTag>();
            }

            SystemAPI.SetSingleton(targetSubGameStatusData);
        }

        private void ChooseSavingSlot(int slot, bool ifNew)
        {
            SystemAPI.SetSingleton(new PlayerSaveSlot { Value = slot });
            var path = FolderPathUtils.GetPlayerSaveSlotFolder(slot);
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }
        }

   
        
    }
}