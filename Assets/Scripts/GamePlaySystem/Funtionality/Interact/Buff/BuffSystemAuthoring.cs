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
        BuffNone = 0,

        //---------Buff-----------
        // Attack Up
        DamageBoost = 1,
        AttackSpeedUp = 2,

        // Health Up
        HealthRegeneration = 3,
        ShieldOvercharge = 4,

        // Others
        SpeedBoost = 5,

        // Buildings Up
        EnergyOverdrive = 6,
        ProjectileDeflection = 7,


        //--------Debuff---------- 

        // Vulnerable
        Vulnerable = -1,


        // Control
        AttackSpeedLow = -2,
        MovementSlow = -3,

        // Building
        EnergyDrain = -4
    }

    public struct BuffData : IBufferElementData
    {
        public BuffType Type;
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