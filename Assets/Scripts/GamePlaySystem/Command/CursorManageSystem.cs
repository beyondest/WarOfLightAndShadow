using System.Runtime.CompilerServices;
using Unity.Entities;
using Unity.Burst;
using SparFlame.GamePlaySystem.General;
using SparFlame.GamePlaySystem.UnitSelection;
using SparFlame.GamePlaySystem.Building;
using SparFlame.GamePlaySystem.Resource;
using SparFlame.GamePlaySystem.CameraControl;
using SparFlame.GamePlaySystem.Mouse;

namespace SparFlame.GamePlaySystem.Command
{
    public partial struct CursorManageSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<NotPauseTag>();
            state.RequireForUpdate<CursorData>();
            state.RequireForUpdate<MouseSystemData>();
            state.RequireForUpdate<UnitSelectionData>();
            state.RequireForUpdate<CameraControlData>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var mouseSystemData = SystemAPI.GetSingleton<MouseSystemData>();
            var cursorManageData = SystemAPI.GetSingletonRW<CursorData>();
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
            var basicAttr = SystemAPI.GetComponent<BasicAttr>(mouseSystemData.HitEntity);
            BuildingAttr buildingAttr = new BuildingAttr
            {
                State = BuildingState.Idle
            };
            ResourceAttr resourceAttr = new ResourceAttr
            {
                State = ResourceState.Depleted
            };
            switch (basicAttr.BaseTag)
            {
                case BaseTag.Buildings:
                    buildingAttr = SystemAPI.GetComponent<BuildingAttr>(mouseSystemData.HitEntity);
                    break;
                case BaseTag.Resources:
                    resourceAttr = SystemAPI.GetComponent<ResourceAttr>(mouseSystemData.HitEntity);
                    break;
            }

            CheckMouseHovering(ref cursorManageData, in unitSelectionData, in basicAttr, in buildingAttr,
                in resourceAttr);
        }


        #region CursorSwitchLogic

        
        private static void CheckMouseHovering(ref RefRW<CursorData> cursorManageData,
            in UnitSelectionData unitSelectionData,
            in BasicAttr basicAttr, in BuildingAttr buildingAttr, in ResourceAttr resourceAttr)
        {
            // None unit selected
            if (unitSelectionData.CurrentSelectCount == 0)
            {
                (cursorManageData.ValueRW.LeftCursorType, cursorManageData.ValueRW.RightCursorType) =
                    (TeamTag: basicAttr.FactionTag, basicAttr.BaseTag) switch
                    {
                        (FactionTag.Neutral, BaseTag.Resources) => (CursorType.CheckInfo, CursorType.None),
                        (FactionTag.Ally, BaseTag.Units) => (CursorType.ControlSelect, CursorType.None),
                        (FactionTag.Ally, BaseTag.Buildings) when buildingAttr.State == BuildingState.Produced => (
                            CursorType.Gather, CursorType.None),
                        (FactionTag.Ally, BaseTag.Buildings) when buildingAttr.State != BuildingState.Produced => (
                            CursorType.ControlSelect, CursorType.None),
                        (FactionTag.Enemy, _) => (CursorType.CheckInfo, CursorType.None),
                        (_, _) => (CursorType.UI, CursorType.None),
                    };
            }
            // Ally unit selected
            else
            {
                (cursorManageData.ValueRW.LeftCursorType, cursorManageData.ValueRW.RightCursorType) =
                    (TeamTag: basicAttr.FactionTag, basicAttr.BaseTag) switch
                    {
                        (FactionTag.Neutral, BaseTag.Resources) when resourceAttr.State == ResourceState.Available => (
                            CursorType.CheckInfo, CursorType.Harvest),
                        (FactionTag.Neutral, BaseTag.Walkable) => (CursorType.None, CursorType.March),
                        (FactionTag.Ally, BaseTag.Units) => (CursorType.ControlSelect, CursorType.Heal),
                        (FactionTag.Ally, BaseTag.Buildings) when buildingAttr.State == BuildingState.Produced => (
                            CursorType.Gather, CursorType.Garrison),
                        (FactionTag.Ally, BaseTag.Buildings) when buildingAttr.State != BuildingState.Produced => (
                            CursorType.ControlSelect, CursorType.Garrison),
                        (FactionTag.Enemy, _) => (CursorType.CheckInfo, CursorType.Attack),
                        (_, _) => (CursorType.UI, CursorType.None),
                    };
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool IsZoomingCamera(ref RefRW<CursorData> cursorManageData,
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

        private static bool IsEdgeScrolling(ref RefRW<CursorData> cursorManageData,
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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool IsDraggingCamera(ref RefRW<CursorData> cursorManageData,
            in CameraControlData cameraControlData)
        {
            if (!cameraControlData.IsDragging) return false;
            cursorManageData.ValueRW.LeftCursorType = CursorType.Drag;
            return true;
        }

        #endregion
    }
}