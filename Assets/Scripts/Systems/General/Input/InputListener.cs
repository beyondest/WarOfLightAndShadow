using System;
using SparFlame.Components.Input;
using TMPro;
using Unity.Entities;
using UnityEngine;
using UnityEngine.EventSystems;

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
        private EntityQuery _overInputText;

        private void Awake()
        {
            if (!Instance)
                Instance = this;
            else
                Destroy(gameObject);
            _customInputActions = new CustomInputActions();
        }

        private void Start()
        {
            _overInputText =
                World.DefaultGameObjectInjectionWorld.EntityManager.CreateEntityQuery(typeof(IsOverInputText));
        }

        private void Update()
        {
            if(_overInputText.IsEmpty)return;
            var rw = _overInputText.GetSingletonRW<IsOverInputText>();
            rw.ValueRW.IsOver = IsTextInputActive();
        }

        public bool IsTextInputActive()
        {
            if (!EventSystem.current) return false;
            var go = EventSystem.current.currentSelectedGameObject;
            if (!go) return false;

            // 原生 UI InputField
            if (go.GetComponent<UnityEngine.UI.InputField>())
                return true;

            // TextMeshPro InputField
            if (go.GetComponent<TMP_InputField>())
                return true;

            return false;
        }
    }
}