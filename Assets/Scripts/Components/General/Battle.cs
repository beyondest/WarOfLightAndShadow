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
    
    public struct BattleTriggerRequest : IComponentData
    {
        public Entity Attacker;
        public Entity Defender;
        public SubGameStatus TargetSubGameStatus;
    }

    public struct PlayerRetreatRequest : IComponentData
    {
    }
    
    public struct UnitRetreatTag : IComponentData{}

    public struct BattleEndRequest : IComponentData
    {
        public BattleResult Result;
    }
    
    public struct SupportFightTag : IComponentData{}

    public struct BattleCheckSightTarget : IBufferElementData
    {
        public Entity Entity;
    }
    
    public struct BattleCheckSightTriggerBelongsTo : IComponentData
    {
        public Entity Value;
    }

    public struct BattleCheckSightData : IComponentData
    {
        public Entity Value;
        public SubGameStatusData TargetSubGameStatusData;
    }
    
      
    [Serializable]
    public struct LoadingGridInfo: IBufferElementData
    {
        public float3 outerCenter;
        public float outerSize;
        public float3 innerCenter;
        public float innerSize;
    }


    public struct EcoEntityData : IBufferElementData
    {
        public EcoType EcoType;
        public Entity EcoEntity;
    }
    
    
    //----------------- Battle info recorder----------------------//

    public struct BattleRecorder : IComponentData
    {
        public float StartTime;
        public float EndTime;
        public int StartPlayerSideCityUnitCount;
        public int StartEnemySideCityUnitCount;
        
        public int PlayerSideDiedCount;
        public int EnemySideDiedCount;
        public int PlayerSideDestroyedBuildingsCount;
        public int EnemySideDestroyedBuildingsCount;
        public int PlayerUnitsUpgradeCount;

        public float KilledRewardValue;
        public float DestroyedRewardValue;
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


    public struct InvadingSupportCityTag : IComponentData
    {
    }

    public struct InvadeSupportCityConfig : IComponentData
    {
        public float ReduceHpPerHour;
    }
    
}