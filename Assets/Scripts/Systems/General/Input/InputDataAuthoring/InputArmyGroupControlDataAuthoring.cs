using SparFlame.Components.Input;
using Unity.Entities;
using UnityEngine;

namespace SparFlame.Systems.General.Input
{
    public class InputArmyGroupControlDataAuthoring : MonoBehaviour
    {
        public float minPressedTimeForCircleShow = 0.2f;
        public float holdTimeForTriggerClearAllTargets = 1f;
        private class InputArmyGroupControlDataAuthoringBaker : Baker<InputArmyGroupControlDataAuthoring>
        {
            public override void Bake(InputArmyGroupControlDataAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.None);
                AddComponent(entity, new InputArmyGroupControlData());
                AddComponent(entity, new InputArmyGroupControlConfig
                {
                    MinPressedTimeForCircleShow = authoring.minPressedTimeForCircleShow,
                    HoldTimeForTriggerClearAllTargets = authoring.holdTimeForTriggerClearAllTargets
                });
            }
        }
    }

   

    public struct InputArmyGroupControlConfig : IComponentData
    {
        public float MinPressedTimeForCircleShow;
        public float HoldTimeForTriggerClearAllTargets;
    }
}