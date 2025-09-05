using SparFlame.Components.General;
using Unity.Entities;
using Unity.Mathematics;

namespace SparFlame.Components.SubGameplay
{
    public struct Selected : IComponentData, IEnableableComponent
    {
    }

    
    public struct UnitSelectionData : IComponentData
    {
        
        public int CurrentSelectCount;
        public FactionTag CurrentSelectFaction;
        public float2 SelectionBoxStartPos;
        public float2 SelectionBoxEndPos;
        public bool DragSelectStart;
        public bool IsDragSelecting;
    }
    
    public struct LockSelectedWorkForDrag : IComponentData, IEnableableComponent
    {
        
    }
}