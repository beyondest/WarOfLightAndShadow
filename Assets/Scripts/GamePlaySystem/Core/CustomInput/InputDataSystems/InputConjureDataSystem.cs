using SparFlame.GamePlaySystem.General;
using Unity.Burst;
using Unity.Entities;
using UnityEngine.InputSystem.Controls;

namespace SparFlame.GamePlaySystem.CustomInput
{
    public partial struct InputConjureDataSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<GamingTag>();
            state.RequireForUpdate<InputConjureData>();
        }

        public void OnUpdate(ref SystemState state)
        {
            var customInputActions = InputListener.Instance.GetCustomInputActions();
            var hotkeyPressed = customInputActions.Conjure.ConjureHotKey.WasPressedThisFrame();
            var hotKeyIndex = -1;
            if (hotkeyPressed)
            {
                foreach (var control in customInputActions.Conjure.ConjureHotKey.controls)
                {
                    var buttonControl = control as ButtonControl;
                    if (buttonControl!.wasPressedThisFrame)
                    {
                        int.TryParse(control.name, out hotKeyIndex);
                        break;
                    }
                }
            }
            SystemAPI.SetSingleton(new InputConjureData
            {
                Enabled = customInputActions.Conjure.enabled,
                FullConjure = customInputActions.Conjure.FullConjure.ReadValue<float>()>0,
                HotKeyIndex = hotKeyIndex
            });
        }
    }
}