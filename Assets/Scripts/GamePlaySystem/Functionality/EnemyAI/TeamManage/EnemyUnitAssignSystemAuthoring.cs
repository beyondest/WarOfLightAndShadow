using System;
using SparFlame.GamePlaySystem.Units;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

namespace SparFlame.GamePlaySystem.EnemyAI
{
    public class EnemyUnitAssignSystemAuthoring : MonoBehaviour
    {
        public float totalCountShortHandRatio = 0.5f;

        private class EnemyTeamManageSystemAuthoringBaker : Baker<EnemyUnitAssignSystemAuthoring>
        {
            public override void Bake(EnemyUnitAssignSystemAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.None);
                AddComponent(entity, new EnemyUnitAssignSystemConfig
                {
                    TotalCountShortHandRatio = authoring.totalCountShortHandRatio

                });
            }
        }
    }

    public struct EnemyUnitAssignSystemConfig : IComponentData
    {
        public float TotalCountShortHandRatio;

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