using SparFlame.BootStrapper;
using SparFlame.GamePlaySystem.General;
using Unity.Burst;
using Unity.Entities;
using UnityEngine;

namespace SparFlame.GamePlaySystem.CustomInput.GamePlaySystem.Core.CustomInput.InputDataSystems
{
    [UpdateAfter(typeof(InputMouseSystem))]
    public partial struct InputCameraNormalModeSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<NotPauseTag>();
            state.RequireForUpdate<InputCameraNormalData>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var customInputActions = InputListener.Instance.GetCustomInputActions();
            var oriMode = SystemAPI.GetSingleton<InputCameraNormalData>().EdgeScrolling;
            SystemAPI.SetSingleton(new InputCameraNormalData
            {
                Enabled = customInputActions.CameraNormalMode.enabled,
                Movement = customInputActions.CameraNormalMode.MoveCamera.ReadValue<Vector2>(),
                RotateCamera = customInputActions.CameraNormalMode.RotateCamera.ReadValue<Vector2>().x,
                ZoomCamera = customInputActions.CameraNormalMode.ZoomCamera.ReadValue<Vector2>(),
                DraggingCamera = customInputActions.CameraNormalMode.DragCamera.ReadValue<float>() > 0f,
                DragCameraStart = customInputActions.CameraNormalMode.DragCamera.WasPressedThisFrame(),
                SpeedUp = customInputActions.CameraNormalMode.SpeedUp.ReadValue<float>() > 0f,
                EdgeScrolling = customInputActions.CameraNormalMode.SwtichEdgeScrolling.WasPressedThisFrame()
                    ? !oriMode
                    : oriMode
            });
        }
    }
}