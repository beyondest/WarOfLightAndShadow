using System;
using SparFlame.Components.General;
using Unity.Entities;
using Unity.Mathematics;

namespace SparFlame.Components.SubGameplay
{
    
    
      public struct MovableData : IComponentData
    {
        public float MoveSpeed;
        public float3 TargetCenterPos;
        /// <summary>
        /// Target collider shape is used for calculating
        /// the extents of nav agent, extra radius for reachable check
        /// </summary>
        public float2 TargetColliderShapeXZ;
        public MovementCommandType MovementCommandType;
        public MovementState MovementState;
        public DetailInfo DetailInfo;
        /// <summary>
        /// This range is attack range for attack movement, garrison range for garrison movement...
        /// </summary>
        public float InteractRange;
        public bool ForceCalculate;
        /// <summary>
        /// This is the collider of object itself, used for raycast for obstacle avoidance 
        /// </summary>
        public float2 SelfColliderShapeXz;
    }

    public struct Surroundings : IComponentData
    {
        public bool MoveSuccess;
        public Entity FrontEntity;
        public Entity LeftEntity;
        public Entity RightEntity;
        public int CompromiseTimes;
        // public float3 IdealDirection;
        public float3 PrePos;
        public float RecordPosTime;

        /*Deprecated
         public Entity LeftTailEntity;
        public Entity RightTailEntity;
        public bool ChooseRight;
        public int SlideTimes;*/
    }
    
    [Serializable]
    public struct NavAgentComponent : IComponentData
    {
        public bool enableCalculation;
        public float3 targetPosition;
        public bool calculationComplete;
        public int currentWaypoint;
        public float nextPathCalculateTime;
        public float calculateInterval;
        public float3 extents;
        public bool forceCalculate;
        public int agentId;
    }

    [Serializable]
    public struct WaypointBuffer : IBufferElementData
    {
        public float3 position;
    }



    public struct MovingStateTag : IComponentData, IEnableableComponent
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
        /// <summary>
        /// If destroy resource, then request from faction is neutral, otherwise is ally or enemy
        /// </summary>
        public FactionTag RequestFromFaction;
        public Entity FromEntity;
    }

    public struct DoorControlRequest : IComponentData
    {
        /// <summary>
        /// Open = true, close = false
        /// </summary>
        public bool OpenOrClose;
        public FactionTag RequestFromFaction;
        public Entity FromEntity;
    }
    
    public struct UpdateNavMeshRequest : IComponentData
    {
        /// <summary>
        /// This is the id for which navmesh should be updated
        /// </summary>
        public FactionTag FactionTag;
    }

    public struct UpdateCityNavMeshRequest : IComponentData
    {
        
    }
}
