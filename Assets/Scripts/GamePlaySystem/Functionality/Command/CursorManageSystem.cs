using System.Runtime.CompilerServices;
using Unity.Entities;
using Unity.Burst;
using SparFlame.GamePlaySystem.General;
using SparFlame.GamePlaySystem.UnitSelection;
using SparFlame.GamePlaySystem.Building;
using SparFlame.GamePlaySystem.Resource;
using SparFlame.GamePlaySystem.CameraControl;
using SparFlame.GamePlaySystem.CustomInput;

namespace SparFlame.GamePlaySystem.Command
{
    [BurstCompile]
    [UpdateAfter(typeof(UnitSelectionPlusSystem))]
    public partial struct CursorManageSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<GamingTag>();
            state.RequireForUpdate<CursorData>();
            state.RequireForUpdate<InputMouseData>();
            state.RequireForUpdate<UnitSelectionData>();
            state.RequireForUpdate<CameraMovementState>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var customMouseData = SystemAPI.GetSingleton<InputMouseData>();
            var cursorManageData = SystemAPI.GetSingletonRW<CursorData>();
            var unitSelectionData = SystemAPI.GetSingleton<UnitSelectionData>();
            var cameraControlData = SystemAPI.GetSingleton<CameraMovementState>();

            cursorManageData.ValueRW.LeftCursorType = CursorType.UI;
            cursorManageData.ValueRW.RightCursorType = CursorType.None;

            // Check is dragging
            if (IsDraggingCamera(ref cursorManageData, in cameraControlData)) return;

            // Check is edge scrolling
            if (IsEdgeScrolling(ref cursorManageData, in cameraControlData)) return;

            // Check is zooming
            if (IsZoomingCamera(ref cursorManageData, in cameraControlData)) return;
            
            // Not Clickable. Like Nav layer object; default layer objects; or over UI
            if (customMouseData.HitEntity == Entity.Null || customMouseData.IsOverUI)
                return;

            // Clickable = Interactable Layer + Terrain Layer

            // Hover on terrain 
            if (!SystemAPI.HasComponent<GeneralAttr>(customMouseData.HitEntity))
            {
                if (unitSelectionData.CurrentSelectCount == 0)
                {
                    cursorManageData.ValueRW.LeftCursorType = CursorType.None;
                    cursorManageData.ValueRW.RightCursorType = CursorType.None;
                }
                else
                {
                    cursorManageData.ValueRW.LeftCursorType = CursorType.None;
                    cursorManageData.ValueRW.RightCursorType = CursorType.March;
                }
                return;
            }
            
            // Hover on interactable
            var basicAttr = SystemAPI.GetComponent<GeneralAttr>(customMouseData.HitEntity);
            var buildingAttr = new BuildingAttr();
            var isResourceValid = true;
            switch (basicAttr.BaseTag)
            {
                case BaseTag.Buildings:
                    buildingAttr = SystemAPI.GetComponent<BuildingAttr>(customMouseData.HitEntity);
                    break;
                case BaseTag.Resources:
                    if (state.EntityManager.HasComponent<RegeneratingTag>(customMouseData.HitEntity))
                    {
                        isResourceValid = false;
                    }
                    break;
            }

            CheckMouseHovering(ref cursorManageData, in unitSelectionData, in basicAttr, in buildingAttr,
                isResourceValid);
        }


        #region CursorSwitchLogic

        
        private static void CheckMouseHovering(ref RefRW<CursorData> cursorManageData,
            in UnitSelectionData unitSelectionData,
            in GeneralAttr generalAttr, in BuildingAttr buildingAttr, bool isResourceValid)
        {
            var attr = generalAttr;
            if(unitSelectionData.CurrentSelectFaction != FactionTag.Ally)
                attr.FactionTag = ~attr.FactionTag;
            
            // None unit selected
            if (unitSelectionData.CurrentSelectCount == 0)
            {
                (cursorManageData.ValueRW.LeftCursorType, cursorManageData.ValueRW.RightCursorType) =
                    (TeamTag: attr.FactionTag, attr.BaseTag) switch
                    {
                        (FactionTag.Neutral, BaseTag.Resources) => (CursorType.CheckInfo, CursorType.None),
                        (FactionTag.Ally, BaseTag.Units) => (CursorType.ControlSelect, CursorType.None),
                        // (FactionTag.Ally, BaseTag.Buildings) when buildingAttr.CurBuildingState == BuildingState.Worked => (
                        //     CursorType.Gather, CursorType.None),
                        (FactionTag.Ally, BaseTag.Buildings)/* when buildingAttr.CurBuildingState != BuildingState.Worked*/ => (
                            CursorType.ControlSelect, CursorType.None),
                        (FactionTag.Enemy, _) => (CursorType.CheckInfo, CursorType.None),
                        (_, _) => (CursorType.UI, CursorType.None),
                    };
            }
            // Ally unit selected
            else
            {
                (cursorManageData.ValueRW.LeftCursorType, cursorManageData.ValueRW.RightCursorType) =
                    (TeamTag: attr.FactionTag, attr.BaseTag) switch
                    {
                        (FactionTag.Neutral, BaseTag.Resources) when isResourceValid => (
                            CursorType.CheckInfo, CursorType.Harvest),
                        (FactionTag.Ally, BaseTag.Units) => (CursorType.ControlSelect, CursorType.Heal),
                        // (FactionTag.Ally, BaseTag.Buildings) when buildingAttr.CurBuildingState == BuildingState.Worked => (
                        //     CursorType.Gather, CursorType.Garrison),
                        (FactionTag.Ally, BaseTag.Buildings)/* when buildingAttr.CurBuildingState != BuildingState.Worked*/ => (
                            CursorType.ControlSelect, CursorType.Garrison),
                        (FactionTag.Enemy, _) => (CursorType.CheckInfo, CursorType.Attack),
                        (_, _) => (CursorType.UI, CursorType.None),
                    };
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool IsZoomingCamera(ref RefRW<CursorData> cursorManageData,
            in CameraMovementState cameraMovementState)
        {
            cursorManageData.ValueRW.LeftCursorType = cameraMovementState.ZState switch
            {
                CameraZoomState.ZoomIn => CursorType.ZoomIn,
                CameraZoomState.ZoomOut => CursorType.ZoomOut,
                _ => CursorType.UI
            };
            return cursorManageData.ValueRW.LeftCursorType != CursorType.UI;
        }

        private static bool IsEdgeScrolling(ref RefRW<CursorData> cursorManageData,
            in CameraMovementState cameraMovementState)
        {
            cursorManageData.ValueRW.LeftCursorType =
                cameraMovementState.EState switch
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
            in CameraMovementState cameraMovementState)
        {
            if (!cameraMovementState.IsDragging) return false;
            cursorManageData.ValueRW.LeftCursorType = CursorType.Drag;
            return true;
        }

        #endregion
    }
}