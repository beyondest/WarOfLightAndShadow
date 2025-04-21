using System;
using Unity.Entities;
using Unity.Physics;
using UnityEngine;

namespace SparFlame.GamePlaySystem.Interact
{
    public class SightAttributesAuthoring : MonoBehaviour
    {
        public GameObject sightPrefab;
        private class Baker: Baker<SightAttributesAuthoring>
        {
            public override void Bake(SightAttributesAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.Dynamic);
                AddBuffer<InsightTarget>(entity);
                AddComponent(entity, new GenerateSightRequest
                {
                    SightPrefab = GetEntity(authoring.sightPrefab, TransformUsageFlags.Dynamic),
                });
            }
        }
    }


    public struct InsightTarget : IBufferElementData, IComparable<InsightTarget>,IEquatable<InsightTarget>
    {
        public Entity Entity;
        public float PriorityValue;
        public float DisValue;
        public float StatChangValue;
        public float InteractOverride;
        public float MemoryValue;
        public float TotalValue;
        
        public int CompareTo(InsightTarget other)
        {
            return other.PriorityValue.CompareTo(PriorityValue);
        }
        public bool Equals(InsightTarget other)
        {
            return Entity == other.Entity;
        }
    }

    public struct GenerateSightRequest : IComponentData
    {
        public Entity SightPrefab;
        // public float SightRange;
        // public CollisionFilter Filter;
    }
    
}