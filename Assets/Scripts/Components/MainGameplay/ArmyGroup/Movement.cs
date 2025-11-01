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
        public ArmyGroupMovementInfo movementInfo;
        public bool isTargetReachable;
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
    
    public struct ArmyGroupMovingTag : IComponentData, IEnableableComponent{}

    // Navigation
    [Serializable]
    public struct ArmyGroupCalculatePathData : IComponentData
    {
        public float3 startPosition;
        public float2 boxColliderSizeXz;
        public int curTargetIndex;
    }
    public struct ArmyGroupCalculateEnable : IComponentData, IEnableableComponent{}

    [Serializable]
    public struct ArmyGroupFinalWayPoint : IBufferElementData
    {
        public float3 position;
    }
  
    [Serializable]
    public struct ArmyGroupPathVisualizeData : IComponentData
    {
        public int preWaypoint;
    }
   
    public struct ArmyGroupWalkableTag : IComponentData{}
    public struct PathVisualizer : IComponentData {}
    public struct ArmyGroupPathVisualizeEnabled : IComponentData, IEnableableComponent{}
}