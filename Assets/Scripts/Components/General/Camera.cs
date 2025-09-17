using System;
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

    public struct CameraViewExtendConfig : IComponentData
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
    
    
    
    public struct CameraStartPosData : IComponentData
    {
        public float3 LightInitStartPos;
        public float3 DarkInitStartPos;
        public float3 SubGameplayCameraLocalPos;
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

    // For camera roaming at the beginning of the battle, or simply set position when player enter player city
    public struct CameraRoamingPosition : IBufferElementData
    {
        public float3 Value;
        public bool IsEnemy;
    }

    [Serializable]
    public struct CameraMainGameplayHistory : IComponentData
    {
        public float3 localPosition;
        public float3 rigPosition;
        public quaternion localRotation;
        public quaternion rigRotation;
        
    }
}