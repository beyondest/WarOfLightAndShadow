using Unity.Entities;
using UnityEngine;

namespace SparFlame.GamePlaySystem.CustomInput
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

    public struct InputArmyGroupControlData : IComponentData
    {

        public bool Enabled;
        public bool SingleSelect;
        public bool DragSelectStart;
        public bool DraggingSelect;
        public bool DragSelectEnd;
        public bool AddArmyGroup;
        public bool SetTarget;
        public bool StartMoving;
        public bool EndMovingAndClearAllTargets;
        public bool ClearAllTargets;
        public bool DeleteLastTarget;
        
            
        public bool ChangeFaction;

    }

    public struct InputArmyGroupControlConfig : IComponentData
    {
        public float MinPressedTimeForCircleShow;
        public float HoldTimeForTriggerClearAllTargets;
    }
}