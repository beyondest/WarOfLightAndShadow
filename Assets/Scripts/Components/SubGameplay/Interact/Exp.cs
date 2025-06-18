using System;
using SparFlame.Components.General;
using Unity.Entities;

namespace SparFlame.Components.SubGameplay
{
 
    
    [Serializable]
    public struct ExpData : IComponentData
    {
        public Tier curTier;
        public int maxValue;
        public float curValue;
        public int curLevel;
    }

    public struct ExpStaticConfig : IBufferElementData
    {
        public int GlobalIdx;
        public Tier MaxTier;
        public Entity NextTierPrefab;
        
        public int MaxLevel;
        public int StatPerLevel;
        public int ExpGainPerLevel;
        public float MoveSpeedPerLevel;
        
        public int AttackAmountPerLevel;
        public float AttackSpeedPerLevel;
        public float AttackRangePerLevel;
        public int AttackTargetsPerLevel;

        public int HealAmountPerLevel;
        public float HealSpeedPerLevel;
        public float HealRangePerLevel;
        public int HealTargetsPerLevel;
        
        public int HarvestAmountPerLevel;
        public float HarvestSpeedPerLevel;
        public float HarvestRangePerLevel;
        public int HarvestTargetsPerLevel;
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
    public struct ExpGainRequest : IComponentData
    {
        public ExpGainType Type;
        public Entity GainEntity;
        public float Multiplier;
    }

}