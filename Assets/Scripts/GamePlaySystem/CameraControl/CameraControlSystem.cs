using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using Unity.Transforms;
using SparFlame.GamePlaySystem.Mouse;
using SparFlame.GamePlaySystem.General;

namespace SparFlame.GamePlaySystem.CameraControl
{
    [UpdateBefore(typeof(TransformSystemGroup))]
    public partial class CameraControlSystem : SystemBase
    {
        protected override void OnCreate()
        {
            base.OnCreate();
            RequireForUpdate<NotPauseTag>();
            RequireForUpdate<CameraData>();
            RequireForUpdate<CameraControlConfig>();
            RequireForUpdate<CameraControlData>();
            RequireForUpdate<MouseSystemData>();
        }


        protected override void OnUpdate()
        {
            var cam = Camera.main;
            if(cam == null)return;
            var cameraEntity = SystemAPI.GetSingletonEntity<CameraData>();
            var camData = SystemAPI.GetComponentRW<CameraData>(cameraEntity);
            var cameraControlConfig = SystemAPI.GetSingleton<CameraControlConfig>();
            var cameraControlData = SystemAPI.GetComponentRW<CameraControlData>(cameraEntity);
            var clickSystemData = SystemAPI.GetSingleton<MouseSystemData>();
            //var cameraControlData = SystemAPI.GetSingletonRW<CameraControlData>();
            
            var newPos = (float3)cam.transform.position;
            MouseDrag(ref cameraControlData.ValueRW, ref newPos, in clickSystemData,  cam.transform);
            EdgeScrolling(ref cameraControlData.ValueRW, ref newPos, in cameraControlConfig,  cam.transform);
            MouseScrollZoom(ref cameraControlData.ValueRW, ref newPos, in cameraControlConfig,  cam.transform);
            //cam.transform.position = newPos;
            cam.transform.position = newPos;
            
            camData.ValueRW.ViewMatrix = cam.worldToCameraMatrix;
            camData.ValueRW.ProjectionMatrix = cam.projectionMatrix;
            camData.ValueRW.ScreenSize = new float2(Screen.width, Screen.height);
        }


        #region CameraControlMethods


        private void MouseDrag(ref CameraControlData data, ref float3 newPos,
            in MouseSystemData mouseSystemData,  Transform transform)
        {
            if (mouseSystemData is { ClickFlag: ClickFlag.Start, ClickType: ClickType.Middle })
            {
                data.DragStartPos = mouseSystemData.HitPosition;
                data.IsDragging = true;
            }

            if (mouseSystemData is { ClickFlag: ClickFlag.Clicking, ClickType: ClickType.Middle })
                newPos = (float3)transform.position + data.DragStartPos - mouseSystemData.HitPosition;

            if (mouseSystemData is { ClickFlag: ClickFlag.End })
                data.IsDragging = false;
        }

        private void EdgeScrolling(ref CameraControlData data, ref float3 newPos,
            in CameraControlConfig cameraControlConfig, Transform transform)
        {
            var speed = Input.GetKey(cameraControlConfig.FasterMoveKey)
                ? cameraControlConfig.FastSpeed
                : cameraControlConfig.NormalSpeed;
            data.EState = EdgeMoveState.Nothing;
            
            // Move Right
            if (Input.mousePosition.x > Screen.width - cameraControlConfig.EdgeMarginSize)
            {
                newPos +=(float3) (transform.right * speed);
                data.EState = EdgeMoveState.Right;
            }

            // Move Left
            else if (Input.mousePosition.x < cameraControlConfig.EdgeMarginSize)
            {
                newPos += (float3)(transform.right * -speed);
                data.EState = EdgeMoveState.Left;
            }

            // Move Up
            if (Input.mousePosition.y > Screen.height - cameraControlConfig.EdgeMarginSize)
            {
                var forwardXZ = math.normalize(new float3(transform.forward.x, 0, transform.forward.z));    
                newPos += forwardXZ* speed;
                data.EState = data.EState switch
                {
                    EdgeMoveState.Right => EdgeMoveState.RightUp,
                    EdgeMoveState.Left => EdgeMoveState.LeftUp,
                    _ => EdgeMoveState.Up
                };
            }

            // Move Down
            else if (Input.mousePosition.y < cameraControlConfig.EdgeMarginSize)
            {
                var forwardXZ = math.normalize(new float3(transform.forward.x, 0, transform.forward.z));    
                newPos += forwardXZ * -speed;
                data.EState = data.EState switch
                {
                    EdgeMoveState.Right => EdgeMoveState.RightDown,
                    EdgeMoveState.Left => EdgeMoveState.LeftDown,
                    _ => EdgeMoveState.Down
                };
            }

        }

        private void MouseScrollZoom(ref CameraControlData data, ref float3 newPos,
            in CameraControlConfig cameraControlConfig, Transform transform)
        {
            var scroll = Input.GetAxis("Mouse ScrollWheel");
            if (scroll == 0)
            {
                data.ZState = CameraZoomState.Nothing;
                return;
            }

            data.ZState = scroll > 0 ? CameraZoomState.ZoomIn : CameraZoomState.ZoomOut;
            var newHeight = transform.position.y - scroll * cameraControlConfig.ZoomSpeed;
            newHeight = math.clamp(newHeight, cameraControlConfig.MinHeight, cameraControlConfig.MaxHeight);
            newPos.y = newHeight;
        }
        #endregion

    }
}