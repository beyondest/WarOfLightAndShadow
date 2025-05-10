using System;
using SparFlame.GamePlaySystem.Units;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

namespace SparFlame.GamePlaySystem.EnemyAI
{
    public class EnemyUnitAssignSystemAuthoring : MonoBehaviour
    {

        private class EnemyTeamManageSystemAuthoringBaker : Baker<EnemyUnitAssignSystemAuthoring>
        {
            public override void Bake(EnemyUnitAssignSystemAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.None);
                AddComponent(entity, new EnemyUnitAssignSystemConfig
                {

                });
            }
        }
    }

    public struct EnemyUnitAssignSystemConfig : IComponentData
    {

    }
    
    public struct WaveUnitAssignStrategyData : IBufferElementData
    {
        public int WavePoint;
        public AITeamType TeamType;
        public int Order;
    }

    public enum AITeamType
    {
        Gather = 0,
        Attack = 1,
        Defense = 2,
        Harass = 3
    }
    
  


}