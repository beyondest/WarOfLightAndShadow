using SparFlame.BootStrapper;
using SparFlame.GamePlaySystem.General;
using Unity.Burst;
using Unity.Entities;
using UnityEngine;

namespace SparFlame.GamePlaySystem.CustomInput.GamePlaySystem.Core.CustomInput.InputDataSystems
{
    [UpdateAfter(typeof(InputMouseSystem))]
    public partial class InputCameraFlyModeSystem : SystemBase
    {
        private CustomInputActions _customInputActions;
        protected override void OnCreate()
        {
            RequireForUpdate<NotPauseTag>();
            RequireForUpdate<InputCameraNormalData>();
        }
        protected override void OnStartRunning()
        {
            _customInputActions = InputListener.Instance.GetCustomInputActions();
            _customInputActions.ModeSwitch.SwitchCameraFly.performed += _ => { ToggleCameraFlyMode(); };
            _customInputActions.CameraFlyMode.Exit.performed += _ => { ToggleCameraFlyMode(); };
        }

        protected override void OnUpdate()
        {
            SystemAPI.SetSingleton(new InputCameraFlyData
            {
                Enabled = _customInputActions.CameraFlyMode.enabled,
                LookDelta = _customInputActions.CameraFlyMode.Look.ReadValue<Vector2>(),
                Move = _customInputActions.CameraFlyMode.Move.ReadValue<Vector2>(),
                Zoom = _customInputActions.CameraFlyMode.Zoom.ReadValue<Vector2>(),
                FlyUp = _customInputActions.CameraFlyMode.FlyUp.ReadValue<float>() > 0f,
                FlyDown = _customInputActions.CameraFlyMode.FlyDown.ReadValue<float>() > 0f,
                SpeedUp = _customInputActions.CameraFlyMode.SpeedUp.ReadValue<float>() > 0f,
            });
        }

        protected override void OnDestroy()
        {
            
        }

        private void ToggleCameraFlyMode()
        {
            if (!_customInputActions.CameraFlyMode.enabled)
            {
                _customInputActions.CameraFlyMode.Enable();
                _customInputActions.CameraNormalMode.Disable();
                _customInputActions.ModeSwitch.Disable();
            }
            else
            {
                _customInputActions.CameraFlyMode.Disable();
                _customInputActions.CameraNormalMode.Enable();
                _customInputActions.ModeSwitch.Enable();
            }
        }


    }

    
}