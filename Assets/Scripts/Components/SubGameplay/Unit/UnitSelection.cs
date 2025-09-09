using SparFlame.Components.General;
using Unity.Collections;
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

    public struct UnitSelectionFilter : IComponentData
    {
        public bool UnitTypeFilterEnabled;
        public bool TierFilterEnabled;
        public bool LevelFilterEnabled;
        public FixedList128Bytes<int> FilteredUnitTypes;
        public Tier FilteredUnitTier;
        public int MinLevel;
        public int MaxLevel;
        
    }

    public struct UnitSelectRequest : IComponentData
    {
        public Entity Unit;
        public bool IsSelected;
    }

    public struct DeselectAllRequest : IComponentData
    {
        
    }
}