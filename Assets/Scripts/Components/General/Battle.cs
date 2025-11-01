using System;
using SparFlame.Components.MainGameplay;
using Unity.Entities;
using Unity.Mathematics;

namespace SparFlame.Components.General
{
    public enum BattleResult
    {
        PlayerWin,
        PlayerLose,
        PlayerRetreat,
        EnemyRetreat
    }
 
    // ---------------- Battle Control Components ------------------------//
    public struct BattleTriggerRequest : IComponentData
    {
        public Entity Attacker;
        public Entity Defender;
        public SubGameStatus TargetSubGameStatus;
    }
    public struct BattleStartRequest : IComponentData
    {
        public bool Initialized;
    }
    public struct BattleEndRequest : IComponentData
    {
        public BattleResult Result;
    }
    
    // ---------------------- Battle Check Sight --------------------------//
    public struct BattleCheckSightTriggerBelongsTo : IComponentData
    {
        public Entity Value;
    }
    public struct BattleCheckSightData : IComponentData
    {
        public Entity Value;
        public SubGameStatusData TargetSubGameStatusData;
    }
    public struct BattleCheckSightTarget : IBufferElementData
    {
        public Entity Entity;
    }
    
    // --------------------- Retreat Components -------------------//
    public struct PlayerRetreatRequest : IComponentData {}
    public struct UnitRetreatTag : IComponentData{}

    

 
    
    
    //----------------- Battle info recorder----------------------//
    public struct BattleRecorder : IComponentData
    {
        public float3 CrystalPosition;
        public float StartTime;
        public float EndTime;
        public float KilledRewardValue;
        public float DestroyedRewardValue;
        
        public int StartPlayerSideCityUnitCount;
        public int StartEnemySideCityUnitCount;
        public int PlayerSideDiedCount;
        public int EnemySideDiedCount;
        public int PlayerSideDestroyedBuildingsCount;
        public int EnemySideDestroyedBuildingsCount;
        public int PlayerUnitsUpgradeCount;
    }
    
    public struct BattleRecorderPlayerSideDied : IComponentData{}
    public struct BattleRecorderEnemySideDied : IComponentData
    {
        public int DiedUnitLevel;
    }
    public struct BattleRecorderPlayerSideDestroyedBuilding : IComponentData{}
    public struct BattleRecorderEnemySideDestroyedBuilding : IComponentData
    {
        public Tier DestroyedBuildingTier;
    }
    public struct BattleRecorderPlayerUnitsUpgrade : IComponentData{}


    // ------------------- Simulation for invading support city ------------------------//
    public struct SupportFightTag : IComponentData{}
    public struct InvadingSupportCityTag : IComponentData
    {
        public float LastCheckTime;
    }
    public struct InvadeSupportCityConfig : IComponentData
    {
        public float ReduceHpRatioPerHour;
    }

    // --------------------- Battle Result ------------------------//
    public struct CrystalPrefab : IComponentData
    {
        public Entity LightCrystalPrefab;
        public Entity DarkCrystalPrefab;
    }
    public struct ChangeCityFactionRequest : IComponentData
    {
        public Entity CityEntity;
    }
}