using System;
using SparFlame.Components.General;
using SparFlame.Core.Utils;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace SparFlame.Components.SubGameplay
{
    [Serializable]
    public struct ExpData : IComponentData
    {
        public Tier curTier;
        public int maxValue;
        public float curValue;
        public int curLevel; // Init is 0
    }

    public struct ExpStaticConfig : IBufferElementData
    {
        public Entity NextTierPrefab;

        public int PrefabId;
        public Tier MaxTier;

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

    public readonly partial struct UnitUpgradeAspect : IAspect
    {
        private readonly RefRO<MovableData> _movableData;
        private readonly RefRO<StatData> _statData;
        private readonly RefRO<ExpData> _expData;
        private readonly RefRO<PrefabId> _prefabId;

   
        public void SetLevelDataWhenThisIsPrefab<TAbility>(int level,
            in NativeHashMap<int, ExpStaticConfig> expDatabase,
            ref StatData statData, ref MovableData movableData, ref ExpData expData, ref TAbility prefabAbility
        ) where TAbility : IInteractAbility
        {
            var config = expDatabase[_prefabId.ValueRO.value];
            var startLevel = ((int)_expData.ValueRO.curTier - 3) * 10 + 1;
            var addLevel = math.max(0, level - startLevel);
            statData = _statData.ValueRO;
            statData.maxValue += config.StatPerLevel * addLevel;
            statData.curValue = statData.maxValue;
            // statData.bonus = 0;

            movableData = _movableData.ValueRO;
            movableData.MoveSpeed += config.MoveSpeedPerLevel * addLevel;

            expData = _expData.ValueRO;
            expData.curLevel = level;
            expData.maxValue += config.ExpGainPerLevel * addLevel;

            switch (prefabAbility.InteractType)
            {
                case InteractType.Attack:
                    prefabAbility.Amount += config.AttackAmountPerLevel;
                    prefabAbility.Range += config.AttackRangePerLevel;
                    prefabAbility.Speed += config.AttackSpeedPerLevel;
                    prefabAbility.Targets += config.AttackTargetsPerLevel;
                    break;
                case InteractType.Heal:
                    prefabAbility.Amount += config.HealAmountPerLevel;
                    prefabAbility.Range += config.HealRangePerLevel;
                    prefabAbility.Speed += config.HealSpeedPerLevel;
                    prefabAbility.Targets += config.HealTargetsPerLevel;
                    break;
                case InteractType.Harvest:
                    prefabAbility.Amount += config.HarvestAmountPerLevel;
                    prefabAbility.Range += config.HarvestRangePerLevel;
                    prefabAbility.Speed += config.HarvestSpeedPerLevel;
                    prefabAbility.Targets += config.HarvestTargetsPerLevel;
                    break;
                default:
                    BurstSafe.UnexpectedEnum(prefabAbility.InteractType);
                    break;
            }
        }
        
        /*
   public void UpGradeWhenThisIsInstance<TAbility>(
       in NativeHashMap<int, ExpStaticConfig> expDatabase,
       ref TAbility ability
   ) where TAbility : IInteractAbility
   {
       var config = expDatabase[_prefabId.ValueRO.value];
       const int addLevel = 1;
       _statData.ValueRW.maxValue += config.StatPerLevel * addLevel;
       _statData.ValueRW.curValue = _statData.ValueRW.maxValue;

       _movableData.ValueRW.MoveSpeed += config.MoveSpeedPerLevel * addLevel;

       _expData.ValueRW.curLevel++;
       _expData.ValueRW.maxValue += config.ExpGainPerLevel * addLevel;

       switch (ability.InteractType)
       {
           case InteractType.Attack:
               ability.Amount += config.AttackAmountPerLevel;
               ability.Range += config.AttackRangePerLevel;
               ability.Speed += config.AttackSpeedPerLevel;
               ability.Targets += config.AttackTargetsPerLevel;
               break;
           case InteractType.Heal:
               ability.Amount += config.HealAmountPerLevel;
               ability.Range += config.HealRangePerLevel;
               ability.Speed += config.HealSpeedPerLevel;
               ability.Targets += config.HealTargetsPerLevel;
               break;
           case InteractType.Harvest:
               ability.Amount += config.HarvestAmountPerLevel;
               ability.Range += config.HarvestRangePerLevel;
               ability.Speed += config.HarvestSpeedPerLevel;
               ability.Targets += config.HarvestTargetsPerLevel;
               break;
           default:
               BurstSafe.UnexpectedEnum(ability.InteractType);
               break;
       }
   }
   */

    }
}