using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;


namespace SparFlame.System.Cam
{
    public class CameraAuthoring : MonoBehaviour
    {
        public Camera cam;
        public Transform startFollowTransform;
        public float normalSpeed = 0.01f;
        public float fastSpeed = 0.05f;
        public float sensitivity = 1f;
        public float edgeMarginSize = 50f;
        public KeyCode fasterMoveKey = KeyCode.LeftShift;
        public float zoomSpeed = 5f;
        public float minHeight = 5f;
        public float maxHeight = 20f;

        class Baker : Baker<CameraAuthoring>
        {
            public override void Bake(CameraAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.Dynamic);
                AddComponent(entity, new CameraControlConfig
                {
                    NormalSpeed = authoring.normalSpeed,
                    FastSpeed = authoring.fastSpeed,
                    EdgeMarginSize = authoring.edgeMarginSize,
                    Sensitivity = authoring.sensitivity,
                    FasterMoveKey = authoring.fasterMoveKey,
                    ZoomSpeed = authoring.zoomSpeed,
                    MinHeight = authoring.minHeight,
                    MaxHeight = authoring.maxHeight,
                });
                AddComponent(entity, new CameraData
                {
                    ViewMatrix = authoring.cam.worldToCameraMatrix,
                    ProjectionMatrix = authoring.cam.projectionMatrix,
                    ScreenSize = new float2(Screen.width, Screen.height)
                });
                AddComponent(entity, new CameraControlData
                {
                    FollowedEntity = authoring.startFollowTransform == null
                        ? Entity.Null
                        : GetEntity(authoring.startFollowTransform.gameObject, TransformUsageFlags.Dynamic)
                });

                AddComponent(entity, new EdgeMoveData
                {
                    State = EdgeMoveState.Nothing
                });
                AddComponent(entity, new DragMoveData
                {
                    DragStartPos = float3.zero,
                    IsDragging = false
                });
                AddComponent(entity, new MouseScrollData
                {
                    State = MouseScrollState.Nothing
                });
            }
        }
    }

    public enum EdgeMoveState
    {
        Left,
        Right,
        Up,
        Down,
        Nothing
    }

    public enum MouseScrollState
    {
        ZoomIn,
        ZoomOut,
        Nothing
    }
    
    public struct CameraControlConfig : IComponentData
    {
        public float NormalSpeed;
        public float FastSpeed;
        public float Sensitivity;
        public float EdgeMarginSize;
        public KeyCode FasterMoveKey;
        public float ZoomSpeed;
        public float MinHeight;
        public float MaxHeight;
    }

    public struct CameraData : IComponentData
    {
        public float4x4 ViewMatrix;
        public float4x4 ProjectionMatrix;
        public float2 ScreenSize;
    }

    public struct CameraControlData : IComponentData
    {
        public Entity FollowedEntity;
    }

    /// <summary>
    /// This is interface for cursor ui
    /// </summary>
    public struct EdgeMoveData : IComponentData
    {
        public EdgeMoveState State;
    }

    public struct DragMoveData : IComponentData
    {
        public float3 DragStartPos;
        public bool IsDragging;
    }

    public struct MouseScrollData : IComponentData
    {
        public MouseScrollState State;
    }
}