using Unity.Entities;

namespace SparFlame.Components.Input
{
    public struct SubGameplayCursorData : IComponentData
    {
        public SubGameplayCursorType LeftCursorType;
        public SubGameplayCursorType RightCursorType;
    }

    public struct MainGameplayCursorData : IComponentData
    {
        public MainGameplayCursorType CursorType;
    }

    public struct CircleCursorData : IComponentData
    {
        public float FillAmount;
    }


    public enum MainGameplayCursorType
    {
        None = 0,
        March = 1,
        Garrison = 2,
        Support = 3,
        Invade = 4,
        Intercept = 5,
        CheckInfo = 6
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