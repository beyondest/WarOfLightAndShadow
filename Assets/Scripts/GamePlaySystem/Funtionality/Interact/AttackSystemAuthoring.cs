using Unity.Entities;
using UnityEngine;

namespace SparFlame.GamePlaySystem.Interact
{
    class AttackSystemAuthoring : MonoBehaviour
    {
        
        class AttackSystemAuthoringBaker : Baker<AttackSystemAuthoring>
        {
            public override void Bake(AttackSystemAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.Dynamic);
                AddComponent<AttackSystemConfig>(entity);
            }
        }
    }

    public struct AttackSystemConfig : IComponentData
    {
        
    }
    
    public enum InteractType
    {
        Attack,
        Heal,
        Harvest
    }
    
    public struct DamageDealtRequest : IComponentData
    {
        public Entity Attacker;
        public Entity Target;
        public float Damage;
        public InteractType InteractType;
    }
    
}