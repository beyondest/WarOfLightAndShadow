using System;
using Unity.Entities;
using Unity.Mathematics;

namespace SparFlame.Components.MainGameplay
{
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
    
    // ------------------ Camera Culling Data Component --------------------------//
    public struct CameraData : IComponentData
    {
        public float4x4 ViewMatrix;
        public float4x4 ProjectionMatrix;
        public float3 CameraRight;
        public float3 CameraForward;
        public float3 CameraUp;
        public float3 CameraRigPosition;
        public float2 ScreenSize;
    }

    public struct CameraViewExtendConfig : IComponentData
    {
        public float2 Value;
    }
    
    public struct ScreenPos : IComponentData
    {
        public float2 ScreenPosition;
    }

    public struct InCameraExtendView : IComponentData, IEnableableComponent {}
    
    public struct InCameraView : IComponentData,IEnableableComponent {}

    // -------------------------- Camera Normal Control ------------------------//
    
    /// <summary>
    /// This is interface for cursor ui
    /// </summary>
    public struct CameraMovementState : IComponentData
    {
        public EdgeMoveState EState;
        public CameraZoomState ZState;
        public bool IsDragging;
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
        public float3 MiniMapRequestPos;
        public float Angle;
    }
    [Serializable]
    public struct CameraMainGameplayHistory : IComponentData
    {
        public quaternion localRotation;
        public quaternion rigRotation;
        public float3 localPosition;
        public float3 rigPosition;
    }
    public struct MainGameCameraTag : IComponentData{}
    public struct SubGameCameraTag : IComponentData {}
    public struct DraggingTag : IComponentData,IEnableableComponent{}

    // -------------------------- Camera Roaming Before Battle ----------------------------------//
    // For camera roaming at the beginning of the battle,
    // or simply set position when player enter player city
    public struct CameraRoamingPosition : IBufferElementData
    {
        public float3 Value;
        public bool IsEnemy;
    }


}