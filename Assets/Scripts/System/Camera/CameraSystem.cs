using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using Unity.Transforms;
using SparFlame.System.Click;
using SparFlame.System.General;

namespace SparFlame.System.Cam
{
    [UpdateBefore(typeof(TransformSystemGroup))]
    public partial class CameraSystem : SystemBase
    {
        protected override void OnCreate()
        {
            base.OnCreate();
            RequireForUpdate<NotPauseTag>();
            RequireForUpdate<CameraData>();
            RequireForUpdate<CameraControlConfig>();
            RequireForUpdate<ClickSystemData>();
        }


        protected override void OnUpdate()
        {
            var cam = Camera.main;
            if(cam == null)return;
            var cameraEntity = SystemAPI.GetSingletonEntity<CameraData>();
            var camData = SystemAPI.GetComponentRW<CameraData>(cameraEntity);
            var cameraControlConfig = SystemAPI.GetSingleton<CameraControlConfig>();

            var clickSystemData = SystemAPI.GetSingleton<ClickSystemData>();
            var dragMoveData = SystemAPI.GetSingletonRW<DragMoveData>();
            var edgeMoveData = SystemAPI.GetSingletonRW<EdgeMoveData>();
            var mouseScrollData = SystemAPI.GetSingletonRW<MouseScrollData>();
            //var cameraControlData = SystemAPI.GetSingletonRW<CameraControlData>();
            
            var newPos = (float3)cam.transform.position;
            MouseDrag(ref dragMoveData.ValueRW, ref newPos, in clickSystemData,  cam.transform);
            EdgeScrolling(ref edgeMoveData.ValueRW, ref newPos, in cameraControlConfig,  cam.transform);
            MouseScrollZoom(ref mouseScrollData.ValueRW, ref newPos, in cameraControlConfig,  cam.transform);
            //cam.transform.position = newPos;
            cam.transform.position = newPos;
            
            camData.ValueRW.ViewMatrix = cam.worldToCameraMatrix;
            camData.ValueRW.ProjectionMatrix = cam.projectionMatrix;
            camData.ValueRW.ScreenSize = new float2(Screen.width, Screen.height);
        }

        private void MouseDrag(ref DragMoveData dragMoveData, ref float3 newPos,
            in ClickSystemData clickSystemData,  Transform transform)
        {
            if (clickSystemData is { ClickFlag: ClickFlag.Start, ClickType: ClickType.Middle })
            {
                dragMoveData.DragStartPos = clickSystemData.HitPosition;
                dragMoveData.IsDragging = true;
            }

            if (clickSystemData is { ClickFlag: ClickFlag.Clicking, ClickType: ClickType.Middle })
                newPos = (float3)transform.position + dragMoveData.DragStartPos - clickSystemData.HitPosition;

            if (clickSystemData is { ClickFlag: ClickFlag.End })
                dragMoveData.IsDragging = false;
        }

        private void EdgeScrolling(ref EdgeMoveData edgeMoveData, ref float3 newPos,
            in CameraControlConfig cameraControlConfig, Transform transform)
        {
            var speed = Input.GetKey(cameraControlConfig.FasterMoveKey)
                ? cameraControlConfig.FastSpeed
                : cameraControlConfig.NormalSpeed;

            if (Input.mousePosition.x > Screen.width - cameraControlConfig.EdgeMarginSize)
            {
                newPos +=(float3) (transform.right * speed);
                edgeMoveData.State = EdgeMoveState.Right;
            }

            // Move Left
            else if (Input.mousePosition.x < cameraControlConfig.EdgeMarginSize)
            {
                newPos += (float3)(transform.right * -speed);
                edgeMoveData.State = EdgeMoveState.Left;
            }

            // Move Up
            else if (Input.mousePosition.y > Screen.height - cameraControlConfig.EdgeMarginSize)
            {
                var forwardXZ = math.normalize(new float3(transform.forward.x, 0, transform.forward.z));    
                newPos += forwardXZ* speed;
                edgeMoveData.State = EdgeMoveState.Up;
            }

            // Move Down
            else if (Input.mousePosition.y < cameraControlConfig.EdgeMarginSize)
            {
                var forwardXZ = math.normalize(new float3(transform.forward.x, 0, transform.forward.z));    
                newPos += forwardXZ * -speed;
                edgeMoveData.State = EdgeMoveState.Down;
            }
            else
            {
                edgeMoveData.State = EdgeMoveState.Nothing;
            }
        }

        private void MouseScrollZoom(ref MouseScrollData mouseScrollData, ref float3 newPos,
            in CameraControlConfig cameraControlConfig, Transform transform)
        {
            var scroll = Input.GetAxis("Mouse ScrollWheel");
            if (scroll == 0)
            {
                mouseScrollData.State = MouseScrollState.Nothing;
                return;
            }

            mouseScrollData.State = scroll > 0 ? MouseScrollState.ZoomIn : MouseScrollState.ZoomOut;
            var newHeight = transform.position.y - scroll * cameraControlConfig.ZoomSpeed;
            newHeight = math.clamp(newHeight, cameraControlConfig.MinHeight, cameraControlConfig.MaxHeight);
            newPos.y = newHeight;
        }
    }
}