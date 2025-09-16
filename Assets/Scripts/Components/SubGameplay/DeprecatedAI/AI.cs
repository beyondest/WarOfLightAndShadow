using System;
using System.Collections.Generic;
using SparFlame.Components.General;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using Random = Unity.Mathematics.Random;
using Range = SparFlame.Core.Structs.Range;

namespace SparFlame.Components.SubGameplay
{
    
    
    [Serializable]
    public struct TeamTypeToAssembleLocRef
    {
        public AITeamType teamType;
        public GameObject locationRef;
    }

    public struct EnemyBaseAssembleLocs : IBufferElementData
    {
        public float3 TeamAssembleLocationBias;
    }


    public struct EnemyBasePosData : IComponentData
    {
        public float3 FallBackPosBias;
        public float DefenseRadius;
    }
    
    public struct AIBaseBelongsTo : IComponentData
    {
        public Entity BuildingPack;
    }
    public struct AIBaseTeamGeneralData : IBufferElementData
    {
        public AITeamType TeamType;
        public int CurCount;
    }

    public struct AIBaseGarrisonTowerData : IBufferElementData
    {
        public Entity Tower;
        public int AvailableCount;
    }

    [Serializable]
    public struct AIUnitCommandSystemConfig : IComponentData
    {
        public float aiMarchExtent;
    }
    public enum AICommandType
    {
        None = 0,
        March = 1,
        Garrison = 2,
        Attack = 3
    }


    public struct AIUnitCommandData : IComponentData
    {
        public Entity TargetEntity;
        public AICommandType CommandType;
        public float3 TargetPos;
        public bool Focus;
    }

    public struct AIUnitCommandUpdate : IComponentData, IEnableableComponent
    {
        
    }

    
    // This tag is controlled by team state machine and should always after
    public struct TeamNeedTargetTag : IComponentData, IEnableableComponent
    {
        
    }
    
    
    public struct TeamStateData : IComponentData
    {
        public Entity TargetEntity;
        public float3 TargetPosition;
        public bool Focus;
        public AICommandType CommandType;
        public bool AssignTarget;
        public bool Idle;
    }

    [Serializable]
    public struct EnemyTeamStateMachineConfig : IComponentData
    {
        public float targetBiasDis;
        public float reachTargetToleranceDisSq;
    }
    
    
    public struct EnemyTeamManageSystemConfig : IComponentData
    {
        public float TotalCountShortHandRatio;
    }
    
    
    public struct TeamWaitTag : IComponentData
    {
        
    }
    
    public struct TeamData : IComponentData
    {
        public AITeamType TeamType;
        public int SpecialUnitCount;
        public bool ShortHanded;
        public Entity BelongsToBase;
    }

    public struct EnemyTeamAssignTargetConfig : IComponentData
    {
        // Each time when attack team needs target, choose a random count from the range,
        // that count is the attack team assemble counts this time
        public Core.Structs.Range AttackTeamAssembleRange;
        public float HarassRadiusToRndPlayerBase;
        public int TargetValueTypeCount;

        // public int cutOffTargetCount;
        public FixedList64Bytes<EnemyChooseTargetProPair> ProPairs;
    }
    [Serializable]
    public struct EnemyChooseTargetProPair
    {
        public TargetValueType valueType;
        public float prob;
    }
    [Serializable]
    public struct EnemyTeamAssignTargetConfigInspector
    {
        // Each time when attack team needs target, choose a random count from the range,
        // that count is the attack team assemble counts this time
        public Range attackTeamAssembleRange;
        public float harassRadiusToRndPlayerBase;
        
        [NonSerialized]
        public int TargetValueTypeCount;

        // public int cutOffTargetCount;
        public List<EnemyChooseTargetProPair> proPairs;
        
    }

    public struct TeamAssignData : IComponentData
    {
        public Random Rnd;
        public int AttackAssembleCount;
    }


    
    public struct TeamEntityData : IBufferElementData
    {
        public Entity Unit;
    }
    
    
    public struct OutsideTag : IComponentData
    {
        public float OutSideDisSq; // The distance sq to nearest player base
    }

    public struct PlayerUnitOutsideMonitorConfig : IComponentData
    {
        public float OutSideDisThresholdSq;
    }
    
    public struct MonitorPrefabData : IComponentData
    {
        public Entity LightMonitorPrefab;
        public Entity DarkMonitorPrefab;
    }
    public struct MonitorData : IComponentData
    {
        public Entity BelongsTo;
    }

    public struct UnderMonitorTag : IComponentData
    {
        
    }

    public struct GenerateMonitorRequest : IComponentData
    {
        public Entity TargetToMonitor;
    }
    

    public struct SurroundingValue : IComponentData
    {
        public float Value;
    }
    
    public struct SurroundingData : IBufferElementData
    {
        public Entity Entity;
    }

    public struct BuildingPackSquareSize : IComponentData
    {
        public float2 Value;
    }
    
    public struct CrystalPackNeedInitTag : IComponentData
    {
        
    }
    public struct EnemySpawnSystemConfig : IComponentData
    {
    }

    public enum TargetValueType
    {
        Good = 0,
        Normal = 1,
        Bad = 2
    }

    public struct TargetLocPair
    {
        public Entity Target;
        public float3 Location;
    }

    [Serializable]
    public struct RangeConfig
    {
        public float goodRangeLower;
        public float normalRangeLower;
    }


    public struct FindResourceToBaseConfig : IComponentData
    {
        public RangeConfig RangeConfig;
        public float AmountWeight;
        public float NegDisSqWeight;
        public FixedList128Bytes<ResourceTypeValue> ResourceTypeValues;
    }

    [Serializable]
    public struct ResourceTypeValue
    {
        public ResourceType resourceType;
        public float value;
    }

    [Serializable]
    public struct FindResourceToBaseConfigInspector
    {
        public RangeConfig rangeConfig;
        public float amountWeight;
        public float negDisSqWeight;
        [Tooltip("If type value is none, then it is 0 value")]
        public List<ResourceTypeValue> resourceTypeValues;
    }

    
}