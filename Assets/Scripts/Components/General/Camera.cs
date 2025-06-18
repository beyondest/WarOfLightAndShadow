using Unity.Entities;
using Unity.Mathematics;

namespace SparFlame.Components.MainGameplay
{
   
   
    public struct CameraData : IComponentData
    {
        public float4x4 ViewMatrix;
        public float4x4 ProjectionMatrix;
        public float2 ScreenSize;
        public float3 CameraRight;
        public float3 CameraForward;
        public float3 CameraUp;
        public float3 CameraRigPosition;
    }
    
    public enum EdgeMoveState
    {
        Left,
        Right,
        Up,
        Down,
        LeftDown,
        RightDown,
        LeftUp,
        RightUp,
        Nothing
    }

    public enum CameraZoomState
    {
        ZoomIn,
        ZoomOut,
        Nothing
    }

    public struct CameraViewExtend : IComponentData
    {
        public float2 Value;
    }
    
    public struct ScreenPos : IComponentData
    {
        public float2 ScreenPosition;
    }

    
    public struct InCameraExtendView : IComponentData, IEnableableComponent
    {
        
    }
    
    public struct InCameraView : IComponentData,IEnableableComponent
    {
        
    }




    /// <summary>
    /// This is interface for cursor ui
    /// </summary>
    public struct CameraMovementState : IComponentData
    {
        public EdgeMoveState EState;
        public bool IsDragging;
        public CameraZoomState ZState;
    }
    
    
    
    public struct CameraStartPos : IComponentData
    {
        public float3 Light;
        public float3 Dark;
    }

    public struct MiniMapControlData : IComponentData
    {
        public float3 CameraRigWorldPos;
        public float Angle;
        public float3 MiniMapRequestPos;
    }
    public struct CameraFollowTag : IComponentData
    {
        
    }

    public struct MainGameCameraTag : IComponentData{}
    public struct SubGameCameraTag : IComponentData {}
    public struct DraggingTag : IComponentData,IEnableableComponent
    {
        
    }
}