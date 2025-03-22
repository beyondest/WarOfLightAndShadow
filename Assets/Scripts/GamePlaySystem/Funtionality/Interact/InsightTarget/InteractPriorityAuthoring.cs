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
