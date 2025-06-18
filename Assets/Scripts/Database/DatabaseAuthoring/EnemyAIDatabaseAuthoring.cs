using System;
using System.Collections.Generic;
using SparFlame.Components.General;
using SparFlame.Components.SubGameplay;
using SparFlame.Core.Utils;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

namespace SparFlame.Database
{
    public class EnemyAIDatabaseAuthoring : MonoBehaviour
    {
        private class Baker : Baker<EnemyAIDatabaseAuthoring>
        {
            public override void Bake(EnemyAIDatabaseAuthoring authoring)
            {
                var lightEntity = GetEntity(TransformUsageFlags.None);
                AddComponent<LightEnemyDatabaseTag>(lightEntity);
                var darkEntity = CreateAdditionalEntity(TransformUsageFlags.None);
                AddComponent<DarkEnemyDatabaseTag>(darkEntity);
                var lightBuffer1 = AddBuffer<EnemyUnitSpawnIntervalData>(lightEntity);
                var lightBuffer2 = AddBuffer<EnemyUnitSpawnProbPrefabEntry>(lightEntity);
                var lightBuffer3 = AddBuffer<WaveUnitAssignStrategyData>(lightEntity);
                var lightBuffer4 = AddBuffer<WaveTeamSpecialData>(lightEntity);
                var lightBuffer5 = AddBuffer<EnemyBuildingSpawnData>(lightEntity);
                var lightBuffer6 = AddBuffer<EnemyBuildingPackData>(lightEntity);
                
                var darkBuffer1 = AddBuffer<EnemyUnitSpawnIntervalData>(darkEntity);
                var darkBuffer2 = AddBuffer<EnemyUnitSpawnProbPrefabEntry>(darkEntity);
                var darkBuffer3 = AddBuffer<WaveUnitAssignStrategyData>(darkEntity);
                var darkBuffer4 = AddBuffer<WaveTeamSpecialData>(darkEntity);
                var darkBuffer5 = AddBuffer<EnemyBuildingSpawnData>(darkEntity);
                var darkBuffer6 = AddBuffer<EnemyBuildingPackData>(darkEntity);
                
                var items = DatabaseManager.EnemyAIDatabaseSo.items;
                foreach (var item in items)
                {
                    var buffer1 = item.faction == FactionTag.Ally? lightBuffer1 : darkBuffer1;
                    var buffer2 = item.faction == FactionTag.Ally ? lightBuffer2 : darkBuffer2;
                    var buffer3 = item.faction == FactionTag.Ally ? lightBuffer3 : darkBuffer3;
                    var buffer4 = item.faction == FactionTag.Ally ? lightBuffer4 : darkBuffer4;
                    var buffer5 = item.faction == FactionTag.Ally ? lightBuffer5 : darkBuffer5;
                    var buffer6 = item.faction == FactionTag.Ally ? lightBuffer6 : darkBuffer6;
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
                                ProbabilityPrefab = new PrefabEntryUtils.ProbabilityPrefabEntry
                                {
                                    Prefab = GetEntity(entry.prefab, TransformUsageFlags.Dynamic),
                                    Probability = entry.probability,
                                    AmountRange = entry.amountRange
                                }
                            });
                        }
                    }
                    // Bake strategy data

                    if (item.assignTeamOrder.Count != Enum.GetValues(typeof(AITeamType)).Length)
                    {
                        throw new ArgumentException(
                            "AI team strategy must contain all types of teams, even if it doesn't spawn. Use team max count to control which team not spawn");
                    }

                    for (var i = 0; i < item.assignTeamOrder.Count; i++)
                    {
                        buffer3.Add(new WaveUnitAssignStrategyData
                        {
                            WavePoint = item.wavePoint,
                            TeamType = item.assignTeamOrder[i],
                            Order = i
                        });
                    }

                    var teamSpecials = new HashSet<AITeamType>();
                    // What is the consist of each type of team in current wave
                    foreach (var entry in item.teamSpecialDatas)
                    {
                        if (!teamSpecials.Add(entry.teamType))
                            throw new ArgumentException(
                                $"Duplicated AITeam type in team composition in enemy ai database wavePoint : {item.wavePoint} team type {entry.teamType}");

                        var fix = new FixedList128Bytes<MemberCountEntry>();
                        foreach (var m in entry.maxMemberCountEntries)
                        {
                            fix.Add(m);
                        }

                        buffer4.Add(new WaveTeamSpecialData
                        {
                            WavePoint = item.wavePoint,
                            TeamSpecialData = new TeamSpecialData
                            {
                                teamType = entry.teamType,
                                maxMemberCountEntries = fix,
                                specialUnitType = entry.specialUnitType,
                                specialUnitMinCount = entry.specialUnitMinCount,
                                specialUnitSubIndex = entry.specialUnitSubIndex,
                                teamsMaxCount = entry.teamsMaxCount,
                                unitMaxCount = entry.unitMaxCount,
                            }
                        });
                    }

                    if (teamSpecials.Count != Enum.GetValues(typeof(AITeamType)).Length)
                        throw new ArgumentException(
                            $"Wave point {item.wavePoint} : Enemy ai database config wrong, all team types composition must be included even" +
                            "if it does not spawn. Use team max count to control which team dont spawn");
                    
                    // Bake building spawn data
       
                    
                    buffer5.Add(new EnemyBuildingSpawnData
                    {
                        WavePoint = item.wavePoint,
                        Interval = item.buildingSpawnInterval,
                        SpawnPackCount = item.buildingPackSpawnCount,
                    });
                    foreach (var entry in item.buildingPack)
                    {
                        buffer6.Add(new EnemyBuildingPackData
                        {
                            WavePoint = item.wavePoint,
                            ProbabilityPrefab = new PrefabEntryUtils.ProbabilityPrefabEntry
                            {
                                Prefab = GetEntity(entry.prefab, TransformUsageFlags.Dynamic),
                                Probability = entry.probability,
                                AmountRange = entry.amountRange
                            }
                        });
                    }
                }
            }
        }
    }
}