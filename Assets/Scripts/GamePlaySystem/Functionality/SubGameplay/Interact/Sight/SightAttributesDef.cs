using System;
using Unity.Entities;
using Unity.Physics;
using UnityEngine;

namespace SparFlame.GamePlaySystem.Interact
{



    public struct InsightTarget : IBufferElementData/*, IComparable<InsightTarget>,IEquatable<InsightTarget>*/
    {
        public Entity Entity;
        public float PriorityValue;
        public float DisValue;
        public float StatChangValue;
        public float InteractOverride;
        public float MemoryValue;
        public float TotalValue;
        
        // public int CompareTo(InsightTarget other)
        // {
        //     return other.PriorityValue.CompareTo(PriorityValue);
        // }
        // public bool Equals(InsightTarget other)
        // {
        //     return Entity == other.Entity;
        // }
    }

    public struct GenerateSightRequest : IComponentData
    {
        public Entity SightPrefab;
        // public float SightRange;
        // public CollisionFilter Filter;
    }
    
    // passive sight attribute
    public struct SightPriority : IComponentData
    {
        /// <summary>
        /// This value is the basic value of target, not consider its damage dealt or distance
        /// </summary>
        public float Value;

    }
    
}