using Unity.Entities;

namespace SparFlame.Components.MainGameplay
{
    public struct ArmyGroupMovableData : IComponentData
    {
        public float Speed;
        public int CurWaypoint;
        public bool IsTargetReachable;
        public ArmyGroupMovementInfo MovementInfo;
    }


    public enum ArmyGroupMovementInfo
    {
        None,
        Complete,
        NotComplete
    }

   
    public struct ArmyGroupWalkableTag : IComponentData{}
}