using SparFlame.Components.Input;
using Unity.Entities;

namespace SparFlame.Systems.General.Input
{
    [UpdateAfter(typeof(InputMouseSystem))]
    public partial struct InputSkillCastSystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<InputMouseData>();
            state.RequireForUpdate<IsOverInputText>();
        }

        public void OnUpdate(ref SystemState state)
        {
            var customInputActions = InputListener.Instance.GetCustomInputActions();
            if (!customInputActions.CastSkill.enabled||SystemAPI.GetSingleton<IsOverInputText>().IsOver)
            {
                SystemAPI.SetSingleton(new InputCastSkillData());
                return;
            }

            var mouseData = SystemAPI.GetSingleton<InputMouseData>();
            SystemAPI.SetSingleton(new InputCastSkillData
            {
                Enabled = true,
                Cancel = customInputActions.CastSkill.Cancel.WasPerformedThisFrame() && ! mouseData.IsOverUI,
                Cast = customInputActions.CastSkill.Cast.WasPerformedThisFrame() && ! mouseData.IsOverUI
            });

        }

        public void OnDestroy(ref SystemState state)
        {

        }
    }
    
}