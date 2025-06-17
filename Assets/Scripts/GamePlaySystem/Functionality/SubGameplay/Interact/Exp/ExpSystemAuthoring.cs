using System;
using System.Collections.Generic;
using SparFlame.GamePlaySystem.General;
using SparFlame.GamePlaySystem.Units;
using Unity.Entities;
using UnityEngine;

namespace SparFlame.GamePlaySystem.Interact
{
    public class ExpSystemAuthoring : MonoBehaviour
    {
        public List<ExpSystemConfig> configs;
        class Baker : Baker<ExpSystemAuthoring>
        {
            public override void Bake(ExpSystemAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.None);
                var buffer = AddBuffer<ExpSystemConfig>(entity);
                foreach (var config in authoring.configs)
                {
                    buffer.Add(config);
                }
            }
        }
    }

    public enum ExpGainType
    {
        None = 0,
        Conquer = 1,
        Defend = 2,
        AttackerGainByAttack = 3,
        ShieldGainByGetDamage = 4,
        HealerGainByHeal = 5,
        HarvestGainByHarvest = 6,
        TimeGain = 7,
    }

    [Serializable]
    public struct ExpSystemConfig : IBufferElementData
    {
        public ExpGainType type;
        public int gainAmount;
    }

    public struct ExpGainRequest : IComponentData
    {
        public ExpGainType Type;
        public Entity GainEntity;
        public float Multiplier;
    }

 

 
}