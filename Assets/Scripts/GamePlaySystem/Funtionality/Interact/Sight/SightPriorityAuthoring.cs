﻿using Unity.Entities;
using UnityEngine;

namespace SparFlame.GamePlaySystem.Interact
{
    public class SightPriorityAuthoring : MonoBehaviour
    {
        public float priority;
        
        private class Baker : Baker<SightPriorityAuthoring>
        {
            public override void Bake(SightPriorityAuthoring authoring)
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
