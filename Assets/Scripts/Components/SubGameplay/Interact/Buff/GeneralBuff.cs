using System;
using Sirenix.OdinInspector;
using SparFlame.Components.General;
using Unity.Entities;
using Unity.Mathematics;

namespace SparFlame.Components.SubGameplay
{
    
    public enum BuffType
    {
        None = 0,
        AoeInteract = 1,
        SingleTargetNotStackable = 2,
    }

    public enum BuffName
    {
        None = 0,
        MagicSwordSplash = 1,
        MagicTowerProjectile = 3,
        MagicTowerCircle = 4,
        LightShield = 5,
        DarkShield = 6,
        LightCavalry = 7,
        DarkCavalry = 8,
        LightArcher = 9,
        DarkArcher = 10,
        ClericSkillCircle = 11,
        SpellSwordSkill = 12,
        ArrowRain = 13,
    }

    public struct BuffRequest : IComponentData
    {
        public quaternion SpawnRotation;
        public float3 SpawnPosition;
        public Entity TrackTarget;

        public BuffName Name;
        public BuffFilter Filter;
    }
    
    
    public struct GeneralBuffData : IComponentData
    {
        public Entity TrackTarget;
        public float StartTime;
        /// <summary>
        /// If this buff lifetime handled by its special system, set it to max float value
        /// </summary>
        public float Duration;
    }

    public struct BuffPrefabDataPair : IBufferElementData
    {
        public Entity Prefab;
        public BuffName Name;
        public BuffType BuffType;
        public BuffFilter Filter;
    }
    
    public struct TrackedByBuff : IBufferElementData
    {
        public Entity BuffEntity;
        public int Count;
        public int MaxStackCount;
        public BuffName Name;
    }

    [Serializable]
    public struct BuffFilter
    {
        public bool factionFilterEnabled;
        [ShowIf(nameof(factionFilterEnabled))]
        public FactionTag faction;
        public bool tierFilterEnabled;
        [ShowIf(nameof(tierFilterEnabled))]
        public Tier tier;
    }

    [Serializable]
    public struct DamageReduceShieldBuffConfig : IComponentData
    {
        public float damageReduceScale;
        public float keepDuration;
    }
    
    public struct DamageReduceShieldBuff : IComponentData
    {
        public float StopTime;
    }
    
    public struct BuildingGarrisonBuff : IComponentData, IEnableableComponent
    { 
    }

    public struct UnitGarrisonBuff : IComponentData, IEnableableComponent
    {
        
    }

    public struct SprintBuff : IComponentData,IEnableableComponent
    {
        public float LastTime;
    }
    
    [Serializable]
    public struct SprintBuffConfig : IComponentData
    {
        public float sprintCoolDown;
        public float sprintDuration;
        public float sprintSpeedBonusAmount;
    }
    
    public struct AoeTriggerRequest : IComponentData
    {
        public Entity Prefab;
    }
    
    public struct AoeTarget : IBufferElementData
    {
        public Entity Entity;
    }

}