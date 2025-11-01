using Unity.Entities;
using Unity.Mathematics;
namespace SparFlame.Components.Input
{
    // WHYNOT using callback but use update
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


    public struct IsOverInputText : IComponentData
    {
        public bool IsOver;
    }

    /// <summary>
    /// The Only Reason to use the mouse system is to reduce the times of using raycast
    /// </summary>
    public struct InputMouseData : IComponentData
    {
        public float3 HitPosition;
        public float3 MousePosition;
        public float3 HitNormal;
        /// <summary>
        /// If no raycast hit , hitEntity will be Entity.Null
        /// </summary>
        public Entity HitEntity;
        public ClickFlag ClickFlag;
        public ClickType ClickType;
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
    }
    
    
    
    public struct InputCameraFlyData : IComponentData
    {
        public float2 LookDelta;
        public float2 Move;
        public float2 Zoom;
        public bool SpeedUp;
        public bool FlyUp;
        public bool FlyDown;
        public bool Enabled;

    }
    
    public struct InputCameraNormalData : IComponentData
    {
        public float2 Movement;
        public float2 ZoomCamera;
        public float RotateCamera;
        public bool EdgeScrolling;
        public bool DraggingCamera;
        public bool DragCameraStart;
        public bool SpeedUp;
        public bool Enabled;

    }
    public struct InputConjureData : IComponentData
    {
        public int HotKeyIndex;
        public bool Enabled;
        public bool FullConjure;
    }
    public struct InputConstructData : IComponentData
    {
        public float Rotate;
        public bool Enabled;
        public bool Build;
        public bool Cancel;
        public bool LeftRotate;
        public bool RightRotate;
        public bool Snap;
        public bool FineAdjustment;
        public bool Recycle;
        public bool Store;
        public bool MoveBuilding;
        public bool Exit;   
        public bool Enter; 
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

    public struct InputCastSkillData : IComponentData
    {
        public bool Enabled;
        public bool Cast;
        public bool Cancel;
    }
}