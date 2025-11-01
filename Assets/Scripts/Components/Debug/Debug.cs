using System;
using Sirenix.OdinInspector;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace SparFlame.Components.General
{
     public struct DebugTag : IComponentData
    {
    }
    [Serializable]
    public struct RandomSpawnDebug : IComponentData
    {
        [ShowIf(nameof(enabled))] public float envSpawnAmountScale;
        [ShowIf(nameof(enabled))] public float resourceSpawnAmountScale;
        [ShowIf(nameof(fixPlayerFirstPawnPosition))]
        [ShowIf(nameof(enabled))] public float3 playerFirstSpawnPosition;
        public bool fixPlayerFirstPawnPosition;
        public bool enabled;

    }

    [Serializable]
    public struct MovementDebug : IComponentData
    {
        [ShowIf(nameof(enabled))] public float playerMovementScale;
        [ShowIf(nameof(enabled))] public float aiMovementScale;
        [ShowIf(nameof(enabled))] public float playerArmyGroupMovementScale;
        [ShowIf(nameof(enabled))] public float nonPlayerArmyGroupMovementScale;
        public bool enabled;
    }

    [Serializable]
    public struct CameraDebug : IComponentData
    {
        [ShowIf(nameof(enabled))] public float3 roamingStartPos;
        [ShowIf(nameof(enabled))] public bool enterPlayerCityNoCrystalAllowed;
        public bool enabled;

    }


    [Serializable]
    public struct StatDebug : IComponentData
    {
   
        [ShowIf(nameof(enabled))] public float playerSideDamageTakenScale ;
        [ShowIf(nameof(enabled))] public float enemySideDamageTakenScale;

        [ShowIf(nameof(enabled))]
        public bool resourceStatInfinite;
        [ShowIf(nameof(enabled))] public bool resourceStatZero;
        [InfoBox("Infinite will be override by zero settings")]
        public bool enabled;

    }
    [Serializable]
    public struct InteractAbilityDebug : IComponentData
    {
        public bool enabled;

        [Header("Attack")] [ShowIf(nameof(enabled))]
        public bool playerAttackAmountInfinite;

        [ShowIf(nameof(enabled))] public bool aiAttackAmountInfinite;
        [ShowIf(nameof(enabled))] public bool playerAttackAmountZero;
        [ShowIf(nameof(enabled))] public bool aiAttackAmountZero;

        [Header("Heal")] [ShowIf(nameof(enabled))]
        public bool playerHealAmountInfinite;

        [ShowIf(nameof(enabled))] public bool aiHealAmountInfinite;
        [ShowIf(nameof(enabled))] public bool playerHealAmountZero;
        [ShowIf(nameof(enabled))] public bool aiHealAmountZero;

        [Header("Harvest")] [ShowIf(nameof(enabled))]
        public bool playerHarvestAmountInfinite;

        [ShowIf(nameof(enabled))] public bool aiHarvestAmountInfinite;
        [ShowIf(nameof(enabled))] public bool playerHarvestAmountZero;
        [ShowIf(nameof(enabled))] public bool aiHarvestAmountZero;
    }

 

    [Serializable]
    public struct ConjureDebug : IComponentData
    {
        [ShowIf(nameof(enabled))] public float conjureHoursScale;
        public bool enabled;
    }

    [Serializable]
    public struct ResourceDebug : IComponentData
    {
        [ShowIf(nameof(enabled))] public float spawnHoursScale;
        public bool enabled;
    }
    
    [Serializable]
    public struct ExpDebug : IComponentData
    {
        [ShowIf(nameof(enabled))] public float playerExpGainScale;
        [ShowIf(nameof(enabled))] public float aiExpGainScale;
        public bool enabled;
    }

    [Serializable]
    public struct EnemyAIMainGameplayDebug : IComponentData
    {
        [ShowIf(nameof(enabled))] public float armyGroupConjureTimeScale;
        [ShowIf(nameof(enabled))] public int unitCountScale;
        public bool enabled;
    }
    
    
    #region Deprecated
    [Serializable]
    public struct OldEnemyAIDebug : IComponentData
    {
        public bool enabled;
        [ShowIf(nameof(enabled)), ShowIf(nameof(enableFixBuildingSpawnPos))]
        public float3 buildingFixSpawnPos;
        [ShowIf(nameof(enabled))] public float unitSpawnSpeedScale;
        [ShowIf(nameof(enabled))] public float buildingSpawnSpeedScale;
        [ShowIf(nameof(enabled))] public bool enableFixBuildingSpawnPos;
    }

    [Serializable]
    public struct WaveDebug : IComponentData
    {
        public bool enabled;
        [ShowIf(nameof(enabled))] public float waveSpeedUpScale;
        
        
    }
    #endregion
}