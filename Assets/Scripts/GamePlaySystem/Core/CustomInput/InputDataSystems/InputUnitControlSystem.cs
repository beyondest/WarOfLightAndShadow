using SparFlame.BootStrapper;
using SparFlame.GamePlaySystem.General;
using Unity.Burst;
using Unity.Entities;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace SparFlame.GamePlaySystem.CustomInput.GamePlaySystem.Core.CustomInput.InputDataSystems
{
    [UpdateAfter(typeof(InputMouseSystem))]
    public partial struct InputUnitControlSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<InputMouseData>();
            state.RequireForUpdate<NotPauseTag>();
            state.RequireForUpdate<InputUnitControlData>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var customInputActions = InputListener.Instance.GetCustomInputActions();
            var isOverUi = SystemAPI.GetSingleton<InputMouseData>().IsOverUI;
            SystemAPI.SetSingleton(new InputUnitControlData
            {
                Enabled = customInputActions.UnitControl.enabled,
                AddUnit = customInputActions.UnitControl.Add.ReadValue<float>() > 0,
                DragSelectStart = customInputActions.UnitControl.DraggingSelect.WasPressedThisFrame() && !isOverUi,
                DraggingSelect = customInputActions.UnitControl.DraggingSelect.ReadValue<float>() > 0 && !isOverUi,
                DragSelectEnd = customInputActions.UnitControl.DraggingSelect.WasReleasedThisFrame(),
                SingleSelect = customInputActions.UnitControl.SingleSelect.WasPerformedThisFrame() && !isOverUi,
                ChangeFaction = customInputActions.UnitControl.ChangeFaction.WasPerformedThisFrame(),
                Focus = customInputActions.UnitControl.Focus.ReadValue<float>() > 0,
                Command = customInputActions.UnitControl.Command.WasPerformedThisFrame() && !isOverUi,
            });
        }

        [BurstCompile]
        public void OnDestroy(ref SystemState state)
        {

        }
    }
}