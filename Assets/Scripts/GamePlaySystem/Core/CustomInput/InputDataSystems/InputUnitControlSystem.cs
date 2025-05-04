using SparFlame.GamePlaySystem.General;
using Unity.Entities;
using Unity.Mathematics;

namespace SparFlame.GamePlaySystem.CustomInput
{
    [UpdateAfter(typeof(InputMouseSystem))]
    public partial class InputUnitControlSystem : SystemBase
    {
        private CustomInputActions _customInputActions;
        
        protected override void OnCreate()
        {
            RequireForUpdate<InputMouseData>();
            RequireForUpdate<GamingTag>();
            RequireForUpdate<InputUnitControlData>();
        }

        protected override void OnStartRunning()
        {
            _customInputActions = InputListener.Instance.GetCustomInputActions();
        }

        protected override void OnUpdate()
        {
            var isOverUi = SystemAPI.GetSingleton<InputMouseData>().IsOverUI;
            SystemAPI.SetSingleton(new InputUnitControlData
            {
                Enabled = _customInputActions.UnitControl.enabled,
                AddUnit = _customInputActions.UnitControl.Add.ReadValue<float>() > 0,
                DragSelectStart = _customInputActions.UnitControl.DraggingSelect.WasPressedThisFrame() && !isOverUi,
                DraggingSelect = _customInputActions.UnitControl.DraggingSelect.ReadValue<float>() > 0 && !isOverUi,
                DragSelectEnd = _customInputActions.UnitControl.DraggingSelect.WasReleasedThisFrame(),
                SingleSelect = _customInputActions.UnitControl.SingleSelect.WasPerformedThisFrame() && !isOverUi,
                ChangeFaction = _customInputActions.UnitControl.ChangeFaction.WasPerformedThisFrame(),
                Focus = _customInputActions.UnitControl.Focus.ReadValue<float>() > 0,
                Command = _customInputActions.UnitControl.Command.WasPerformedThisFrame() && !isOverUi,
                MoveOutSameIdUnits = _customInputActions.UnitControl.MoveOutSameIdUnits.ReadValue<float>() >0,
            });
        }
    }
}