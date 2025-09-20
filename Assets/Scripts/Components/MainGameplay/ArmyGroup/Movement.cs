using System;
using Unity.Entities;
using Unity.Mathematics;

namespace SparFlame.Components.MainGameplay
{
    
    [Serializable]
    public struct ArmyGroupMovableData : IComponentData
    {
        public float minUnitMoveSpeed;
        public int curWaypoint;
        public bool isTargetReachable;
        public ArmyGroupMovementInfo movementInfo;
    }
   
    [Serializable]
    public struct ArmyGroupMovingTarget : IBufferElementData
    {
        public float3 position;
        public float2 boxColliderSizeXz;
    }


    public enum ArmyGroupMovementInfo
    {
        None,
        Complete,
        NotComplete
    }

    public enum ArmyGroupPathCalculationInfo
    {
        None = 0,
        FailedAtQuery = 1,
        FailedAtStartingCalculation = 2,
        FailedAfterCalculation = 3,
        FailedAfterFindingStraightPath  = 4,
        Success = 5
    }
    
    public struct ArmyGroupMovingTag : IComponentData, IEnableableComponent{}

    
    // Navigation
    [Serializable]
    public struct ArmyGroupCalculatePathData : IComponentData
    {
        public int curTargetIndex;
        public float3 startPosition;
        public ArmyGroupPathCalculationInfo calculationInfo;
        public float2 boxColliderSizeXz;
    }
    public struct ArmyGroupCalculateEnable : IComponentData, IEnableableComponent{}

    [Serializable]
    public struct ArmyGroupFinalWayPoint : IBufferElementData
    {
        public float3 position;
    }
  
    [Serializable]
    public struct PathVisualizeData : IComponentData
    {
        public int preWaypoint;
    }
   
    public struct ArmyGroupWalkableTag : IComponentData{}

    public struct PathVisualizer : IComponentData
    {
    }
    public struct PathVisualizeEnabled : IComponentData, IEnableableComponent{}



    
}