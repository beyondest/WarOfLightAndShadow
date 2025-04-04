using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;


namespace SparFlame.GamePlaySystem.CameraControl
{
    public class CameraDataAuthoring : MonoBehaviour
    {
        
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


    public struct CameraData : IComponentData
    {
        public float4x4 ViewMatrix;
        public float4x4 ProjectionMatrix;
        public float2 ScreenSize;
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