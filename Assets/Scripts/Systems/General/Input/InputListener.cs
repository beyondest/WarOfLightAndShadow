using SparFlame.Components.Input;
using UnityEngine;

namespace SparFlame.Systems.General.Input
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

        public void DisableAllMaps()
        {
            _customInputActions.ModeSwitch.Disable();
            _customInputActions.UnitControl.Disable();
            _customInputActions.Construct.Disable();
            _customInputActions.GeneralShortcut.Disable();
            _customInputActions.Conjure.Disable();
            _customInputActions.CameraFlyMode.Disable();
            _customInputActions.CameraNormalMode.Disable();
        }

        public void EnableSubGameMaps()
        {
            _customInputActions.CameraNormalMode.Enable();
            
            _customInputActions.UnitControl.Enable();
            _customInputActions.ModeSwitch.Enable();
            _customInputActions.GeneralShortcut.Enable();
            _customInputActions.Conjure.Enable();
            
            _customInputActions.ArmyGroupControl.Disable();
        }

        public void EnableMainGameMaps()
        {
            _customInputActions.CameraNormalMode.Enable();
            _customInputActions.ArmyGroupControl.Enable();
            _customInputActions.GeneralShortcut.Enable();

            _customInputActions.UnitControl.Disable();
            _customInputActions.ModeSwitch.Disable();
            _customInputActions.Conjure.Disable();
        }

        #endregion


        private CustomInputActions _customInputActions;

        private void Awake()
        {
            if (!Instance)
                Instance = this;
            else
                Destroy(gameObject);
            _customInputActions = new CustomInputActions();
        }
        


    }
}