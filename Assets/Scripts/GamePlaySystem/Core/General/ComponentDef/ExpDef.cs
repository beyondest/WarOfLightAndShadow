using System;
using Unity.Entities;

namespace SparFlame.GamePlaySystem.Interact
{
    public enum Tier
    {
        Tier1 = 3,
        Tier2 = 4,
        Tier3 = 5,
        // Tier4 = 6,
        // Tier5 = 7,
    }
    
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
    

}