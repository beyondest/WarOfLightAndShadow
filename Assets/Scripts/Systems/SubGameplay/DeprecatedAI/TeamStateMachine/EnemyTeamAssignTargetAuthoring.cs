using System;
using System.Linq;
using SparFlame.Components.SubGameplay;
using SparFlame.Systems.SubGameplay.EnemyAI;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

namespace SparFlame.Systems.EnemyAI
{
    
    public class EnemyTeamAssignTargetAuthoring : MonoBehaviour
    {
        public EnemyTeamAssignTargetConfigInspector config;
        public FindResourceToBaseConfigInspector findResourceToBaseConfig;
        public FindCrystalToBaseConfig findCrystalToBaseConfig;
        public FindOutSideUnitToPlayerBaseConfigInspector findOutSideUnitToPlayerBaseConfig;
        private class EnemyTeamAssignTargetAuthoringBaker : Baker<EnemyTeamAssignTargetAuthoring>
        {
            public override void Bake(EnemyTeamAssignTargetAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.None);
                if (!Mathf.Approximately(authoring.config.proPairs.Sum(pair => pair.prob), 1f))
                    throw new ArgumentException("Enemy team assign target config Probability is not 1");
                authoring.config.TargetValueTypeCount = Enum.GetValues(typeof(TargetValueType)).Length;
                var fix = new FixedList64Bytes<EnemyChooseTargetProPair>();
                foreach (var pair in authoring.config.proPairs)
                {
                    fix.Add(pair);
                }
                AddComponent(entity, new EnemyTeamAssignTargetConfig
                {
                    AttackTeamAssembleRange = authoring.config.attackTeamAssembleRange,
                    HarassRadiusToRndPlayerBase = authoring.config.harassRadiusToRndPlayerBase,
                    ProPairs = fix,
                    TargetValueTypeCount = authoring.config.TargetValueTypeCount,
                    
                });
                var fix2 = new FixedList128Bytes<ResourceTypeValue>();
                foreach (var value in authoring.findResourceToBaseConfig.resourceTypeValues)
                {
                    fix2.Add(value);
                }
                AddComponent(entity, new FindResourceToBaseConfig
                {
                    ResourceTypeValues = fix2,
                    AmountWeight = authoring.findResourceToBaseConfig.amountWeight,
                    RangeConfig = authoring.findResourceToBaseConfig.rangeConfig,
                    NegDisSqWeight = authoring.findResourceToBaseConfig.negDisSqWeight,
                    
                });
                AddComponent(entity, authoring.findCrystalToBaseConfig);

                var fix3 = new FixedList128Bytes<UnitTypeValue>();
                foreach (var typeValue in authoring.findOutSideUnitToPlayerBaseConfig.unitTypeValues)
                {
                    fix3.Add(typeValue);
                }
                AddComponent(entity, new FindOutSideUnitToPlayerBaseConfig
                {
                    rangeConfig = authoring.findOutSideUnitToPlayerBaseConfig.rangeConfig,
                    negStatWeight = authoring.findOutSideUnitToPlayerBaseConfig.negStatWeight,
                    unitTypeValues = fix3,
                    disSqWeight = authoring.findOutSideUnitToPlayerBaseConfig.posDisWeight,
                });
                AddComponent<TeamAssignData>(entity);
            }
        }
    }
    


}