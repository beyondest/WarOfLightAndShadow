using System;
using Sirenix.OdinInspector;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace SparFlame.GamePlaySystem.General
{
    public class GlobalDebugAuthoring : MonoBehaviour
    {
        [Title("General Debug Switch")] [GUIColor(1, 0.7f, 0.2f)]
        public bool globalDebugEnable;


        [FoldoutGroup("Movement Debug"), HideLabel] [ShowIf(nameof(globalDebugEnable))]
        public MovementDebug movement;

        [FoldoutGroup("Stat Debug"), HideLabel] [ShowIf(nameof(globalDebugEnable))]
        public StatDebug stat;

        [FoldoutGroup("Interact Ability Debug"), HideLabel] [ShowIf(nameof(globalDebugEnable))]
        public InteractAbilityDebug interactAbility;

        [FoldoutGroup("EnemyAIDebug"), HideLabel] [ShowIf(nameof(globalDebugEnable))]
        public EnemyAIDebug aiDebug;

        [FoldoutGroup("WaveDebug"), HideLabel] [ShowIf(nameof(globalDebugEnable))]
        public WaveDebug waveDebug;
        private class GlobalDebugAuthoringBaker : Baker<GlobalDebugAuthoring>
        {
            public override void Bake(GlobalDebugAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.None);
                if (authoring.globalDebugEnable)
                {
                    AddComponent<DebugTag>(entity);
                    if (authoring.movement.enabled)
                        AddComponent(entity, authoring.movement);
                    if (authoring.stat.enabled)
                        AddComponent(entity, authoring.stat);
                    if (authoring.interactAbility.enabled)
                        AddComponent(entity, authoring.interactAbility);
                    if (authoring.aiDebug.enabled)
                        AddComponent(entity, authoring.aiDebug);
                    if(authoring.waveDebug.enabled)
                        AddComponent(entity, authoring.waveDebug);
                }
            }
        }
    }

    public struct DebugTag : IComponentData
    {
    }


    [Serializable]
    public struct RandomSpawnDebug : IComponentData
    {
        public bool enabled;

        [ShowIf(nameof(enabled))] public float envSpawnAmountScale;
        [ShowIf(nameof(enabled))] public float resourceSpawnAmountScale;
        public bool fixPlayerFirstPawnPosition;
        [ShowIf(nameof(fixPlayerFirstPawnPosition))]
        [ShowIf(nameof(enabled))] public float3 playerFirstSpawnPosition;
    }

    [Serializable]
    public struct MovementDebug : IComponentData
    {
        public bool enabled;

        [ShowIf(nameof(enabled))] public float playerMovementScale;
        [ShowIf(nameof(enabled))] public float aiMovementScale;
    }


    [Serializable]
    public struct StatDebug : IComponentData
    {
        [InfoBox("Infinite will be override by zero settings")]
        public bool enabled;


        [Header("Player")] [ShowIf(nameof(enabled))]
        public bool playerStatGeneralInfinite;

        [ShowIf(nameof(enabled))] public bool playerStatGeneralZero;
        [ShowIf(nameof(enabled))] public bool playerCrystalStatInfinite;
        [ShowIf(nameof(enabled))] public bool playerCrystalStatZero;

        [ShowIf(nameof(enabled))] public bool playerUnitStatInfinite;
        [ShowIf(nameof(enabled))] public bool playerUnitStatZero;

        [ShowIf(nameof(enabled))] public bool playerBuildingStatInfinite;
        [ShowIf(nameof(enabled))] public bool playerBuildingStatZero;

        [Header("AI")] [ShowIf(nameof(enabled))]
        public bool aiStatGeneralInfinite;

        [ShowIf(nameof(enabled))] public bool aiStatGeneralZero;

        [ShowIf(nameof(enabled))] public bool aiCrystalStatInfinite;
        [ShowIf(nameof(enabled))] public bool aiCrystalStatZero;

        [ShowIf(nameof(enabled))] public bool aiUnitStatInfinite;
        [ShowIf(nameof(enabled))] public bool aiUnitStatZero;

        [ShowIf(nameof(enabled))] public bool aiBuildingStatInfinite;
        [ShowIf(nameof(enabled))] public bool aiBuildingStatZero;


        [Header("Neutral")] [ShowIf(nameof(enabled))]
        public bool resourceStatInfinite;

        [ShowIf(nameof(enabled))] public bool resourceStatZero;
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
    public struct EnemyAIDebug : IComponentData
    {
        public bool enabled;
        [ShowIf(nameof(enabled))] public float unitSpawnSpeedScale;
        [ShowIf(nameof(enabled))] public float buildingSpawnSpeedScale;
        [ShowIf(nameof(enabled))] public bool enableFixBuildingSpawnPos;

        [ShowIf(nameof(enabled)), ShowIf(nameof(enableFixBuildingSpawnPos))]
        public float3 buildingFixSpawnPos;
    }

    [Serializable]
    public struct WaveDebug : IComponentData
    {
        public bool enabled;
        [ShowIf(nameof(enabled))] public float waveSpeedUpScale;
        
        
    }
}