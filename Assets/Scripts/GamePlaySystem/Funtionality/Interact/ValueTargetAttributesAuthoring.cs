using System;
using Unity.Entities;
using UnityEngine;

namespace SparFlame.GamePlaySystem.Interact
{
    public class ValueTargetAttributesAuthoring : MonoBehaviour
    {
        private class ValueTargetAttributesAuthoringBaker : Baker<ValueTargetAttributesAuthoring>
        {
            public override void Bake(ValueTargetAttributesAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.Dynamic);
                AddBuffer<InsightTarget>(entity);
               
            }
        }
       
        
    }
    
    // public struct InsightTarget : IComparable<InsightTarget>
    // {
    //     
    // }
        
    public struct InsightTarget : IBufferElementData, IComparable<InsightTarget>,IEquatable<InsightTarget>
    {
        public Entity Target;
        public float Value;
        public int CompareTo(InsightTarget other)
        {
            return other.Value.CompareTo(Value);
        }

        public bool Equals(InsightTarget other)
        {
            return Target == other.Target;
        }
    }
}