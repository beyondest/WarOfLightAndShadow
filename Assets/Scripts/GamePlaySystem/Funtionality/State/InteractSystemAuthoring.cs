using Unity.Entities;
using UnityEngine;


namespace SparFlame.GamePlaySystem.State
{
    class InteractSystemAuthoring : MonoBehaviour
    {
        
        class AttackSystemAuthoringBaker : Baker<InteractSystemAuthoring>
        {
            public override void Bake(InteractSystemAuthoring authoring)
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
        Harvest,
        Garrison
    }
    
    public struct DamageDealtRequest : IComponentData
    {
        public Entity Attacker;
        public Entity Target;
        public float Damage;
        public InteractType InteractType;
    }
    
    
    
}