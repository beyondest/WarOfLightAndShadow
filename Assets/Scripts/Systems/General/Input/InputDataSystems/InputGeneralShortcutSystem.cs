using SparFlame.Components.Input;
using Unity.Entities;
using UnityEngine;

namespace SparFlame.Systems.General.Input
{
    public partial class InputGeneralShortcutSystem : SystemBase
    {
        private CustomInputActions _customInputActions;
        
        protected override void OnCreate()
        {
            RequireForUpdate<InputGeneralShortcutData>();
        }

        protected override void OnStartRunning()
        {
            _customInputActions = InputListener.Instance.GetCustomInputActions();
        }

        protected override void OnUpdate()
        {
            var isOverInput = SystemAPI.GetSingleton<IsOverInputText>().IsOver;
            if (!_customInputActions.GeneralShortcut.enabled)
            {
                SystemAPI.SetSingleton(new InputGeneralShortcutData());
                return;
            }
            SystemAPI.SetSingleton(new InputGeneralShortcutData
            {
                Wait = _customInputActions.GeneralShortcut.Wait.WasPerformedThisFrame() && !isOverInput,
                CheckInfo = _customInputActions.GeneralShortcut.CheckInfo.WasPerformedThisFrame(),
                CloseWindow = _customInputActions.GeneralShortcut.CloseWindow.WasPerformedThisFrame(),
            });
        }
    }
}