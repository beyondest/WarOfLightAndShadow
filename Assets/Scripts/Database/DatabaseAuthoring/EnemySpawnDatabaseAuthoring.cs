using SparFlame.GamePlaySystem.EnemyAI;
using SparFlame.GamePlaySystem.General;
using Unity.Entities;
using UnityEngine;

namespace SparFlame.Database
{
    public class EnemySpawnDatabaseAuthoring : MonoBehaviour
    {
        private class EnemySpawnDatabaseAuthoringBaker : Baker<EnemySpawnDatabaseAuthoring>
        {
            public override void Bake(EnemySpawnDatabaseAuthoring authoring)
            {
                var basicSpawnDataEntity = GetEntity(TransformUsageFlags.None);
                var buffer1 = AddBuffer<EnemyUnitSpawnIntervalData>(basicSpawnDataEntity);
                var buffer2 = AddBuffer<EnemyUnitSpawnProbPrefabEntry>(basicSpawnDataEntity);
                var strategyEntity = CreateAdditionalEntity(TransformUsageFlags.None);
                var buffer3 = AddBuffer<WaveUnitAssignStrategyData>(strategyEntity);
                var buffer5 = AddBuffer<WaveTeamSpecialData>(strategyEntity);

                var items = DatabaseManager.EnemyAIDatabaseSo.items;
                foreach (var item in items)
                {
                    // Bake Unit Spawn Data
                    foreach (var unitTypeSpawnData in item.unitTypeSpawnDatas)
                    {
                        buffer1.Add(new EnemyUnitSpawnIntervalData
                        {
                            UnitType = unitTypeSpawnData.type,
                            Interval = unitTypeSpawnData.interval,
                            WavePoint = item.wavePoint
                        });
                        foreach (var entry in unitTypeSpawnData.entries)
                        {
                            buffer2.Add(new EnemyUnitSpawnProbPrefabEntry
                            {
                                WavePoint = item.wavePoint,
                                UnitType = unitTypeSpawnData.type,
                                ProbabilityPrefab = new ProbabilityPrefabEntry
                                {
                                    Prefab = GetEntity(entry.prefab, TransformUsageFlags.Dynamic),
                                    Probability = entry.probability,
                                    AmountRange = entry.amountRange
                                }
                            });
                        }
                    }

                    // Bake strategy data

                    // Which team is considered first
                    for (var i = 0; i < item.assignTeamOrder.Count; i++)
                    {
                        buffer3.Add(new WaveUnitAssignStrategyData
                        {
                            WavePoint = item.wavePoint,
                            TeamType = item.assignTeamOrder[i],
                            Order = i
                        });
                    }


                    // What is the consist of each type of team in current wave
                    foreach (var entry in item.teamSpecialDatas)
                    {
                        buffer5.Add(new WaveTeamSpecialData
                        {
                            WavePoint = item.wavePoint,
                            TeamSpecialData = entry
                        });
                    }
                    // TODO : Back Building Pack Data
                }
            }
        }
    }
}