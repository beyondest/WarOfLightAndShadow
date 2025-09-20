using System.Collections.Generic;
using SparFlame.Components.MainGameplay;
using Unity.Entities;
using UnityEngine;

namespace SparFlame.Systems.MainGameplay.EnemyAI
{
    public class EnemyCityStateMachineAuthoring : MonoBehaviour
    {
        public ArmyGroupThreatenCalculationConfig calConfig;
        public FormationConfig formationConfig;
        public List<VeryRadicalPossibility> configPossibilities;
        
        private class EnemyCItyStateMachineAuthoringBaker : Baker<EnemyCityStateMachineAuthoring>
        {
            public override void Bake(EnemyCityStateMachineAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.None);
                AddComponent(entity, authoring.calConfig);
                AddComponent(entity, authoring.formationConfig);
                var buffer = AddBuffer<VeryRadicalPossibility>(entity);
                foreach (var possibility in authoring.configPossibilities)
                {
                    buffer.Add(possibility);
                }
                
            }
        }
    }
}