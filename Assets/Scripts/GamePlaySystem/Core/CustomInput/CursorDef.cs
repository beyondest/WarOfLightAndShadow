using Unity.Entities;

namespace SparFlame.GamePlaySystem.CustomInput
{
    
    public struct SubGameplayCursorData : IComponentData
    {
        public SubGameplayCursorType LeftCursorType;
        public SubGameplayCursorType RightCursorType;
    }

    public struct MainGameplayCursorData : IComponentData
    {
        public MainGameplayCursorType Type;
    }

    public struct CircleCursorData : IComponentData
    {
        public float FillAmount;
    }


    public enum MainGameplayCursorType
    {
        None = 0,
        City = 1,
        ArmyGroup = 2,
        March = 3,
        Others = 4
    }
    
    
    public enum SubGameplayCursorType
    {
        
        // EdgeScroll Cursors
        ArrowUp,
        ArrowDown,
        ArrowLeft,
        ArrowRight,
        ArrowLeftUp,
        ArrowRightUp,
        ArrowLeftDown,
        ArrowRightDown,
        
        // GamePlay Cursors
        ControlSelect,
        CheckInfo,
        Gather,
        None,
        
        // Ally units control
        Attack,
        Heal,
        March,
        Garrison,
        Harvest,

        
        // Zoom Cursors
        ZoomIn,
        ZoomOut,
        
        // Drag Cursor
        Drag,
        // UI 
        UI,
    }

}