using SparFlame.Components.General;
using SparFlame.Components.Input;
using SparFlame.Components.MainGameplay;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Collections;
using Unity.Burst;
using Unity.Rendering;
using Unity.Transforms;

namespace SparFlame.Systems.MainGameplay.ArmyGroup
{
    [BurstCompile]
    public partial struct ArmyGroupSelectionSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<ArmyGroupBillboardConfig>();
            state.RequireForUpdate<MainGamingTag>();
            state.RequireForUpdate<InputArmyGroupControlData>();
            state.RequireForUpdate<InputMouseData>();
            state.RequireForUpdate<ArmyGroupSelectionData>();
            state.RequireForUpdate<ArmyGroupSelectionConfig>();
            state.RequireForUpdate<PlayerFactionData>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var armyGroupSelectionConfig = SystemAPI.GetSingleton<ArmyGroupSelectionConfig>();
            var armyGroupSelectionData = SystemAPI.GetSingletonRW<ArmyGroupSelectionData>();

            var inputMouseData = SystemAPI.GetSingleton<InputMouseData>();
            var inputArmyGroupSelectionData = SystemAPI.GetSingleton<InputArmyGroupControlData>();
            var playerFactionData = SystemAPI.GetSingleton<PlayerFactionData>();
            var ecb = new EntityCommandBuffer(Allocator.TempJob);

         
            armyGroupSelectionData.ValueRW.CurrentSelectFaction = SystemAPI.GetSingleton<PlayerFactionData>().faction;
            // Left Click Start

            if (inputArmyGroupSelectionData.SingleSelect)
            {
                var selectable =
                    ArmyGroupUtils.IsSelectable(state.EntityManager, inputMouseData.HitEntity, playerFactionData);
                // Press AddUnitKey
                if (!inputArmyGroupSelectionData.AddArmyGroup)
                {
                    DeselectAll(ref state, ref ecb, ref armyGroupSelectionData, armyGroupSelectionConfig);
                    
                }

                if (selectable)
                {
                    var position = SystemAPI.GetComponent<LocalTransform>(inputMouseData.HitEntity).Position;
                    ToggleOne(ref state, ref ecb, ref armyGroupSelectionData, inputMouseData.HitEntity,
                        position);
                }
            }


            if (inputArmyGroupSelectionData.DragSelectStart)
            {
                if (inputArmyGroupSelectionData.AddArmyGroup)
                    LockArmyGroupSelected(ref state, ref ecb, true);
                else
                {
                    DeselectAll(ref state, ref ecb, ref armyGroupSelectionData, armyGroupSelectionConfig);
                }

                armyGroupSelectionData.ValueRW.DragSelectStart = true;
                StartSelectionBox(ref armyGroupSelectionData, inputMouseData);
            }

            if (inputArmyGroupSelectionData.DraggingSelect && armyGroupSelectionData.ValueRO.DragSelectStart)
            {
                RecordSelectionBox(ref armyGroupSelectionData, inputMouseData);
                DragSelect(ref state, ref ecb, ref armyGroupSelectionData, inputArmyGroupSelectionData.AddArmyGroup,
                    armyGroupSelectionConfig);
            }

            if (inputArmyGroupSelectionData.DragSelectEnd)
            {
                armyGroupSelectionData.ValueRW.DragSelectStart = false;
                ResetSelectionBox(ref armyGroupSelectionData, inputMouseData);
                LockArmyGroupSelected(ref state, ref ecb, false);
            }

            
            var query = SystemAPI.QueryBuilder().WithAll<ArmyGroupSelected>()
                .Build();
            var entities = query.ToEntityArray(Allocator.Temp);
            armyGroupSelectionData.ValueRW.CurrentSelectCount = entities.Length;

            if (armyGroupSelectionData.ValueRW.CurrentSelectCount < 0)
                armyGroupSelectionData.ValueRW.CurrentSelectCount = 0;
            ecb.Playback(state.EntityManager);
            ecb.Dispose();
            entities.Dispose();
        }
        
        #region SelectMethods

        private void SelectOne(ref SystemState state, ref EntityCommandBuffer ecb,
            ref RefRW<ArmyGroupSelectionData> armyGroupSelectionData, Entity entity, in bool isArmyGroupSelected,
            in float3 position)
        {
            if (!SystemAPI.HasComponent<ArmyGroupSelected>(entity)) return;
            if (state.EntityManager.IsComponentEnabled<ArmyGroupSelected>(entity) == isArmyGroupSelected) return;

            ecb.SetComponentEnabled<ArmyGroupSelected>(entity, isArmyGroupSelected);
            var addValue = isArmyGroupSelected ? 1 : -1;
            armyGroupSelectionData.ValueRW.CurrentSelectCount += addValue;
            EnableArmyGroupSelectedIndicator(ref state, ref ecb, entity, isArmyGroupSelected,
                armyGroupSelectionData.ValueRO.CurrentSelectFaction, position);
            if (isArmyGroupSelected && SystemAPI.IsComponentEnabled<ArmyGroupMovingTag>(entity))
            {
                ecb.SetComponentEnabled<ArmyGroupPathVisualizeEnabled>(entity,true);
            }
        }

        private void ToggleOne(ref SystemState state, ref EntityCommandBuffer ecb,
            ref RefRW<ArmyGroupSelectionData> armyGroupSelectionData, Entity entity,
            in float3 position)
        {
            var isArmyGroupSelected = state.EntityManager.IsComponentEnabled<ArmyGroupSelected>(entity);
            ecb.SetComponentEnabled<ArmyGroupSelected>(entity, !isArmyGroupSelected);
            var addValue = !isArmyGroupSelected ? 1 : -1;
            armyGroupSelectionData.ValueRW.CurrentSelectCount += addValue;
            EnableArmyGroupSelectedIndicator(ref state, ref ecb, entity, !isArmyGroupSelected,
                armyGroupSelectionData.ValueRO.CurrentSelectFaction, position);
            if (!isArmyGroupSelected && SystemAPI.IsComponentEnabled<ArmyGroupMovingTag>(entity))
            {
                ecb.SetComponentEnabled<ArmyGroupPathVisualizeEnabled>(entity,true);
            }
        }

        private void DeselectAll(ref SystemState state, ref EntityCommandBuffer ecb,
            ref RefRW<ArmyGroupSelectionData> armyGroupSelectionData,
            in ArmyGroupSelectionConfig armyGroupSelectionConfig)
        {
            var job = new ArmyGroupClearAllMovingTargetsJob{ECB =ecb }.Schedule(state.Dependency);
            job.Complete();
            
            var query = SystemAPI.QueryBuilder().WithAll<ArmyGroupSelected>().WithAll<LocalTransform>()
                .Build();
            var entities = query.ToEntityArray(Allocator.Temp);
            var trans = query.ToComponentDataArray<LocalTransform>(Allocator.Temp);

            for (var i = 0; i < entities.Length; i++)
            {
                var armyGroupSelectedEntity = entities[i];
                var pos = trans[i].Position;
                SelectOne(ref state, ref ecb, ref armyGroupSelectionData, armyGroupSelectedEntity, false,
                    pos
                );
            }

            armyGroupSelectionData.ValueRW.CurrentSelectCount = 0;
        }

        private void DragSelect(ref SystemState state, ref EntityCommandBuffer ecb,
            ref RefRW<ArmyGroupSelectionData> armyGroupSelectionData, in bool shouldAddUnit,
            in ArmyGroupSelectionConfig armyGroupSelectionConfig)
        {
            // Check Box Size
            if (IsBoxTooSmall(armyGroupSelectionData.ValueRW.SelectionBoxStartPos,
                    armyGroupSelectionData.ValueRW.SelectionBoxEndPos, armyGroupSelectionConfig.DragMinDistanceSq))
                return;
            // Realign start position and end position
            CalculateMinMax(armyGroupSelectionData.ValueRW.SelectionBoxStartPos,
                armyGroupSelectionData.ValueRW.SelectionBoxEndPos, out float2 min, out float2 max);

            
            foreach (var (screenPos, trans, entity) in SystemAPI.Query<RefRO<ScreenPos>, RefRO<LocalTransform>>().WithAll<InCameraView>()
                         .WithDisabled<LockArmyGroupSelectedWorkForDrag>()
                         .WithNone<ArmyGroupInGarrison>()
                         .WithEntityAccess()
                         .WithAll<PlayerTag>())
            {
                // Inside selection box
                if (IsInsideBox(screenPos.ValueRO.ScreenPosition, min, max))
                {
                    SelectOne(ref state, ref ecb, ref armyGroupSelectionData, entity,
                        true, trans.ValueRO.Position);
                }
                else
                {
                    SelectOne(ref state, ref ecb, ref armyGroupSelectionData, entity,
                        false, trans.ValueRO.Position);
                }
            }
        }

        private void LockArmyGroupSelected(ref SystemState state, ref EntityCommandBuffer ecb, in bool isLock)
        {
            if (isLock)
            {
                foreach (var (_, entity) in SystemAPI.Query<RefRO<ArmyGroupSelected>>()
                             .WithDisabled<LockArmyGroupSelectedWorkForDrag>()
                             .WithEntityAccess())
                {
                    ecb.SetComponentEnabled<LockArmyGroupSelectedWorkForDrag>(entity, true);
                }
            }
            else
            {
                foreach (var (_, entity) in SystemAPI.Query<RefRO<LockArmyGroupSelectedWorkForDrag>>()
                             .WithEntityAccess())
                {
                    ecb.SetComponentEnabled<LockArmyGroupSelectedWorkForDrag>(entity, false);
                }
            }
        }

        #endregion

        #region SelectionBox

        private static void StartSelectionBox(ref RefRW<ArmyGroupSelectionData> armyGroupSelectionData,
            in InputMouseData inputMouseData)
        {
            armyGroupSelectionData.ValueRW.SelectionBoxStartPos = new float2
                { x = inputMouseData.MousePosition.x, y = inputMouseData.MousePosition.y };
        }

        private static void RecordSelectionBox(ref RefRW<ArmyGroupSelectionData> armyGroupSelectionData,
            in InputMouseData inputMouseData)
        {
            armyGroupSelectionData.ValueRW.SelectionBoxEndPos = new float2
                { x = inputMouseData.MousePosition.x, y = inputMouseData.MousePosition.y };
            armyGroupSelectionData.ValueRW.IsDragSelecting = true;
        }

        private static void ResetSelectionBox(ref RefRW<ArmyGroupSelectionData> armyGroupSelectionData,
            in InputMouseData inputMouseData)
        {
            armyGroupSelectionData.ValueRW.SelectionBoxStartPos = float2.zero;
            armyGroupSelectionData.ValueRW.SelectionBoxEndPos = float2.zero;
            armyGroupSelectionData.ValueRW.IsDragSelecting = false;
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

        #region ArmyGroupSelectedIndicator

        private void EnableArmyGroupSelectedIndicator(ref SystemState state, ref EntityCommandBuffer ecb,
            in Entity entity,
            in bool isEnable, in FactionTag curFaction, in float3 position)
        {
            var config = SystemAPI.GetSingleton<ArmyGroupBillboardConfig>();
            var selectionBillboard = SystemAPI.GetBuffer<LinkedEntityGroup>(entity)[config.SelectChildIndex].Value;

            var disableRendering = SystemAPI.HasComponent<DisableRendering>(selectionBillboard);
            if ( disableRendering&& isEnable)
            {
                ecb.RemoveComponent<DisableRendering>(selectionBillboard);
            }

            if (!disableRendering && !isEnable)
            {
                ecb.AddComponent<DisableRendering>(selectionBillboard);
            }
            // var request = ecb.CreateEntity();
            // ecb.AddComponent(request, new VFXRequest
            // {
            //     VFXName = VFXName.ArmyGroupSelectionIndicator,
            //     Filter = new VFXSubFilter
            //     {
            //         FactionFilterEnable = true,
            //         Faction = curFaction,
            //     },
            //     SpawnPosition = position,
            //     KeepDuration = 0,
            //     StatChangeRequest = default,
            //     RequestType = isEnable ? VFXRequestType.Spawn : VFXRequestType.Kill,
            //     VFXTrackTarget = entity
            // });
        }

        #endregion
    }
}