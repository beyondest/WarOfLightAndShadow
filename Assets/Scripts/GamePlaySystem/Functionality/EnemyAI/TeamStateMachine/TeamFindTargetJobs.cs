using System;
using SparFlame.GamePlaySystem.General;
using SparFlame.GamePlaySystem.Interact;
using SparFlame.GamePlaySystem.Resource;
using SparFlame.GamePlaySystem.Units;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace SparFlame.GamePlaySystem.EnemyAI
{
    public enum TargetValueType
    {
        Good = 0,
        Normal = 1,
        Bad = 2
    }

    public struct TargetLocPair
    {
        public Entity Target;
        public float3 Location;
    }
    
    [Serializable]
    public struct RangeConfig
    {
        public float goodRangeLower;
        public float normalRangeLower;
    }


    [Serializable]
    public struct FindResourceToBaseConfig : IComponentData
    {
        public RangeConfig rangeConfig;
        public float amountWeight;
        public float negDisWeight;
        public FixedList128Bytes<ResourceTypeValue> resourceTypeValues;

        [Serializable]
        public struct ResourceTypeValue
        {
            public ResourceType resourceType;
            public float value;
        }
    }


    [BurstCompile]
    [WithNone(typeof(RegeneratingTag))]
    public partial struct FindResourceToBaseJob : IJobEntity
    {
        [NativeDisableParallelForRestriction] public NativeParallelMultiHashMap<int, TargetLocPair> HarvestTargets;
        [ReadOnly] public FindResourceToBaseConfig Config;
        [ReadOnly] public float3 BasePos;

        private void Execute(in ResourceAttr resourceAttr, in LocalTransform transform,Entity selfEntity)
        {
            var disSq = math.distancesq(transform.Position, BasePos);
            var resourceTypeValue = 0f;
            foreach (var typeValue in Config.resourceTypeValues)
            {
                if (typeValue.resourceType == resourceAttr.Type)
                {
                    resourceTypeValue = typeValue.value;
                    break;
                }
            }
            var totalValue = disSq * Config.negDisWeight + resourceAttr.AmountRange.upper * Config.amountWeight +
                             resourceTypeValue;
            EnemyAIUtils.AddToMapByTotalValue(transform.Position,selfEntity, totalValue, Config.rangeConfig.goodRangeLower,
                Config.rangeConfig.normalRangeLower,
                HarvestTargets);
        }
    }

    [Serializable]
    public struct FindCrystalToBaseConfig : IComponentData
    {
        public RangeConfig rangeConfig;
        public float negDisWeight;
        public float negStatWeight;
        public float negSurroundingWeight;
    }

    [BurstCompile]
    [WithAll(typeof(PlayerTag))]
    [WithAll(typeof(CoreCrystalTag))]
    public partial struct FindCrystalToBaseJob : IJobEntity
    {
        [NativeDisableParallelForRestriction] public NativeParallelMultiHashMap<int, TargetLocPair> AttackTargets;
        [ReadOnly] public ComponentLookup<AttackAbility> AttackAbilityLookUp;
        [ReadOnly] public ComponentLookup<StatData> StatDataLookUp;
        [ReadOnly] public FindCrystalToBaseConfig Config;
        [ReadOnly] public float3 BasePos;


        private void Execute(in LocalTransform transform,Entity selfEntity,
            ref SurroundingValue value, ref DynamicBuffer<SurroundingData> surroundingData)
        {
            // Calculate surrounding value
            var surroundingValue = 0f;
            for (int i =  surroundingData.Length - 1; i >= 0; i--)
            {
                var surrounding = surroundingData[i];
                // Only attackable unit counts; And remove dead units
                if (!AttackAbilityLookUp.TryGetComponent(surrounding.Entity, out var attackAbility))
                {
                    surroundingData.RemoveAt(i);
                    continue;
                }
                var stat =StatDataLookUp[surrounding.Entity];
                surroundingValue += attackAbility.Amount * attackAbility.Speed * attackAbility.Targets *
                              stat.CurValue;
            }
            value.Value = surroundingValue;
            
            // Calculate total value
            var statData = StatDataLookUp[selfEntity];
            var disSq = math.distancesq(transform.Position, BasePos);
            var totalValue = disSq * Config.negDisWeight + statData.CurValue * Config.negStatWeight + value.Value * Config.negSurroundingWeight;
            EnemyAIUtils.AddToMapByTotalValue(transform.Position,selfEntity, totalValue, Config.rangeConfig.goodRangeLower,
                Config.rangeConfig.normalRangeLower,
                AttackTargets);
        }
    }

    [Serializable]
    public struct FindOutSideUnitToPlayerBaseConfig : IComponentData
    {
        public RangeConfig rangeConfig;
         public float posDisWeight;
        public FixedList128Bytes<UnitTypeValue> unitTypeValues;
        public float negStatWeight;
        [Serializable]
        public struct UnitTypeValue
        {
            public UnitType unitType;
            public int subTypeIndex;
            public float value;
        }
    }
    
    
    [BurstCompile]
    [WithAll(typeof(PlayerTag))]
    public partial struct FindOutsideUnitToPlayerBaseJob : IJobEntity
    {
        [NativeDisableParallelForRestriction] public NativeParallelMultiHashMap<int, TargetLocPair> HarassTargets;
        [ReadOnly] public FindOutSideUnitToPlayerBaseConfig Config;
        private void Execute(in GeneralAttr attr,in LocalTransform transform,in UnitAttr unitAttr, in StatData statData, in OutsideTag outsideTag, Entity selfEntity)
        {
            
            var unitTypeValue = 0f;
            foreach (var typeValue in Config.unitTypeValues)
            {
                if (unitAttr.Type == typeValue.unitType
                    && (unitAttr.SubTypeIndex == -1 || unitAttr.SubTypeIndex == typeValue.subTypeIndex))
                {
                    unitTypeValue = typeValue.value;
                    break;
                }
            }
            var totalValue = outsideTag.OutSideDisSq * Config.posDisWeight + statData.CurValue * Config.negStatWeight
                + unitTypeValue;
            EnemyAIUtils.AddToMapByTotalValue(transform.Position, selfEntity, totalValue, Config.rangeConfig.goodRangeLower,
                Config.rangeConfig.normalRangeLower,HarassTargets);
        }
    }
}