using Unity.Entities;
using UnityEngine;

namespace SparFlame.GamePlaySystem.Interact
{
    public class BuffSystemAuthoring : MonoBehaviour
    {
        private class BuffSystemAuthoringBaker : Baker<BuffSystemAuthoring>
        {
            public override void Bake(BuffSystemAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.None);
                AddComponent(entity, new BuffSystemConfig
                {
                    
                });
            }
        }
    }

    public enum BuffType
    {
        None = 0,
        // Attack Up
        DamageBoost = 1,
        AttackSpeedUp = 2,
        SplashDamage = 9,

        // Health Up
        HealthRegeneration = 3,
        HealthBoost = 4,
        ShieldOvercharge = 5,
        
        // Others
        SpeedBoost = 6,
        
        // Buildings Up
        EnergyOverdrive = 7,
        EnergyBoost = 8,
        ProjectileDeflection =9
    }

    public enum DebuffType
    {
        None = 0,
        
        // Vulnerable
        ArmorReduction = 1,
        
        
        // Control
        DamageWeakness = 2,
        AttackSpeedLow = 3,
        MovementSlow = 4,
        EmpStun = 5,
        
        // Building
        EnergyDrain = 6
        
        
        
    }
    
    
    public struct BuffData : IComponentData
    {
        public float MoveSpeedMultiplier;
        public float InteractSpeedMultiplier;
        public float InteractRangeMultiplier;
        public float InteractAmountMultiplier;
        public float HealthMultiplier;
    }

    public struct BuffSystemConfig : IComponentData
    {
        
    }
}