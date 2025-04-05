using Unity.Entities;
using Unity.Mathematics;
using Unity.Collections;
using SparFlame.GamePlaySystem.General;
using SparFlame.GamePlaySystem.CustomInput;
using Unity.Burst;

namespace SparFlame.GamePlaySystem.UnitSelection
{
    [BurstCompile]
    [UpdateAfter(typeof(CalWorldToScreenSystem))]
    public partial struct UnitSelectionPlusSystem : ISystem
    {
        private BufferLookup<LinkedEntityGroup> _linkedGroupLookup;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<NotPauseTag>();
            state.RequireForUpdate<InputUnitControlData>();
            state.RequireForUpdate<InputMouseData>();
            state.RequireForUpdate<UnitSelectionData>();
            state.RequireForUpdate<UnitSelectionConfig>();
            _linkedGroupLookup = state.GetBufferLookup<LinkedEntityGroup>(true);
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            _linkedGroupLookup.Update(ref state);
            var unitSelectionConfig = SystemAPI.GetSingleton<UnitSelectionConfig>();
            var unitSelectionData = SystemAPI.GetSingletonRW<UnitSelectionData>();

            var inputMouseData = SystemAPI.GetSingleton<InputMouseData>();
            var inputUnitSelectionData = SystemAPI.GetSingleton<InputUnitControlData>();

            var ecb = new EntityCommandBuffer(Allocator.Temp);
   
            if (inputUnitSelectionData.ChangeFaction)
                unitSelectionData.ValueRW.CurrentSelectFaction = ~unitSelectionData.ValueRW.CurrentSelectFaction;


            // Left Click Start
            if (inputUnitSelectionData.SingleSelect)
            {
                var selectable = UnitSelectionUtils.IsSelectable(state.EntityManager, in unitSelectionData.ValueRO,
                    inputMouseData.HitEntity);
                // Press AddUnitKey
                if (!inputUnitSelectionData.AddUnit)
                {
                    DeselectAll(ref state, ref ecb, ref unitSelectionData, unitSelectionConfig);
                }
                if (selectable)
                    ToggleOne(ref state, ref ecb, ref unitSelectionData, inputMouseData.HitEntity,
                        unitSelectionConfig);
            }

            if (inputUnitSelectionData.DragSelectStart)
            {
                if(inputUnitSelectionData.AddUnit)
                    LockSelected(ref state, ref ecb, true);
                else
                {
                    DeselectAll(ref state, ref ecb, ref unitSelectionData, unitSelectionConfig);
                }
                StartSelectionBox(ref unitSelectionData, inputMouseData);
            }

            if (inputUnitSelectionData.DraggingSelect)
            {
                RecordSelectionBox(ref unitSelectionData, inputMouseData);
                DragSelect(ref state, ref ecb, ref unitSelectionData, inputUnitSelectionData.AddUnit,
                    unitSelectionConfig);
            }

            if (inputUnitSelectionData.DragSelectEnd)
            {
                ResetSelectionBox(ref unitSelectionData, inputMouseData);
                LockSelected(ref state, ref ecb, false);
            }

            // Reduce selection count when they are dead
            foreach (var (_, entity) in SystemAPI.Query<RefRO<UnitSelectReduceRequest>>().WithEntityAccess())
            {
                unitSelectionData.ValueRW.CurrentSelectCount -= 1;
                ecb.DestroyEntity(entity);
            }

            ecb.Playback(state.EntityManager);
            ecb.Dispose();
        }


        #region SelectMethods

        private void SelectOne(ref SystemState state, ref EntityCommandBuffer ecb,
            ref RefRW<UnitSelectionData> unitSelectionData, Entity entity,
            in UnitSelectionConfig unitSelectionConfig, in bool isSelected)
        {
            if (state.EntityManager.IsComponentEnabled<Selected>(entity) == isSelected) return;

            ecb.SetComponentEnabled<Selected>(entity, isSelected);
            var addValue = isSelected ? 1 : -1;
            unitSelectionData.ValueRW.CurrentSelectCount += addValue;
            EnableSelectedIndicator(ref state, ref ecb, entity, isSelected, unitSelectionConfig);
        }

        private void ToggleOne(ref SystemState state, ref EntityCommandBuffer ecb,
            ref RefRW<UnitSelectionData> unitSelectionData, Entity entity,
            in UnitSelectionConfig unitSelectionConfig)
        {
            var isSelected = state.EntityManager.IsComponentEnabled<Selected>(entity);
            ecb.SetComponentEnabled<Selected>(entity, !isSelected);
            var addValue = !isSelected ? 1 : -1;
            unitSelectionData.ValueRW.CurrentSelectCount += addValue;
            EnableSelectedIndicator(ref state, ref ecb, entity, !isSelected, unitSelectionConfig);
        }

        private void DeselectAll(ref SystemState state, ref EntityCommandBuffer ecb,
            ref RefRW<UnitSelectionData> unitSelectionData, in UnitSelectionConfig unitSelectionConfig)
        {
            var query = SystemAPI.QueryBuilder().WithAll<Selected>().Build();
            foreach (var selectedEntity in query.ToEntityArray(Allocator.Temp))
            {
                SelectOne(ref state, ref ecb, ref unitSelectionData, selectedEntity,
                    unitSelectionConfig, false);
            }
        }

        private void DragSelect(ref SystemState state, ref EntityCommandBuffer ecb,
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
                    SelectOne(ref state, ref ecb, ref unitSelectionData, entity, unitSelectionConfig,
                        true);
                }
                else
                {
                    SelectOne(ref state, ref ecb, ref unitSelectionData, entity, unitSelectionConfig,
                        false);
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
            in InputMouseData inputMouseData)
        {
            unitSelectionData.ValueRW.SelectionBoxStartPos = new float2
                { x = inputMouseData.MousePosition.x, y = inputMouseData.MousePosition.y };
        }

        private static void RecordSelectionBox(ref RefRW<UnitSelectionData> unitSelectionData,
            in InputMouseData inputMouseData)
        {
            unitSelectionData.ValueRW.SelectionBoxEndPos = new float2
                { x = inputMouseData.MousePosition.x, y = inputMouseData.MousePosition.y };
            unitSelectionData.ValueRW.IsDragSelecting = true;
        }

        private static void ResetSelectionBox(ref RefRW<UnitSelectionData> unitSelectionData,
            in InputMouseData inputMouseData)
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

        private void EnableSelectedIndicator(ref SystemState state, ref EntityCommandBuffer ecb,
            in Entity entity,
            in bool isEnable, in UnitSelectionConfig unitSelectionConfig)
        {
            if (!_linkedGroupLookup.TryGetBuffer(entity, out var linkedEntities))
            {
                return;
            }

            ecb.SetEnabled(linkedEntities[unitSelectionConfig.SelectedIndicatorIndex].Value, isEnable);
        }

        #endregion
    }
}