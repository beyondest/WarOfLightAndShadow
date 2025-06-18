using SparFlame.Components.General;
using SparFlame.Components.Input;
using Unity.Burst;
using Unity.Entities;
using UnityEngine;

namespace SparFlame.Systems.General.Input
{
    [UpdateAfter(typeof(InputMouseSystem))]
    public partial struct InputCameraNormalModeSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<GameStatusData>();
            state.RequireForUpdate<InputCameraNormalData>();
        }

        public void OnUpdate(ref SystemState state)
        {
            var gameStatus = SystemAPI.GetSingleton<GameStatusData>().Value;
            var customInputActions = InputListener.Instance.GetCustomInputActions();

            if ((gameStatus != GameStatus.SubGaming && gameStatus != GameStatus.MainGaming)
                || !customInputActions.CameraNormalMode.enabled)
            {
                SystemAPI.SetSingleton(new InputCameraNormalData());
                return;
            }
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