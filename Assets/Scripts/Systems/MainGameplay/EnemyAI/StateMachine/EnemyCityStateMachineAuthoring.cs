using System.Collections.Generic;
using SparFlame.Components.MainGameplay;
using Unity.Entities;
using UnityEngine;

namespace SparFlame.Systems.MainGameplay.EnemyAI
{
    public class EnemyCityStateMachineAuthoring : MonoBehaviour
    {
        public ArmyGroupThreatenCalculationConfig calConfig;
        public List<VeryRadicalPossibilityConfig> configPossibilities;
        
        private class EnemyCItyStateMachineAuthoringBaker : Baker<EnemyCityStateMachineAuthoring>
        {
            public override void Bake(EnemyCityStateMachineAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.None);
                AddComponent(entity, authoring.calConfig);
                var buffer = AddBuffer<VeryRadicalPossibilityConfig>(entity);
                foreach (var possibility in authoring.configPossibilities)
                {
                    buffer.Add(possibility);
                }
                
            }
        }
    }
}