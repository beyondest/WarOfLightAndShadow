using Unity.Entities;
using UnityEngine;

namespace SparFlame.GamePlaySystem.Interact
{
    public class InteractPriorityAuthoring : MonoBehaviour
    {
        public float priority;
        
        private class InteractPriorityAuthoringBaker : Baker<InteractPriorityAuthoring>
        {
            public override void Bake(InteractPriorityAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.Dynamic);
                AddComponent(entity, new InteractPriority
                {
                    Value = authoring.priority
                });
            }
        }
    }

    public struct InteractPriority : IComponentData
    {
        /// <summary>
        /// This value is the basic value of target, not consider its damage dealt or distance
        /// </summary>
        public float Value;

    }
}
