﻿using System.Runtime.CompilerServices;
using Unity.Entities;
using Unity.Burst;
using SparFlame.GamePlaySystem.General;
using SparFlame.GamePlaySystem.UnitSelection;
using SparFlame.GamePlaySystem.Building;
using SparFlame.GamePlaySystem.Resource;
using SparFlame.GamePlaySystem.CameraControl;
using SparFlame.GamePlaySystem.CustomInput;
using UnityEngine;

namespace SparFlame.GamePlaySystem.Command
{
    [UpdateAfter(typeof(UnitSelectionPlusSystem))]
    public partial struct CursorManageSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<NotPauseTag>();
            state.RequireForUpdate<CursorData>();
            state.RequireForUpdate<InputMouseData>();
            state.RequireForUpdate<UnitSelectionData>();
            state.RequireForUpdate<CameraMovementState>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var customInputSystemData = SystemAPI.GetSingleton<InputMouseData>();
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
            if (customInputSystemData.HitEntity == Entity.Null || customInputSystemData.IsOverUI)
                return;

            // Clickable = Interactable Layer + Terrain Layer

            // Hover on terrain 
            if (!SystemAPI.HasComponent<InteractableAttr>(customInputSystemData.HitEntity))
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
            var basicAttr = SystemAPI.GetComponent<InteractableAttr>(customInputSystemData.HitEntity);
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
                    buildingAttr = SystemAPI.GetComponent<BuildingAttr>(customInputSystemData.HitEntity);
                    break;
                case BaseTag.Resources:
                    resourceAttr = SystemAPI.GetComponent<ResourceAttr>(customInputSystemData.HitEntity);
                    break;
            }

            CheckMouseHovering(ref cursorManageData, in unitSelectionData, in basicAttr, in buildingAttr,
                in resourceAttr);
        }


        #region CursorSwitchLogic

        
        private static void CheckMouseHovering(ref RefRW<CursorData> cursorManageData,
            in UnitSelectionData unitSelectionData,
            in InteractableAttr interactableAttr, in BuildingAttr buildingAttr, in ResourceAttr resourceAttr)
        {
            var attr = interactableAttr;
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
                    (TeamTag: attr.FactionTag, attr.BaseTag) switch
                    {
                        (FactionTag.Neutral, BaseTag.Resources) when resourceAttr.State == ResourceState.Available => (
                            CursorType.CheckInfo, CursorType.Harvest),
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