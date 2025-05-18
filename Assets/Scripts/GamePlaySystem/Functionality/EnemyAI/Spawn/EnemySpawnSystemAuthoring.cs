using SparFlame.GamePlaySystem.General;
using SparFlame.GamePlaySystem.Units;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

namespace SparFlame.GamePlaySystem.EnemyAI
{
    public class EnemySpawnSystemAuthoring : MonoBehaviour
    {
        private class EnemySpawnSystemAuthoringBaker : Baker<EnemySpawnSystemAuthoring>
        {
            public override void Bake(EnemySpawnSystemAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.None);
                AddComponent(entity, new EnemySpawnSystemConfig
                {
                });
            }
        }
    }

    public struct EnemySpawnSystemConfig : IComponentData
    {
    }


    
    public struct EnemyUnitSpawnIntervalData : IBufferElementData
    {
        public int WavePoint;
        public UnitType UnitType;
        public int Interval;
    }

    public struct EnemyUnitSpawnProbPrefabEntry : IBufferElementData
    {
        public int WavePoint;
        public UnitType UnitType;
        public ProbabilityPrefabEntry ProbabilityPrefab;
    }

    public struct EnemyBuildingSpawnData : IBufferElementData
    {
        public int WavePoint;
        public int Interval;
        public int SpawnPackCount;
    }

    public struct EnemyBuildingPackData : IBufferElementData
    {
        public int WavePoint;
        public ProbabilityPrefabEntry ProbabilityPrefab;
    }
    
    public struct LightEnemyDatabaseTag : IComponentData
    {
        
    }

    public struct DarkEnemyDatabaseTag : IComponentData
    {
        
    }
}