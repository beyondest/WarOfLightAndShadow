using SparFlame.GamePlaySystem.General;
using SparFlame.GamePlaySystem.Units;
using Unity.Entities;
using UnityEngine;

namespace SparFlame.GamePlaySystem.EnemyAI
{
    public class EnemySpawnSystemAuthoring : MonoBehaviour
    {
        public float globalConjureScale = 1;
        private class EnemySpawnSystemAuthoringBaker : Baker<EnemySpawnSystemAuthoring>
        {
            public override void Bake(EnemySpawnSystemAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.None);
                AddComponent(entity, new EnemySpawnSystemConfig
                {
                    GlobalConjureScale = authoring.globalConjureScale,
                });
            }
        }
    }

    public struct EnemySpawnSystemConfig : IComponentData
    {
        public float GlobalConjureScale ;
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
}