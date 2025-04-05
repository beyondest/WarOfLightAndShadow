using SparFlame.BootStrapper;
using SparFlame.GamePlaySystem.General;
using Unity.Burst;
using Unity.Entities;

namespace SparFlame.GamePlaySystem.CustomInput.GamePlaySystem.Core.CustomInput.InputDataSystems
{
    [UpdateAfter(typeof(InputMouseSystem))]
    public partial class InputConstructSystem : SystemBase
    {
        private CustomInputActions _customInputActions;
        protected override void OnCreate()
        {
            RequireForUpdate<NotPauseTag>();
            RequireForUpdate<InputConstructData>(); 
            
        }

        protected override void OnStartRunning()
        {
            _customInputActions = InputListener.Instance.GetCustomInputActions();
        }

        protected override void OnUpdate()
        {
            var rotate = _customInputActions.Construct.Rotate.ReadValue<float>();
            SystemAPI.SetSingleton(new InputConstructData
            {
                Enabled = _customInputActions.Construct.enabled,
                Build = _customInputActions.Construct.Build.WasPerformedThisFrame(),
                Cancel = _customInputActions.Construct.Cancel.WasPerformedThisFrame(),
                FineAdjustment = _customInputActions.Construct.FineAdjustment.ReadValue<float>() > 0,
                MoveBuilding = _customInputActions.Construct.MoveBuilding.WasPerformedThisFrame(),
                Recycle = _customInputActions.Construct.Recycle.WasPerformedThisFrame(),
                Rotate =rotate,
                LeftRotate = _customInputActions.Construct.Rotate.WasPressedThisFrame() &&rotate < 0,
                RightRotate = _customInputActions.Construct.Rotate.WasPressedThisFrame() && rotate > 0,
                Snap = _customInputActions.Construct.Snap.ReadValue<float>() > 0,
                Store = _customInputActions.Construct.Store.WasPerformedThisFrame(),
                Enter = _customInputActions.ModeSwitch.SwitchBuild.WasPerformedThisFrame(),
                Exit = _customInputActions.Construct.Exit.WasPerformedThisFrame(),
            });
        }

  
    }
}