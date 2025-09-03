using Unity.Entities;
using Unity.Mathematics;
namespace SparFlame.Components.Input
{
    
    public enum ClickFlag
    {
        Start,
        Clicking,
        End,
        DoubleClick,
        None
    }
    public enum ClickType
    {
        Left,
        Right,
        Middle,
        None
    }

    

    /// <summary>
    /// The Only Reason to use the mouse system is to reduce the times of using raycast
    /// </summary>
    public struct InputMouseData : IComponentData
    {
        public ClickFlag ClickFlag;
        public ClickType ClickType;
        /// <summary>
        /// if no raycast hit , hitEntity is Entity.Null
        /// </summary>
        public Entity HitEntity;
        public float3 HitPosition;
        public float3 MousePosition;
        public bool IsOverUI;
    }


    
    public struct InputArmyGroupControlData : IComponentData
    {

        public bool Enabled;
        public bool SingleSelect;
        public bool DragSelectStart;
        public bool DraggingSelect;
        public bool DragSelectEnd;
        public bool AddArmyGroup;
        public bool SetTarget;
        public bool StartMoving;
        public bool EndMovingAndClearAllTargets;
        public bool ClearAllTargets;
        public bool DeleteLastTarget;
        public bool MoveOutAllSameIconArmyGroups;


    }
    
    
    public struct InputCameraFlyData : IComponentData
    {
        public bool Enabled;
        public float2 LookDelta;
        public float2 Move;
        public bool SpeedUp;
        public bool FlyUp;
        public bool FlyDown;
        public float2 Zoom;

    }
    
    public struct InputCameraNormalData : IComponentData
    {
        public bool Enabled;
        public float2 Movement;
        public float2 ZoomCamera;
        public float RotateCamera;
        public bool EdgeScrolling;
        public bool DraggingCamera;
        public bool DragCameraStart;
        public bool SpeedUp;
    }
    public struct InputConjureData : IComponentData
    {
        public bool Enabled;
        public bool FullConjure;
        public int HotKeyIndex;
    }
    public struct InputConstructData : IComponentData
    {
        public bool Enabled;
        public bool Build;
        public bool Cancel;
        public float Rotate;
        public bool LeftRotate;
        public bool RightRotate;
        public bool Snap;
        public bool FineAdjustment;
        public bool Recycle;
        public bool Store;
        public bool MoveBuilding;
        public bool Exit;   // Exit by Button
        public bool Enter;  // Enter by Button
    }
    
    public struct InputUnitControlData : IComponentData
    {
        public bool Enabled;
        public bool SingleSelect;
        public bool DragSelectStart;
        public bool DraggingSelect;
        public bool DragSelectEnd;
        public bool AddUnit;
        public bool ChangeFaction;
        public bool Focus;
        public bool Command;
        public bool MoveOutSameIdUnits;
        public bool ClassSelection;
    }

    public struct InputGeneralShortcutData : IComponentData
    {
        public bool Wait;
        public bool CheckInfo;
        public bool CloseWindow;

    }
}