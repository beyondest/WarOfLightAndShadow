using System;
using System.Runtime.InteropServices;
using SparFlame.Components.General;
using SparFlame.Components.MainGameplay;
using Unity.Entities;
using Unity.Mathematics;

namespace SparFlame.Components.SubGameplay
{
    
      public struct MovableData : IComponentData
    {
        public float3 TargetColliderShape;
        public float3 TargetCenterPos;
        public float MoveSpeed;
        /// <summary>
        /// This range is attack range for attack movement, garrison range for garrison movement...
        /// </summary>
        public float InteractRange;
        /// <summary>
        /// Target collider shape is used for calculating
        /// the extents of nav agent, extra radius for reachable check
        /// </summary>
        public MovementCommandType MovementCommandType;
        public MovementState MovementState;
        public DetailInfo DetailInfo;
        public bool ForceCalculate;
    }

    public struct Surroundings : IComponentData
    {
        public float3 PrePos;
        public Entity FrontEntity;
        public Entity LeftEntity;
        public Entity RightEntity;
        public float RecordPosTime;
        public int CompromiseTimes;
        public bool MoveSuccess;
    }

    public struct SeekTarget : IComponentData
    {
        public float3 Direction;
    }
    public struct Separation : IComponentData
    {
       public float3 Value;
    }
    public struct GroundInfo : IComponentData
    {
        public float3 Normal;
        public float3 HitPosition;
        public bool Hit;
    }
    public struct Velocity : IComponentData
    {
        public float3 Value; 
    }

    public struct Avoidance : IComponentData
    {
        public float3 Value;
    }

    public struct Alignment : IComponentData
    {
        public float3 Value;
    }
    
    [Serializable]
    public struct NavAgentComponent : IComponentData
    {
        public float3 targetPosition;
        public float3 extents;
        public float nextPathCalculateTime;
        public float calculateInterval;
        public int currentWaypoint;
        public int agentId;
                public NavAgentCalculateInfo calculationInfo;
                public bool enableCalculation;
                public bool calculationComplete;
        public bool forceCalculate;
                
    }
    public enum NavAgentCalculateInfo
    {
        None = 0,
        FailedAtQuery = 1,
        FailedAtStartingCalculation = 2,
        FailedAfterCalculation = 3,
        FailedAfterFindingStraightPath  = 4,
        NoWaypointsAfterCalculation = 5,
        Success = 6,
        
    }
    [Serializable]
    public struct WaypointBuffer : IBufferElementData
    {
        public float3 position;
    }


    [WriteGroup(typeof(SeekTarget))]
    public struct MovingStateTag : IComponentData, IEnableableComponent
    {
    }

    public struct AutoGiveWayTag : IComponentData, IEnableableComponent
    {
        
    }
    
    
    public enum MovementCommandType
    {
        None,
        /// <summary>
        /// Interactive includes attack, heal, garrison, harvest
        /// </summary>
        Interactive,
        March,
    }

    public enum MovementState
    {
        NotMoving ,
        /// <summary>
        /// Is moving 
        /// </summary>
        IsMoving ,
        /// <summary>
        /// Target reachable and reach
        /// </summary>
        MovementComplete ,
        /// <summary>
        /// Only reach the closest point , cause target not reachable
        /// </summary>
        MovementPartialComplete,
    }

    public enum DetailInfo
    {
        None,
        Reachable,
        NotReachable,
        /// <summary>
        /// Calculation not complete or fail will return this
        /// </summary>
        CalculationNotComplete,
        AutoGiveWay,
        Stuck
    }

    
    
    
    public struct BuildingSyncVolumeRequest : IComponentData
    {
        public Entity FromEntity;
    }


    public struct VolumeObstacleTag : IComponentData
    {
    }

    public struct VolumeObstacleSpawnRequest : IComponentData, IEnableableComponent
    {
        public float3 Center;
        public float3 Size;

        public float VolumeRadius;
        public AreaType VolumeAreaType;

        /// <summary>
        /// Will instantiate allyObstacle and enemy volume if from ally, vice versa;
        /// If this is neutral faction, then will generate obstacle for all faction nav mesh map,
        /// and neutral obstacle is not dynamic
        /// </summary>
        public FactionTag RequestFromFaction;

    }


    public enum AreaType
    {
        Walkable = 0,
        NotWalkable = 1,
        Jump = 2,
        
        // Not attackable cost
        Cost00 = 3,
        Cost01 = 4,
        Cost02 = 5,
        Cost03 = 6,
        Cost04 = 7,
        
        // Attackable cost
        Cost10 = 13,
        Cost11 = 14,
        Cost12 = 15,
        Cost13 = 16,
        Cost14 = 17,
        Cost15 = 18,
    }
    public struct VolumeObstacleDestroyRequest : IComponentData
    {
        public Entity FromEntity;
        /// <summary>
        /// If destroy resource, then request from faction is neutral, otherwise is ally or enemy
        /// </summary>
        public FactionTag RequestFromFaction;
    }

    public struct DoorControlRequest : IComponentData
    {
        public Entity FromEntity;
        public FactionTag RequestFromFaction;
                /// <summary>
                /// Open = true, close = false
                /// </summary>
                public bool OpenOrClose;
    }
    
    public struct UpdateNavMeshRequest : IComponentData
    {
        /// <summary>
        /// This is the id for which navmesh should be updated
        /// </summary>
        public FactionTag FactionTag;
    }

    public struct FakeCollisionTriggerRequest : IComponentData
    {
        public Entity TriggerPrefab;
    }
    public struct FakeColliderTarget : IBufferElementData
    {
        public Entity Entity;
    }
    
    
    public enum AutoGiveWayState
    {
        None,
        GoTo,
        GoBack,
    }


    public struct AutoGiveWayData : IComponentData
    {
        public float3 OriPosition;
        public float AccumulatedTime;
        public AutoGiveWayState State;
    }

    public struct GridColliderTarget : IBufferElementData
    {
        public Entity Entity;
    }
}
