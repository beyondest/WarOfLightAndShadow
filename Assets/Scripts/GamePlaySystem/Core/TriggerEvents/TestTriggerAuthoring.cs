using Unity.Entities;
using UnityEngine;

namespace SparFlame.GamePlaySystem.State
{
    public class TestTriggerAuthoring : MonoBehaviour
    {
        public bool originalEvent = false;
        public bool statefulEvent = false;
        private class DebugTriggerEventsBaker : Baker<TestTriggerAuthoring>
        {
            public override void Bake(TestTriggerAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.None);

                if (authoring.originalEvent)
                {
                    AddComponent<EnableOriginalEvent>(entity);
                }

                if (authoring.statefulEvent)
                {
                    AddComponent<EnableStateFulEvent>(entity);
                }
            }
        }
        public struct EnableOriginalEvent : IComponentData
        {
            
        }
        public struct EnableStateFulEvent : IComponentData
        {
            
        }
    }
}