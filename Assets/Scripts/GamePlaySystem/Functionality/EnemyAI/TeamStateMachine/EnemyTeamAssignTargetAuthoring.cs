using System;
using System.Linq;
using SparFlame.Utils;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;
using Random = Unity.Mathematics.Random;

namespace SparFlame.GamePlaySystem.EnemyAI
{
    
    public class EnemyTeamAssignTargetAuthoring : MonoBehaviour
    {
        public EnemyTeamAssignTargetConfig config;
        public FindResourceToBaseConfig findResourceToBaseConfig;
        public FindCrystalToBaseConfig findCrystalToBaseConfig;
        public FindOutSideUnitToPlayerBaseConfig findOutSideUnitToPlayerBaseConfig;
        private class EnemyTeamAssignTargetAuthoringBaker : Baker<EnemyTeamAssignTargetAuthoring>
        {
            public override void Bake(EnemyTeamAssignTargetAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.None);
                if (!Mathf.Approximately(authoring.config.proPairs.Sum(pair => pair.prob), 1f))
                    throw new ArgumentException("Enemy team assign target config Probability is not 1");
                authoring.config.TargetValueTypeCount = Enum.GetValues(typeof(TargetValueType)).Length;
                AddComponent(entity, authoring.config);
                AddComponent(entity, authoring.findResourceToBaseConfig);
                AddComponent(entity, authoring.findCrystalToBaseConfig);
                AddComponent(entity, authoring.findOutSideUnitToPlayerBaseConfig);
                AddComponent<TeamAssignData>(entity);
            }
        }
    }
    

    [Serializable]
    public struct EnemyTeamAssignTargetConfig : IComponentData
    {
        // Each time when attack team needs target, choose a random count from the range,
        // that count is the attack team assemble counts this time
        public CustomDs.Range attackTeamAssembleRange;
        public float harassRadiusToWorldCenter;
        
        [NonSerialized]
        public int TargetValueTypeCount;

        // public int cutOffTargetCount;
        public FixedList64Bytes<EnemyChooseTargetProPair> proPairs;

        [Serializable]
        public struct EnemyChooseTargetProPair
        {
            public TargetValueType valueType;
            public float prob;
        }
    }

    public struct TeamAssignData : IComponentData
    {
        public Random Rnd;
        public int AttackAssembleCount;
    }
}