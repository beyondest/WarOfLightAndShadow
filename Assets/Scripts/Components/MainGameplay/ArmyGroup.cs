using SparFlame.Components.General;
using Unity.Entities;
using Unity.Mathematics;

namespace SparFlame.Components.MainGameplay
{
    public struct ArmyGroupMovableData : IComponentData
    {
        public float Speed;
        public int CurWaypoint;
        public bool IsTargetReachable;
    }
    public struct ArmyGroupWalkableTag : IComponentData{}

    
    public struct ArmyGroupSelectionData : IComponentData
    {
                
        public int CurrentSelectCount;
        public FactionTag CurrentSelectFaction;
        public float2 SelectionBoxStartPos;
        public float2 SelectionBoxEndPos;
        public bool DragSelectStart;
        public bool IsDragSelecting;
    }
    public struct ArmyGroupSelected : IComponentData, IEnableableComponent{}

}