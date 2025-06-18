using SparFlame.Components.Input;
using Unity.Entities;
using UnityEngine.InputSystem;

namespace SparFlame.Systems.General.Input
{
    public partial class InputArmyGroupControlSystem : SystemBase
    {
        
        private CustomInputActions _customInputActions;
        private float _pressedTime;
        protected override void OnCreate()
        {
            RequireForUpdate<InputArmyGroupControlData>();
        }

        protected override void OnStartRunning()
        {
            _customInputActions = InputListener.Instance.GetCustomInputActions();
            _customInputActions.ArmyGroupControl.ResetAllTargets.canceled += ResetAllCanceled;
        }

        private void ResetAllCanceled(InputAction.CallbackContext obj)
        {
           SystemAPI.SetSingleton(new CircleCursorData());
           _pressedTime = 0;
        }

        protected override void OnUpdate()
        {
            
            if (!_customInputActions.ArmyGroupControl.enabled)
            {
                SystemAPI.SetSingleton(new InputArmyGroupControlData());
                return;
            }
            
            var isOverUi = SystemAPI.GetSingleton<InputMouseData>().IsOverUI;
            SystemAPI.SetSingleton(new InputArmyGroupControlData
            {
                Enabled = _customInputActions.ArmyGroupControl.enabled,
                AddArmyGroup = _customInputActions.ArmyGroupControl.Add.ReadValue<float>() > 0,
                DragSelectStart = _customInputActions.ArmyGroupControl.DraggingSelect.WasPressedThisFrame() && !isOverUi,
                DraggingSelect = _customInputActions.ArmyGroupControl.DraggingSelect.ReadValue<float>() > 0 && !isOverUi,
                DragSelectEnd = _customInputActions.ArmyGroupControl.DraggingSelect.WasReleasedThisFrame(),
                SingleSelect = _customInputActions.ArmyGroupControl.SingleSelect.WasPerformedThisFrame() && !isOverUi,
                ChangeFaction = _customInputActions.ArmyGroupControl.ChangeFaction.WasPerformedThisFrame(),
                StartMoving = _customInputActions.ArmyGroupControl.StartMoving.WasPerformedThisFrame() ,
                SetTarget = _customInputActions.ArmyGroupControl.SetTarget.WasPerformedThisFrame() && !isOverUi,
                EndMovingAndClearAllTargets = _customInputActions.ArmyGroupControl.EndMoving.WasPerformedThisFrame() ,
                ClearAllTargets = _customInputActions.ArmyGroupControl.ResetAllTargets.WasPerformedThisFrame() ,
                DeleteLastTarget = _customInputActions.ArmyGroupControl.ResetOnlyTheLastTarget.WasPerformedThisFrame() ,
            });
            
            var deltaTime = SystemAPI.Time.DeltaTime;
            if (_customInputActions.ArmyGroupControl.ResetAllTargets.ReadValue<float>() > 0)
            {
                _pressedTime += deltaTime;
                var config = SystemAPI.GetSingleton<InputArmyGroupControlConfig>();
                if (_pressedTime >config.MinPressedTimeForCircleShow)
                {
                    var circleCursorData = SystemAPI.GetSingletonRW<CircleCursorData>();
                    circleCursorData.ValueRW.FillAmount = _pressedTime/ config.HoldTimeForTriggerClearAllTargets;
                }
                
            }
        }

       
    }
}