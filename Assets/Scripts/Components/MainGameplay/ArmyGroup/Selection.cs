using SparFlame.Components.General;
using Unity.Entities;
using Unity.Mathematics;

namespace SparFlame.Components.MainGameplay
{
    public struct ArmyGroupSelectionData : IComponentData
    {
        public float2 SelectionBoxStartPos;
        public float2 SelectionBoxEndPos;
        public int CurrentSelectCount;
        public FactionTag CurrentSelectFaction;
        public bool DragSelectStart;
        public bool IsDragSelecting;
    }
    public struct ArmyGroupSelected : IComponentData, IEnableableComponent{}
    public struct LockArmyGroupSelectedWorkForDrag : IComponentData,IEnableableComponent {}
}