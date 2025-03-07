using Unity.Entities;
using Unity.Burst;
using Unity.Mathematics;
using SparFlame.GamePlaySystem.General;
using SparFlame.GamePlaySystem.UnitSelection;
using SparFlame.GamePlaySystem.Building;
using SparFlame.GamePlaySystem.Resource;
using SparFlame.GamePlaySystem.CameraControl;
using SparFlame.GamePlaySystem.Mouse;

namespace SparFlame.UI.Cursor
{
    public partial struct CursorManageSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<NotPauseTag>();
            state.RequireForUpdate<CursorManageData>();
            state.RequireForUpdate<MouseSystemData>();
            state.RequireForUpdate<UnitSelectionData>();
            state.RequireForUpdate<CameraControlData>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var mouseSystemData = SystemAPI.GetSingleton<MouseSystemData>();
            var cursorManageData = SystemAPI.GetSingletonRW<CursorManageData>();
            var unitSelectionData = SystemAPI.GetSingleton<UnitSelectionData>();
            var cameraControlData = SystemAPI.GetSingleton<CameraControlData>();

            cursorManageData.ValueRW.LeftCursorType = CursorType.UI;
            cursorManageData.ValueRW.RightCursorType = CursorType.None;

            // Check is dragging
            if (IsDraggingCamera(ref cursorManageData, in cameraControlData)) return;

            // Check is edge scrolling
            if (IsEdgeScrolling(ref cursorManageData, in cameraControlData)) return;

            // Check is zooming
            if (IsZoomingCamera(ref cursorManageData, in cameraControlData)) return;

            // Not Clickable
            if (mouseSystemData.HitEntity == Entity.Null)
                return;

            // Clickable 
            var basicAttr = SystemAPI.GetComponent<BasicAttributes>(mouseSystemData.HitEntity);
            BuildingAttributes buildingAttr = new BuildingAttributes
            {
                State = BuildingState.Idle
            };
            ResourceAttributes resourceAttr = new ResourceAttributes
            {
                State = ResourceState.Depleted
            };
            switch (basicAttr.BaseTag)
            {
                case BaseTag.Buildings:
                    buildingAttr = SystemAPI.GetComponent<BuildingAttributes>(mouseSystemData.HitEntity);
                    break;
                case BaseTag.Resources:
                    resourceAttr = SystemAPI.GetComponent<ResourceAttributes>(mouseSystemData.HitEntity);
                    break;
            }

            CheckMouseHovering(ref cursorManageData, in unitSelectionData, in basicAttr, in buildingAttr,
                in resourceAttr);
        }


        #region CursorSwitchLogic

        private static void CheckMouseHovering(ref RefRW<CursorManageData> cursorManageData,
            in UnitSelectionData unitSelectionData,
            in BasicAttributes basicAttr, in BuildingAttributes buildingAttr, in ResourceAttributes resourceAttr)
        {
            // None unit selected
            if (unitSelectionData.CurrentSelectCount == 0)
            {
                (cursorManageData.ValueRW.LeftCursorType, cursorManageData.ValueRW.RightCursorType) =
                    (basicAttr.TeamTag, basicAttr.BaseTag) switch
                    {
                        (TeamTag.Neutral, BaseTag.Resources) => (CursorType.CheckInfo, CursorType.None),
                        (TeamTag.Ally, BaseTag.Units) => (CursorType.ControlSelect, CursorType.None),
                        (TeamTag.Ally, BaseTag.Buildings) when buildingAttr.State == BuildingState.Produced => (
                            CursorType.Gather, CursorType.None),
                        (TeamTag.Ally, BaseTag.Buildings) when buildingAttr.State != BuildingState.Produced => (
                            CursorType.ControlSelect, CursorType.None),
                        (TeamTag.Enemy, _) => (CursorType.CheckInfo, CursorType.None),
                        (_, _) => (CursorType.UI, CursorType.None),
                    };
            }
            // Ally unit selected
            else
            {
                (cursorManageData.ValueRW.LeftCursorType, cursorManageData.ValueRW.RightCursorType) =
                    (basicAttr.TeamTag, basicAttr.BaseTag) switch
                    {
                        (TeamTag.Neutral, BaseTag.Resources) when resourceAttr.State == ResourceState.Available => (
                            CursorType.CheckInfo, CursorType.Harvest),
                        (TeamTag.Neutral, BaseTag.Env) => (CursorType.None, CursorType.March),
                        (TeamTag.Ally, BaseTag.Units) => (CursorType.ControlSelect, CursorType.Heal),
                        (TeamTag.Ally, BaseTag.Buildings) when buildingAttr.State == BuildingState.Produced => (
                            CursorType.Gather, CursorType.Garrison),
                        (TeamTag.Ally, BaseTag.Buildings) when buildingAttr.State != BuildingState.Produced => (
                            CursorType.ControlSelect, CursorType.Garrison),
                        (TeamTag.Enemy, _) => (CursorType.CheckInfo, CursorType.Attack),
                        (_, _) => (CursorType.UI, CursorType.None),
                    };
            }
        }

        private static bool IsZoomingCamera(ref RefRW<CursorManageData> cursorManageData,
            in CameraControlData cameraControlData)
        {
            cursorManageData.ValueRW.LeftCursorType = cameraControlData.ZState switch
            {
                CameraZoomState.ZoomIn => CursorType.ZoomIn,
                CameraZoomState.ZoomOut => CursorType.ZoomOut,
                _ => CursorType.UI
            };
            return cursorManageData.ValueRW.LeftCursorType != CursorType.UI;
        }

        private static bool IsEdgeScrolling(ref RefRW<CursorManageData> cursorManageData,
            in CameraControlData cameraControlData)
        {
            cursorManageData.ValueRW.LeftCursorType =
                cameraControlData.EState switch
                {
                    EdgeMoveState.Down => CursorType.ArrowDown,
                    EdgeMoveState.Up => CursorType.ArrowUp,
                    EdgeMoveState.Left => CursorType.ArrowLeft,
                    EdgeMoveState.Right => CursorType.ArrowRight,
                    EdgeMoveState.LeftDown => CursorType.ArrowLeftDown,
                    EdgeMoveState.RightDown => CursorType.ArrowRightDown,
                    EdgeMoveState.LeftUp => CursorType.ArrowLeftUp,
                    EdgeMoveState.RightUp => CursorType.ArrowRightUp,
                    _ => CursorType.UI,
                };
            return cursorManageData.ValueRW.LeftCursorType != CursorType.UI;
        }

        private static bool IsDraggingCamera(ref RefRW<CursorManageData> cursorManageData,
            in CameraControlData cameraControlData)
        {
            if (!cameraControlData.IsDragging) return false;
            cursorManageData.ValueRW.LeftCursorType = CursorType.Drag;
            return true;
        }

        #endregion
    }
}