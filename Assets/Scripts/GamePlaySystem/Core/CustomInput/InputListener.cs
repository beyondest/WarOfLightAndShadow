using System;
using SparFlame.GamePlaySystem.CustomInput;
using UnityEngine;

namespace SparFlame.BootStrapper
{
    public class InputListener : MonoBehaviour
    {
        public static InputListener Instance;


        public CustomInputActions GetCustomInputActions()
        {
            return _customInputActions;
        }
        
        #region MapSwitch Methods

        public void ToggleConstructMap()
        {
            if (!_customInputActions.Construct.enabled)
            {
                _customInputActions.Construct.Enable();
                _customInputActions.UnitControl.Disable();
                _customInputActions.ModeSwitch.Disable();
            }
            else
            {
                _customInputActions.Construct.Disable();
                _customInputActions.UnitControl.Enable();
                _customInputActions.ModeSwitch.Enable();
            }
        }
        #endregion


        private CustomInputActions _customInputActions;

        private void Awake()
        {
            if (Instance == null)
                Instance = this;
            else
                Destroy(gameObject);
            _customInputActions = new CustomInputActions();
        }

        private void OnEnable()
        {
            _customInputActions.UnitControl.Enable();
            _customInputActions.CameraNormalMode.Enable();
            _customInputActions.ModeSwitch.Enable();
            _customInputActions.InfoWindow.Enable();
        }
    }
}