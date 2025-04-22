using SparFlame.BootStrapper;
using SparFlame.GamePlaySystem.General;
using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace SparFlame.GamePlaySystem.CustomInput
{
    [UpdateAfter(typeof(InputMouseSystem))]
    public partial class InputUnitControlSystem : SystemBase
    {
        private int _commandCounter;

        protected override void OnCreate()
        {
            RequireForUpdate<InputMouseData>();
            RequireForUpdate<NotPauseTag>();
            RequireForUpdate<InputUnitControlData>();
            _commandCounter = 0;
        }

        protected override void OnUpdate()
        {
            var customInputActions = InputListener.Instance.GetCustomInputActions();
            var isOverUi = SystemAPI.GetSingleton<InputMouseData>().IsOverUI;
            if (customInputActions.UnitControl.Command.WasPerformedThisFrame() && !isOverUi)
            {
                _commandCounter = 2;
            }
            else
            {
                _commandCounter = math.clamp(_commandCounter - 1, 0, 2);
            }
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
                MoveOutSameIdUnits = customInputActions.UnitControl.MoveOutSameIdUnits.ReadValue<float>() >0,
            });
        }
    }
}