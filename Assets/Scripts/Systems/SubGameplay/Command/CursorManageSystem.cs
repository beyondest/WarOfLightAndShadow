using System.Runtime.CompilerServices;
using SparFlame.Components.General;
using SparFlame.Components.Input;
using SparFlame.Components.MainGameplay;
using SparFlame.Components.SubGameplay;
using Unity.Entities;
using Unity.Burst;

namespace SparFlame.Systems.SubGameplay.Command
{
    [BurstCompile]
    [UpdateBefore(typeof(PlayerCommandSystem))]
    public partial struct CursorManageSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<SubGamingTag>();
            state.RequireForUpdate<SubGameplayCursorData>();
            state.RequireForUpdate<InputMouseData>();
            state.RequireForUpdate<UnitSelectionData>();
            state.RequireForUpdate<CameraMovementState>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var customMouseData = SystemAPI.GetSingleton<InputMouseData>();
            var cursorManageData = SystemAPI.GetSingletonRW<SubGameplayCursorData>();
            var unitSelectionData = SystemAPI.GetSingleton<UnitSelectionData>();
            var cameraControlData = SystemAPI.GetSingleton<CameraMovementState>();

            cursorManageData.ValueRW.LeftCursorType = SubGameplayCursorType.UI;
            cursorManageData.ValueRW.RightCursorType = SubGameplayCursorType.None;

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
            if (!SystemAPI.HasComponent<SubGameplayGeneralAttr>(customMouseData.HitEntity))
            {
                if (unitSelectionData.CurrentSelectCount == 0)
                {
                    cursorManageData.ValueRW.LeftCursorType = SubGameplayCursorType.None;
                    cursorManageData.ValueRW.RightCursorType = SubGameplayCursorType.None;
                }
                else
                {
                    cursorManageData.ValueRW.LeftCursorType = SubGameplayCursorType.None;
                    cursorManageData.ValueRW.RightCursorType = SubGameplayCursorType.March;
                }
                return;
            }
            
            // Hover on interactable
            var basicAttr = SystemAPI.GetComponent<SubGameplayGeneralAttr>(customMouseData.HitEntity);
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

        
        private static void CheckMouseHovering(ref RefRW<SubGameplayCursorData> cursorManageData,
            in UnitSelectionData unitSelectionData,
            in SubGameplayGeneralAttr subGameplayGeneralAttr, in BuildingAttr buildingAttr, bool isResourceValid)
        {
            var attr = subGameplayGeneralAttr;
            if(unitSelectionData.CurrentSelectFaction != FactionTag.Ally)
                attr.FactionTag = ~attr.FactionTag;
            
            // None unit selected
            if (unitSelectionData.CurrentSelectCount == 0)
            {
                (cursorManageData.ValueRW.LeftCursorType, cursorManageData.ValueRW.RightCursorType) =
                    (TeamTag: attr.FactionTag, attr.BaseTag) switch
                    {
                        (FactionTag.Neutral, BaseTag.Resources) => (SubGameplayCursorType.CheckInfo, SubGameplayCursorType.None),
                        (FactionTag.Ally, BaseTag.Units) => (SubGameplayCursorType.ControlSelect, SubGameplayCursorType.None),
                        // (FactionTag.Ally, BaseTag.Buildings) when buildingAttr.CurBuildingState == BuildingState.Worked => (
                        //     CursorType.Gather, CursorType.None),
                        (FactionTag.Ally, BaseTag.Buildings)/* when buildingAttr.CurBuildingState != BuildingState.Worked*/ => (
                            SubGameplayCursorType.ControlSelect, SubGameplayCursorType.None),
                        (FactionTag.Enemy, _) => (SubGameplayCursorType.CheckInfo, SubGameplayCursorType.None),
                        (_, _) => (SubGameplayCursorType.UI, SubGameplayCursorType.None),
                    };
            }
            // Ally unit selected
            else
            {
                (cursorManageData.ValueRW.LeftCursorType, cursorManageData.ValueRW.RightCursorType) =
                    (TeamTag: attr.FactionTag, attr.BaseTag) switch
                    {
                        (FactionTag.Neutral, BaseTag.Resources) when isResourceValid => (
                            SubGameplayCursorType.CheckInfo, SubGameplayCursorType.Harvest),
                        (FactionTag.Ally, BaseTag.Units) => (SubGameplayCursorType.ControlSelect, SubGameplayCursorType.Heal),
                        // (FactionTag.Ally, BaseTag.Buildings) when buildingAttr.CurBuildingState == BuildingState.Worked => (
                        //     CursorType.Gather, CursorType.Garrison),
                        (FactionTag.Ally, BaseTag.Buildings)/* when buildingAttr.CurBuildingState != BuildingState.Worked*/ => (
                            SubGameplayCursorType.ControlSelect, SubGameplayCursorType.Garrison),
                        (FactionTag.Enemy, _) => (SubGameplayCursorType.CheckInfo, SubGameplayCursorType.Attack),
                        (_, _) => (SubGameplayCursorType.UI, SubGameplayCursorType.None),
                    };
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool IsZoomingCamera(ref RefRW<SubGameplayCursorData> cursorManageData,
            in CameraMovementState cameraMovementState)
        {
            cursorManageData.ValueRW.LeftCursorType = cameraMovementState.ZState switch
            {
                CameraZoomState.ZoomIn => SubGameplayCursorType.ZoomIn,
                CameraZoomState.ZoomOut => SubGameplayCursorType.ZoomOut,
                _ => SubGameplayCursorType.UI
            };
            return cursorManageData.ValueRW.LeftCursorType != SubGameplayCursorType.UI;
        }

        private static bool IsEdgeScrolling(ref RefRW<SubGameplayCursorData> cursorManageData,
            in CameraMovementState cameraMovementState)
        {
            cursorManageData.ValueRW.LeftCursorType =
                cameraMovementState.EState switch
                {
                    EdgeMoveState.Down => SubGameplayCursorType.ArrowDown,
                    EdgeMoveState.Up => SubGameplayCursorType.ArrowUp,
                    EdgeMoveState.Left => SubGameplayCursorType.ArrowLeft,
                    EdgeMoveState.Right => SubGameplayCursorType.ArrowRight,
                    EdgeMoveState.LeftDown => SubGameplayCursorType.ArrowLeftDown,
                    EdgeMoveState.RightDown => SubGameplayCursorType.ArrowRightDown,
                    EdgeMoveState.LeftUp => SubGameplayCursorType.ArrowLeftUp,
                    EdgeMoveState.RightUp => SubGameplayCursorType.ArrowRightUp,
                    _ => SubGameplayCursorType.UI,
                };
            return cursorManageData.ValueRW.LeftCursorType != SubGameplayCursorType.UI;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool IsDraggingCamera(ref RefRW<SubGameplayCursorData> cursorManageData,
            in CameraMovementState cameraMovementState)
        {
            if (!cameraMovementState.IsDragging) return false;
            cursorManageData.ValueRW.LeftCursorType = SubGameplayCursorType.Drag;
            return true;
        }

        #endregion
    }
}