using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using Unity.Transforms;
using Unity.Burst;
using SparFlame.System.Click;
namespace SparFlame.System.Cam
{
    [BurstCompile]
    public partial class CameraSystem : SystemBase
    {
        [BurstCompile]
        protected override void OnCreate()
        {
            base.OnCreate();
            RequireForUpdate<CameraControlConfig>();
            RequireForUpdate<ClickSystemData>();
            RequireForUpdate<DragMoveData>();
            RequireForUpdate<CameraControlData>();
            RequireForUpdate<CameraData>();
            RequireForUpdate<EdgeMoveData>();
        }

        [BurstCompile]
        protected override void OnUpdate()
        {
            
            var cameraEntity = SystemAPI.GetSingletonEntity<CameraData>();
            var localToWorld = SystemAPI.GetComponentRO<LocalToWorld>(cameraEntity);
            var transform = SystemAPI.GetComponentRW<LocalTransform>(cameraEntity);
            
            var cameraControlConfig = SystemAPI.GetSingleton<CameraControlConfig>();
            
            var clickSystemData = SystemAPI.GetSingleton<ClickSystemData>();
            var dragMoveData = SystemAPI.GetSingletonRW<DragMoveData>();
            var edgeMoveData = SystemAPI.GetSingletonRW<EdgeMoveData>();
            //var cameraControlData = SystemAPI.GetSingletonRW<CameraControlData>();
            
            var newPos = transform.ValueRO.Position;
            MouseDrag(ref dragMoveData.ValueRW, ref newPos, in clickSystemData, in transform.ValueRO);
            EdgeScrolling(ref edgeMoveData.ValueRW, ref newPos, in cameraControlConfig, in localToWorld.ValueRO);
            transform.ValueRW.Position = newPos;
            
            // Not consider the projection matrix change
            // This is NOT correct if fov, aspect ratio, far/near clip plane
            // or Screen Ratio is changed
            SystemAPI.SetComponent(cameraEntity, new CameraData
            {
                ViewMatrix = math.inverse(localToWorld.ValueRO.Value)
            });
        }

        [BurstCompile]
        private void MouseDrag( ref DragMoveData dragMoveData, ref float3 newPos,
            in ClickSystemData clickSystemData, in LocalTransform localTransform)
        {
            if (clickSystemData is { ClickFlag: ClickFlag.Start, ClickType: ClickType.Middle })
            {
                dragMoveData.DragStartPos = clickSystemData.HitPosition;
                dragMoveData.isDragging = true;
            }

            if (clickSystemData is { ClickFlag: ClickFlag.Clicking, ClickType: ClickType.Middle })
                newPos = localTransform.Position + dragMoveData.DragStartPos - clickSystemData.HitPosition;

            if (clickSystemData is { ClickFlag: ClickFlag.End })
                dragMoveData.isDragging = false;
        }

        [BurstCompile]
        private void EdgeScrolling( ref EdgeMoveData edgeMoveData, ref float3 newPos,
            in CameraControlConfig cameraControlConfig, in LocalToWorld localToWorld)
        {
            var speed = Input.GetKey(cameraControlConfig.FasterMoveKey) ? cameraControlConfig.FastSpeed : cameraControlConfig.NormalSpeed;
            
            if (Input.mousePosition.x > Screen.width - cameraControlConfig.EdgeMarginSize)
            {
                
                newPos += (localToWorld.Right * speed);
                edgeMoveData.State = EdgeMoveState.Right;
            }

            // Move Left
            else if (Input.mousePosition.x < cameraControlConfig.EdgeMarginSize)
            {
                newPos += (localToWorld.Right * -speed);
                edgeMoveData.State = EdgeMoveState.Left;
            }

            // Move Up
            else if (Input.mousePosition.y > Screen.height - cameraControlConfig.EdgeMarginSize)
            {
                newPos += (localToWorld.Forward * speed);
                edgeMoveData.State = EdgeMoveState.Up;
            }

            // Move Down
            else if (Input.mousePosition.y < cameraControlConfig.EdgeMarginSize)
            {
                newPos += (localToWorld.Forward * -speed);
                edgeMoveData.State = EdgeMoveState.Down;
            }
            else
            {
                edgeMoveData.State = EdgeMoveState.Nothing;
            }
        }
    }
}