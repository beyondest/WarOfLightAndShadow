using System;
using System.Collections.Generic;
using SparFlame.GamePlaySystem.General;
using SparFlame.GamePlaySystem.Interact;
using SparFlame.GamePlaySystem.Resource;
using SparFlame.GamePlaySystem.Units;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using UnityEngine.Serialization;

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


    public struct FindResourceToBaseConfig : IComponentData
    {
        public RangeConfig RangeConfig;
        public float AmountWeight;
         public float NegDisSqWeight;
        public FixedList128Bytes<ResourceTypeValue> ResourceTypeValues;
    }

    [Serializable]
    public struct ResourceTypeValue
    {
        public ResourceType resourceType;
        public float value;
    }

    [Serializable]
    public struct FindResourceToBaseConfigInspector
    {
        public RangeConfig rangeConfig;
        public float amountWeight;
        public float negDisSqWeight;
        [Tooltip("If type value is none, then it is 0 value")]
        public List<ResourceTypeValue> resourceTypeValues;
    }


    [BurstCompile]
    [WithNone(typeof(RegeneratingTag))]
    public partial struct FindResourceToBaseJob : IJobEntity
    {
        // Although disable the restriction, but this job CANNOT parallel because many resource may have same value type
        [NativeDisableParallelForRestriction] public NativeParallelMultiHashMap<int, TargetLocPair> HarvestTargets;
        [ReadOnly] public FindResourceToBaseConfig Config;
        [ReadOnly] public float3 BasePos;

        private void Execute(in ResourceAttr resourceAttr, in LocalTransform transform, Entity selfEntity)
        {
            var disSq = math.distancesq(transform.Position, BasePos);
            var resourceTypeValue = 0f;
            foreach (var typeValue in Config.ResourceTypeValues)
            {
                if (typeValue.resourceType == resourceAttr.Type)
                {
                    resourceTypeValue = typeValue.value;
                    break;
                }
            }

            var totalValue = disSq * Config.NegDisSqWeight + resourceAttr.AmountRange.upper * Config.AmountWeight +
                             resourceTypeValue;
            EnemyAIUtils.AddToMapByTotalValue(transform.Position, selfEntity, totalValue,
                Config.RangeConfig.goodRangeLower,
                Config.RangeConfig.normalRangeLower,
                HarvestTargets);
        }
    }

    [Serializable]
    public struct FindCrystalToBaseConfig : IComponentData
    {
        public RangeConfig rangeConfig;
        public float negDisSqWeight;
        public float negStatWeight;
        [Tooltip("Surround value =  attackAbility.Amount * attackAbility.Speed * attackAbility.Targets * stat.CurValue")]
        public float negSurroundingWeight;
    }

    [BurstCompile]
    [WithAll(typeof(PlayerTag))]
    [WithAll(typeof(CoreCrystalTag))]
    public partial struct FindCrystalToBaseJob : IJobEntity
    {
        // Although disable the restriction, but this job CANNOT parallel because many crystals may have same value type
        [NativeDisableParallelForRestriction] public NativeParallelMultiHashMap<int, TargetLocPair> AttackTargets;
        [ReadOnly] public ComponentLookup<AttackAbility> AttackAbilityLookUp;
        [ReadOnly] public ComponentLookup<StatData> StatDataLookUp;
        [ReadOnly] public FindCrystalToBaseConfig Config;
        [ReadOnly] public float3 BasePos;


        private void Execute(in LocalTransform transform, Entity selfEntity,
            ref SurroundingValue value, ref DynamicBuffer<SurroundingData> surroundingData)
        {
            // Calculate surrounding value
            var surroundingValue = 0f;
            for (int i = surroundingData.Length - 1; i >= 0; i--)
            {
                var surrounding = surroundingData[i];
                // Only attackable unit counts; And remove dead units
                if (!AttackAbilityLookUp.TryGetComponent(surrounding.Entity, out var attackAbility)
                    ||!StatDataLookUp.TryGetComponent(surrounding.Entity,out var stat))
                {
                    surroundingData.RemoveAt(i);
                    continue;
                }

                surroundingValue += attackAbility.Amount * attackAbility.Speed * attackAbility.Targets *
                                    stat.CurValue;
            }

            value.Value = surroundingValue;

            // Calculate total value
            var statData = StatDataLookUp[selfEntity];
            var disSq = math.distancesq(transform.Position, BasePos);
            var totalValue = disSq * Config.negDisSqWeight + statData.CurValue * Config.negStatWeight +
                             value.Value * Config.negSurroundingWeight;
            EnemyAIUtils.AddToMapByTotalValue(transform.Position, selfEntity, totalValue,
                Config.rangeConfig.goodRangeLower,
                Config.rangeConfig.normalRangeLower,
                AttackTargets);
        }
    }

    [Serializable]
    public struct FindOutSideUnitToPlayerBaseConfig : IComponentData
    {
        public RangeConfig rangeConfig;
        public float disSqWeight;
        public FixedList128Bytes<UnitTypeValue> unitTypeValues;
        public float negStatWeight;
    }

    [Serializable]
    public struct UnitTypeValue
    {
        public UnitType unitType;
        public int subTypeIndex;
        public float value;
    }

    [Serializable]
    public struct FindOutSideUnitToPlayerBaseConfigInspector
    {
        public RangeConfig rangeConfig;
        public float posDisWeight;
        public List<UnitTypeValue> unitTypeValues;
        public float negStatWeight;
    }


    [BurstCompile]
    [WithAll(typeof(PlayerTag))]
    [WithNone(typeof(UnitDeadTag))]
    public partial struct FindOutsideUnitToPlayerBaseJob : IJobEntity
    {
        // Although disable the restriction, but this job CANNOT parallel because many harassTargets may have same value type
        [NativeDisableParallelForRestriction] public NativeParallelMultiHashMap<int, TargetLocPair> HarassTargets;
        [ReadOnly] public FindOutSideUnitToPlayerBaseConfig Config;

        private void Execute(in GeneralAttr attr, in LocalTransform transform, in UnitAttr unitAttr,
            in StatData statData, in OutsideTag outsideTag, Entity selfEntity)
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

            var totalValue = outsideTag.OutSideDisSq * Config.disSqWeight + statData.CurValue * Config.negStatWeight
                                                                           + unitTypeValue;
            EnemyAIUtils.AddToMapByTotalValue(transform.Position, selfEntity, totalValue,
                Config.rangeConfig.goodRangeLower,
                Config.rangeConfig.normalRangeLower, HarassTargets);
        }
    }


    public struct DefendTarget
    {
        public int BaseIndexInQuery;
        public TargetLocPair Pair;
        public int AvailableCount;
    }
    public struct TowerAvailableCountComparer : IComparer<DefendTarget>
    {
        public int Compare(DefendTarget x, DefendTarget y)
        {
            return y.AvailableCount.CompareTo(x.AvailableCount); // 降序
        }
    }
}