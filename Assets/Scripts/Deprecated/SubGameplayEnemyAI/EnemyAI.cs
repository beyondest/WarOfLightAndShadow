// using System;
// using Sirenix.OdinInspector;
// using SparFlame.Core.Utils;
// using Unity.Collections;
// using Unity.Entities;
// using UnityEngine;
// using Random = Unity.Mathematics.Random;
//
// namespace SparFlame.Components.SubGameplay
// {
//     public struct InTeamTag : IComponentData
//     {
//         public Entity BelongsToTeam;
//     }
//     public struct AIUnitBelongsTo : IComponentData
//     {
//         public Entity Base;
//     }
//     
//     public struct RemoveFromTeamRequest : IComponentData
//     {
//         public Entity UnitToRemove;
//         public UnitAttr UnitAttr; // For check if this is special unit
//         public Entity BelongsToTeam;
//     }
//     
//     // When enemy base is destroyed, building pack of that base level is destroyed;
//     // If all base level is destroyed, then destroy the building pack;
//     // If all building pack is destroyed, then next wave point can start
//     public struct DestroyAIBaseRequest : IComponentData
//     {
//         public Entity Base;
//     }
//     
//     public struct AIConjureShrineData : IComponentData
//     {
//         public Random Rnd;
//         public Entity Base;
//         public float ConjureTime;
//     }
//     
//     public struct AIBaseTeamAvailableData : IBufferElementData
//     {
//         public AITeamType TeamType; // gather / attack / defense / harass
//         public Entity TeamEntity; 
//         public FixedList128Bytes<MemberCountEntry> AvailableMemberCountEntries;
//     }
//     public struct WaveTeamSpecialData : IBufferElementData
//     {
//         public int WavePoint;
//         public TeamSpecialData TeamSpecialData;
//     }
//     
//     [Serializable]
//     public struct MemberCountEntry
//     {
//         [HideLabel, Tooltip("UnitType"),VerticalGroup("Type_SubType"), TableColumnWidth(120,false)]
//         public UnitType unitType;
//         /// <summary>
//         /// -1 means no filter
//         /// </summary>
//         [HideLabel, Tooltip("SubTypeIndex"),VerticalGroup("Type_SubType")]
//         public int subTypeIndex;
//         [HideLabel,Tooltip("Max count in inspector"),VerticalGroup("MaxCount"), TableColumnWidth(80,false)]
//         public int availableCount; 
//     }
//
//
//
//     [Serializable]
//     public struct TeamSpecialData
//     {
//         public AITeamType teamType;
//         public int teamsMaxCount;
//         public int unitMaxCount;
//         public UnitType specialUnitType;
//         public int specialUnitSubIndex;
//         public int specialUnitMinCount;
//         public FixedList128Bytes<MemberCountEntry> maxMemberCountEntries;
//     }
//     
//     public struct WaveUnitAssignStrategyData : IBufferElementData
//     {
//         public int WavePoint;
//         public AITeamType TeamType;
//         public int Order;
//     }
//
//     public enum AITeamType
//     {
//         Gather = 0,
//         Attack = 1,
//         Defense = 2,
//         Harass = 3
//     }
//     
//     public struct EnemyUnitSpawnIntervalData : IBufferElementData
//     {
//         public int WavePoint;
//         public UnitType UnitType;
//         public int Interval;
//     }
//
//     public struct EnemyUnitSpawnProbPrefabEntry : IBufferElementData
//     {
//         public int WavePoint;
//         public UnitType UnitType;
//         public PrefabEntryUtils.ProbabilityPrefabEntry ProbabilityPrefab;
//     }
//
//     public struct EnemyBuildingSpawnData : IBufferElementData
//     {
//         public int WavePoint;
//         public int Interval;
//         public int SpawnPackCount;
//     }
//
//     public struct EnemyBuildingPackData : IBufferElementData
//     {
//         public int WavePoint;
//         public PrefabEntryUtils.ProbabilityPrefabEntry ProbabilityPrefab;
//     }
//     
//     public struct LightEnemyDatabaseTag : IComponentData
//     {
//         
//     }
//
//     public struct DarkEnemyDatabaseTag : IComponentData
//     {
//         
//     }
// }