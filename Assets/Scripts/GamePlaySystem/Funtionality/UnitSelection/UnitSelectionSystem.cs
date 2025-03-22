using Unity.Entities;
using UnityEngine;
using Unity.Mathematics;
using Unity.Collections;
using SparFlame.GamePlaySystem.General;
using SparFlame.GamePlaySystem.Mouse;
using Unity.Burst;

namespace SparFlame.GamePlaySystem.UnitSelection
{
    
    [BurstCompile]
    [UpdateAfter(typeof(CalWorldToScreenSystem))]
    public partial struct UnitSelectionSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<NotPauseTag>();
            state.RequireForUpdate<MouseSystemData>();
            state.RequireForUpdate<UnitSelectionData>();
            state.RequireForUpdate<UnitSelectionConfig>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            // TODO This code can be optimized, query can be made in onCreate method
            var unitSelectionConfig = SystemAPI.GetSingleton<UnitSelectionConfig>();
            var unitSelectionData = SystemAPI.GetSingletonRW<UnitSelectionData>();
            var clickSystemData = SystemAPI.GetSingleton<MouseSystemData>();
            var ecb = new EntityCommandBuffer(Allocator.Temp);
            var bufferLookup = SystemAPI.GetBufferLookup<LinkedEntityGroup>();
            var isDeselectAll = true;
            var isClickOnValid = false;
            
            // Left Click Start
            if (clickSystemData is { ClickFlag: ClickFlag.Start, ClickType: ClickType.Left })
            {
                StartSelectionBox(ref unitSelectionData, clickSystemData);
                // Press AddUnitKey
                if (Input.GetKey(unitSelectionConfig.AddUnitKey))
                {
                    isDeselectAll = false;  
                    LockSelected(ref state, ref ecb, true);
                }
                // Left click on clickable. Clickable layer consists of interactable layer and terrain layer. Interactable layer has basic attr always
                if (clickSystemData.HitEntity != Entity.Null && state.EntityManager.HasComponent<InteractableAttr>(clickSystemData.HitEntity))
                {
                    var basicAttributes =
                        state.EntityManager.GetComponentData<InteractableAttr>(clickSystemData.HitEntity);
                    if (basicAttributes.BaseTag == BaseTag.Units)
                    {
                        isClickOnValid = true;
                        // Left click on Valid : Same Team Unit
                        if (unitSelectionData.ValueRW.CurrentSelectFaction == basicAttributes.FactionTag)
                        {
                            // Not Press AddUnitKey
                            if (isDeselectAll)
                            {
                                // Clicked Object Not Selected
                                if (false == state.EntityManager.IsComponentEnabled<Selected>(clickSystemData
                                        .HitEntity))
                                {
                                    DeselectAll(ref state, ref ecb, ref bufferLookup, ref unitSelectionData,
                                        in unitSelectionConfig);
                                    SelectOne(ref state, ref ecb, ref bufferLookup, ref unitSelectionData,
                                        clickSystemData.HitEntity,
                                        unitSelectionConfig, true);
                                }
                                else
                                {
                                    DeselectAll(ref state, ref ecb, ref bufferLookup, ref unitSelectionData,
                                        in unitSelectionConfig);
                                }
                            }
                            // Pressed AddUnitKey
                            else
                            {
                                ToggleOne(ref state, ref ecb, ref bufferLookup,ref unitSelectionData, in clickSystemData.HitEntity, in unitSelectionConfig);
                            }
                        }
                    }
                }
                // Left Click On InValid
                if(!isClickOnValid && isDeselectAll) 
                    DeselectAll(ref state, ref ecb, ref bufferLookup, ref unitSelectionData,
                        in unitSelectionConfig);
            }
            // Double Left Click
            if(clickSystemData is { ClickFlag: ClickFlag.DoubleClick, ClickType: ClickType.Left })
                StartSelectionBox(ref unitSelectionData, clickSystemData);

            // Left-Clicking
            if (clickSystemData is { ClickFlag: ClickFlag.Clicking, ClickType: ClickType.Left })
            {
                RecordSelectionBox(ref unitSelectionData, clickSystemData);
                DragSelect(ref state, ref ecb,ref bufferLookup, ref unitSelectionData, Input.GetKey(unitSelectionConfig.AddUnitKey),
                    unitSelectionConfig);
            }

            // Left Click Up
            if (clickSystemData is { ClickFlag: ClickFlag.End, ClickType: ClickType.Left })
            {
                ResetSelectionBox(ref unitSelectionData, clickSystemData);
                LockSelected(ref state, ref ecb, false);
            }

            ecb.Playback(state.EntityManager);
            ecb.Dispose();
        }


        #region SelectMethods
        private void SelectOne(ref SystemState state, ref EntityCommandBuffer ecb,ref BufferLookup<LinkedEntityGroup> bufferLookup,
            ref RefRW<UnitSelectionData> unitSelectionData, in Entity entity,
            in UnitSelectionConfig unitSelectionConfig,in bool isSelected)
        {
            if (state.EntityManager.IsComponentEnabled<Selected>(entity) == isSelected) return;

            ecb.SetComponentEnabled<Selected>(entity, isSelected);
            var addValue = isSelected ? 1 : -1;
            unitSelectionData.ValueRW.CurrentSelectCount += addValue;
            EnableSelectedIndicator(ref state, ref ecb,ref bufferLookup, entity, isSelected, unitSelectionConfig);
        }

        private void ToggleOne(ref SystemState state, ref EntityCommandBuffer ecb,
            ref BufferLookup<LinkedEntityGroup> bufferLookup,
            ref RefRW<UnitSelectionData> unitSelectionData, in Entity entity,
            in UnitSelectionConfig unitSelectionConfig)
        {
            var isSelected = state.EntityManager.IsComponentEnabled<Selected>(entity);
            ecb.SetComponentEnabled<Selected>(entity, !isSelected);
            var addValue = !isSelected ? 1 : -1;
            unitSelectionData.ValueRW.CurrentSelectCount += addValue;
            EnableSelectedIndicator(ref state, ref ecb, ref bufferLookup, entity, !isSelected, unitSelectionConfig);
        }
        
        private void DeselectAll(ref SystemState state, ref EntityCommandBuffer ecb,ref BufferLookup<LinkedEntityGroup> bufferLookup,
            ref RefRW<UnitSelectionData> unitSelectionData, in UnitSelectionConfig unitSelectionConfig)
        {
            var query = SystemAPI.QueryBuilder().WithAll<Selected>().Build();
            foreach (var selectedEntity in query.ToEntityArray(Allocator.Temp))
            {
                SelectOne(ref state, ref ecb, ref bufferLookup,ref unitSelectionData, selectedEntity, unitSelectionConfig, false);
            }
        }

        private void DragSelect(ref SystemState state, ref EntityCommandBuffer ecb, ref BufferLookup<LinkedEntityGroup> bufferLookup,
            ref RefRW<UnitSelectionData> unitSelectionData, in bool shouldAddUnit,
            in UnitSelectionConfig unitSelectionConfig)
        {
            // Check Box Size
            if (IsBoxTooSmall(unitSelectionData.ValueRW.SelectionBoxStartPos,
                    unitSelectionData.ValueRW.SelectionBoxEndPos, unitSelectionConfig.DragMinDistanceSq))
                return;
            // Realign start position and end position
            CalculateMinMax(unitSelectionData.ValueRW.SelectionBoxStartPos,
                unitSelectionData.ValueRW.SelectionBoxEndPos, out float2 min, out float2 max);

            foreach (var (screenPos, basicAttr, entity) in SystemAPI.Query<RefRO<ScreenPos>, RefRO<InteractableAttr>>()
                         .WithDisabled<LockSelectedWorkForDrag>().WithEntityAccess())
            {
                // Inside selection box

                if (basicAttr.ValueRO.FactionTag == unitSelectionData.ValueRO.CurrentSelectFaction &&
                    IsInsideBox(screenPos.ValueRO.ScreenPosition, min, max))
                {
                    SelectOne(ref state, ref ecb, ref bufferLookup,ref unitSelectionData, entity, unitSelectionConfig,true);
                }
                else
                {
                    SelectOne(ref state, ref ecb, ref bufferLookup,ref unitSelectionData, entity, unitSelectionConfig,false);
                }
            }
        }

        private void LockSelected(ref SystemState state, ref EntityCommandBuffer ecb, in bool isLock)
        {
            if (isLock)
            {
                foreach (var (_, entity) in SystemAPI.Query<RefRO<Selected>>().WithDisabled<LockSelectedWorkForDrag>()
                             .WithEntityAccess())
                {
                    ecb.SetComponentEnabled<LockSelectedWorkForDrag>(entity, true);
                }
            }
            else
            {
                foreach (var (_, entity) in SystemAPI.Query<RefRO<LockSelectedWorkForDrag>>()
                             .WithEntityAccess())
                {
                    ecb.SetComponentEnabled<LockSelectedWorkForDrag>(entity, false);
                }
            }
        }
        
        #endregion

        #region SelectionBox

        private static void StartSelectionBox(ref RefRW<UnitSelectionData> unitSelectionData,
            in MouseSystemData mouseSystemData)
        {
            unitSelectionData.ValueRW.SelectionBoxStartPos = new float2
                { x = mouseSystemData.MousePosition.x, y = mouseSystemData.MousePosition.y };
        }

        private static void RecordSelectionBox(ref RefRW<UnitSelectionData> unitSelectionData,
            in MouseSystemData mouseSystemData)
        {
            unitSelectionData.ValueRW.SelectionBoxEndPos = new float2
                { x = mouseSystemData.MousePosition.x, y = mouseSystemData.MousePosition.y };
            unitSelectionData.ValueRW.IsDragSelecting = true;
        }

        private static void ResetSelectionBox(ref RefRW<UnitSelectionData> unitSelectionData,
            in MouseSystemData mouseSystemData)
        {
            unitSelectionData.ValueRW.SelectionBoxStartPos = float2.zero;
            unitSelectionData.ValueRW.SelectionBoxEndPos = float2.zero;
            unitSelectionData.ValueRW.IsDragSelecting = false;
        }

        #endregion

        #region MathOfSelectionBox

        private static bool IsInsideBox(float2 point, float2 min, float2 max)
        {
            var isInside = (point.x >= min.x &&
                            point.x <= max.x &&
                            point.y >= min.y &&
                            point.y <= max.y);

            return isInside;
        }

        private static void CalculateMinMax(in float2 startPos, in float2 endPos, out float2 min, out float2 max)
        {
            min = new float2
            {
                x = math.min(startPos.x, endPos.x),
                y = math.min(startPos.y, endPos.y)
            };
            max = new float2
            {
                x = math.max(startPos.x, endPos.x),
                y = math.max(startPos.y, endPos.y)
            };
        }

        private static bool IsBoxTooSmall(in float2 startPos, in float2 endPos, in float minDisSq)
        {
            var disSq = math.distancesq(startPos, endPos);
            return disSq < minDisSq;
        }

        #endregion

        #region SelectedIndicator

        private void EnableSelectedIndicator(ref SystemState state, ref EntityCommandBuffer ecb,ref BufferLookup<LinkedEntityGroup> bufferLookup, in Entity entity,
            in bool isEnable, in UnitSelectionConfig unitSelectionConfig)
        {
            if (!bufferLookup.TryGetBuffer(entity, out var linkedEntities))
            {
                return;
            }
            ecb.SetEnabled(linkedEntities[unitSelectionConfig.SelectedIndicatorIndex].Value, isEnable);
        }
        #endregion
    }
}