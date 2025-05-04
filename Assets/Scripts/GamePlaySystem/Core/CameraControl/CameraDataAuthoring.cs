using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;


namespace SparFlame.GamePlaySystem.CameraControl
{
    public class CameraDataAuthoring : MonoBehaviour
    {

        [Tooltip("This value is crucial important for both performance and" +
                "gameplay. This extend determines which entity is going to be calculated in many systems," +
                "The larger the extend, the cost is more, the gameplay is better")]
        public float2 cameraViewExtend = new(30, 30);
        
        class Baker : Baker<CameraDataAuthoring>
        {
            public override void Bake(CameraDataAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.Dynamic);

                AddComponent(entity, new CameraData
                {
                    ScreenSize = new float2(Screen.width, Screen.height)
                });
     
                AddComponent(entity, new CameraMovementState
                {
                    EState = EdgeMoveState.Nothing,
                    IsDragging = false,
                    ZState = CameraZoomState.Nothing,
                });
                AddComponent(entity,new CameraViewExtend
                {
                    Value = authoring.cameraViewExtend
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

    public struct CameraData : IComponentData
    {
        public float4x4 ViewMatrix;
        public float4x4 ProjectionMatrix;
        public float2 ScreenSize;
        public float3 CameraRight;
        public float3 CameraForward;
        public float3 CameraUp;
        public float3 CameraPosition;
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

}