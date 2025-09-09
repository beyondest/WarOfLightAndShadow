using SparFlame.Components.General;
using SparFlame.Components.Input;
using SparFlame.Components.MainGameplay;
using SparFlame.Components.SubGameplay;
using SparFlame.Components.VFX;
using SparFlame.Core.Utils;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Collections;
using Unity.Burst;
using Unity.Transforms;
// ReSharper disable Unity.Entities.SingletonMustBeRequested

namespace SparFlame.Systems.SubGameplay.UnitSelection
{
    [BurstCompile]
    public partial struct UnitSelectionPlusSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<SubGamingTag>();
            state.RequireForUpdate<InputUnitControlData>();
            state.RequireForUpdate<InputMouseData>();
            state.RequireForUpdate<UnitSelectionData>();
            state.RequireForUpdate<UnitSelectionConfig>();
            state.RequireForUpdate<PlayerFactionData>();
            state.RequireForUpdate<UnitSelectionFilter>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var unitSelectionConfig = SystemAPI.GetSingleton<UnitSelectionConfig>();
            var unitSelectionData = SystemAPI.GetSingletonRW<UnitSelectionData>();
            var playerFactionData = SystemAPI.GetSingleton<PlayerFactionData>();
            var inputMouseData = SystemAPI.GetSingleton<InputMouseData>();
            var inputUnitSelectionData = SystemAPI.GetSingleton<InputUnitControlData>();

            var ecb = new EntityCommandBuffer(Allocator.Temp);

            // if (unitSelectionConfig.EnableDebugSwitch && inputUnitSelectionData.ChangeFaction)
            // {
            //     DeselectAll(ref state, ecb,ref unitSelectionData,unitSelectionConfig);
            //     unitSelectionData.ValueRW.CurrentSelectFaction = ~unitSelectionData.ValueRW.CurrentSelectFaction;
            //     unitSelectionData.ValueRW.CurrentSelectCount = 0;
            // }
            unitSelectionData.ValueRW.CurrentSelectFaction = playerFactionData.faction;
            // Left Click Start

            if (inputUnitSelectionData.ClassSelection)
            {
                var selectable = UnitSelectionUtils.IsSelectable(state.EntityManager, playerFactionData,
                    inputMouseData.HitEntity);
                if (selectable)
                {
                    DeselectAll(ref state, ecb, ref unitSelectionData);
                    var unitAttr = SystemAPI.GetComponent<UnitAttr>(inputMouseData.HitEntity);
                    switch (unitAttr.Type)
                    {
                        case UnitType.Shield:
                            foreach (var (trans, exp, entity) in SystemAPI
                                         .Query<RefRO<LocalTransform>, RefRO<ExpData>>().WithAll<InCameraView>()
                                         .WithAll<PlayerTag>().WithNone<UnitDeadTag>().WithAll<ShieldTag>()
                                         .WithEntityAccess())
                            {
                                SelectOne(ref state, ecb, ref unitSelectionData, entity, true, exp.ValueRO,
                                    trans.ValueRO.Position);
                            }

                            break;
                        case UnitType.Ranged:

                            foreach (var (trans, exp, entity) in SystemAPI
                                         .Query<RefRO<LocalTransform>, RefRO<ExpData>>().WithAll<InCameraView>()
                                         .WithAll<PlayerTag>().WithNone<UnitDeadTag>().WithAll<RangedTag>()
                                         .WithEntityAccess())
                            {
                                SelectOne(ref state, ecb, ref unitSelectionData, entity, true, exp.ValueRO,
                                    trans.ValueRO.Position);
                            }

                            break;
                        case UnitType.Magic:
                            if (unitAttr.SubTypeIndex == (int)MagicType.Cleric)
                            {
                                foreach (var (trans, exp, entity) in SystemAPI
                                             .Query<RefRO<LocalTransform>, RefRO<ExpData>>().WithAll<InCameraView>()
                                             .WithAll<PlayerTag>().WithNone<UnitDeadTag>().WithAll<ClericTag>()
                                             .WithEntityAccess())
                                {
                                    SelectOne(ref state, ecb, ref unitSelectionData, entity, true, exp.ValueRO,
                                        trans.ValueRO.Position);
                                }
                            }
                            else if (unitAttr.SubTypeIndex == (int)MagicType.Mage)
                            {
                                foreach (var (trans, exp, entity) in SystemAPI
                                             .Query<RefRO<LocalTransform>, RefRO<ExpData>>().WithAll<InCameraView>()
                                             .WithAll<PlayerTag>().WithNone<UnitDeadTag>().WithAll<MageTag>()
                                             .WithEntityAccess())
                                {
                                    SelectOne(ref state, ecb, ref unitSelectionData, entity, true, exp.ValueRO,
                                        trans.ValueRO.Position);
                                }
                            }

                            break;
                        case UnitType.Cavalry:
                            foreach (var (trans, exp, entity) in SystemAPI
                                         .Query<RefRO<LocalTransform>, RefRO<ExpData>>().WithAll<InCameraView>()
                                         .WithAll<PlayerTag>().WithNone<UnitDeadTag>().WithAll<CavalryTag>()
                                         .WithEntityAccess())
                            {
                                SelectOne(ref state, ecb, ref unitSelectionData, entity, true, exp.ValueRO,
                                    trans.ValueRO.Position);
                            }

                            break;
                        case UnitType.Worker:
                            foreach (var (trans, exp, entity) in SystemAPI
                                         .Query<RefRO<LocalTransform>, RefRO<ExpData>>().WithAll<InCameraView>()
                                         .WithAll<PlayerTag>().WithNone<UnitDeadTag>().WithAll<WorkerTag>()
                                         .WithEntityAccess())
                            {
                                SelectOne(ref state, ecb, ref unitSelectionData, entity, true, exp.ValueRO,
                                    trans.ValueRO.Position);
                            }

                            break;
                        default:
                            BurstSafe.UnexpectedEnum(unitAttr.Type);
                            break;
                    }
                }
            }
            else if (inputUnitSelectionData.SingleSelect)
            {
                var selectable = UnitSelectionUtils.IsSelectable(state.EntityManager, playerFactionData,
                    inputMouseData.HitEntity);
                // Press AddUnitKey
                if (!inputUnitSelectionData.AddUnit)
                {
                    DeselectAll(ref state, ecb, ref unitSelectionData);
                }

                if (selectable)
                {
                    var expData = SystemAPI.GetComponent<ExpData>(inputMouseData.HitEntity);
                    var position = SystemAPI.GetComponent<LocalTransform>(inputMouseData.HitEntity).Position;
                    ToggleOne(ref state, ecb, ref unitSelectionData, inputMouseData.HitEntity, expData,
                        position);
                }
            }


            if (inputUnitSelectionData.DragSelectStart)
            {
                if (inputUnitSelectionData.AddUnit)
                    LockSelected(ref state, ecb, true);
                else
                {
                    DeselectAll(ref state, ecb, ref unitSelectionData);
                }

                unitSelectionData.ValueRW.DragSelectStart = true;
                StartSelectionBox(ref unitSelectionData, inputMouseData);
            }

            if (inputUnitSelectionData.DraggingSelect && unitSelectionData.ValueRO.DragSelectStart)
            {
                RecordSelectionBox(ref unitSelectionData, inputMouseData);
                DragSelect(ref state, ecb, ref unitSelectionData, inputUnitSelectionData.AddUnit,
                    unitSelectionConfig);
            }

            if (inputUnitSelectionData.DragSelectEnd)
            {
                unitSelectionData.ValueRW.DragSelectStart = false;
                ResetSelectionBox(ref unitSelectionData, inputMouseData);
                LockSelected(ref state, ecb, false);
            }


            var query = SystemAPI.QueryBuilder().WithAll<Selected>()
                .Build();
            var entities = query.ToEntityArray(Allocator.Temp);
            unitSelectionData.ValueRW.CurrentSelectCount = entities.Length;

            if (unitSelectionData.ValueRW.CurrentSelectCount < 0)
                unitSelectionData.ValueRW.CurrentSelectCount = 0;
            ecb.Playback(state.EntityManager);
            ecb.Dispose();
            entities.Dispose();
            
            DealWithSelectionRequest(ref state);
        }

        private void DealWithSelectionRequest(ref SystemState state)
        {
            var ecb = new EntityCommandBuffer(Allocator.Temp);
            RefRW<UnitSelectionData> unitSelectionData;

            if (SystemAPI.HasSingleton<DeselectAllRequest>())
            {
                var singleton = SystemAPI.GetSingletonEntity<DeselectAllRequest>();
                state.EntityManager.DestroyEntity(singleton);
                unitSelectionData = SystemAPI.GetSingletonRW<UnitSelectionData>();
                DeselectAll(ref state, ecb,ref unitSelectionData);
            }
            unitSelectionData = SystemAPI.GetSingletonRW<UnitSelectionData>();
            foreach (var (request, entity) in SystemAPI.Query<RefRO<UnitSelectRequest>>().WithEntityAccess())
            {
                ecb.DestroyEntity(entity);
                if (SystemAPI.HasComponent<ExpData>(request.ValueRO.Unit))
                {
                    var expData = SystemAPI.GetComponent<ExpData>(request.ValueRO.Unit);
                    var pos = SystemAPI.GetComponent<LocalTransform>(request.ValueRO.Unit).Position;
                    SelectOne(ref state,ecb,ref unitSelectionData,request.ValueRO.Unit,
                        request.ValueRO.IsSelected,expData,pos);
                }
            }
            ecb.Playback(state.EntityManager);
            ecb.Dispose();
        }
        #region SelectMethods

        private void SelectOne(ref SystemState state, EntityCommandBuffer ecb,
            ref RefRW<UnitSelectionData> unitSelectionData, Entity entity, in bool isSelected,
            in ExpData expData, in float3 position)
        {
            if (!SystemAPI.HasComponent<Selected>(entity)) return;
            if (state.EntityManager.IsComponentEnabled<Selected>(entity) == isSelected) return;

            ecb.SetComponentEnabled<Selected>(entity, isSelected);
            var addValue = isSelected ? 1 : -1;
            unitSelectionData.ValueRW.CurrentSelectCount += addValue;
            EnableSelectedIndicator(ref state, ecb, entity, isSelected,
                unitSelectionData.ValueRO.CurrentSelectFaction, expData, position);
        }

        private void ToggleOne(ref SystemState state, EntityCommandBuffer ecb,
            ref RefRW<UnitSelectionData> unitSelectionData, Entity entity, in ExpData expData, in float3 position)
        {
            var isSelected = state.EntityManager.IsComponentEnabled<Selected>(entity);
            ecb.SetComponentEnabled<Selected>(entity, !isSelected);
            var addValue = !isSelected ? 1 : -1;
            unitSelectionData.ValueRW.CurrentSelectCount += addValue;
            EnableSelectedIndicator(ref state, ecb, entity, !isSelected,
                unitSelectionData.ValueRO.CurrentSelectFaction,
                expData, position);
        }

        private void DeselectAll(ref SystemState state, EntityCommandBuffer ecb,
            ref RefRW<UnitSelectionData> unitSelectionData)
        {
            var query = SystemAPI.QueryBuilder().WithAll<Selected>().WithAll<ExpData>().WithAll<LocalTransform>()
                .Build();
            var entities = query.ToEntityArray(Allocator.Temp);
            var trans = query.ToComponentDataArray<LocalTransform>(Allocator.Temp);
            var exps = query.ToComponentDataArray<ExpData>(Allocator.Temp);

            for (var i = 0; i < entities.Length; i++)
            {
                var selectedEntity = entities[i];
                var pos = trans[i].Position;
                var exp = exps[i];
                SelectOne(ref state, ecb, ref unitSelectionData, selectedEntity, false,
                    exp, pos
                );
            }

            unitSelectionData.ValueRW.CurrentSelectCount = 0;
        }

        private void DragSelect(ref SystemState state, EntityCommandBuffer ecb,
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

            var selectionFilter = SystemAPI.GetSingleton<UnitSelectionFilter>();
            
            foreach (var (screenPos, trans, expData, unitAttr, entity) in SystemAPI
                         .Query<RefRO<ScreenPos>, RefRO<LocalTransform>,
                             RefRO<ExpData>, RefRO<UnitAttr>>().WithAll<InCameraView>()
                         .WithDisabled<LockSelectedWorkForDrag>().WithEntityAccess().WithNone<InGarrison>()
                         .WithAll<PlayerTag>())
            {
                
                // Inside selection box
                if (IsInsideBox(screenPos.ValueRO.ScreenPosition, min, max))
                {
                    if (selectionFilter.UnitTypeFilterEnabled && !NativeContainerUtils.ContainsEq(selectionFilter.FilteredUnitTypes,(int)unitAttr.ValueRO.Type))continue;
                    if(selectionFilter.TierFilterEnabled && expData.ValueRO.curTier != selectionFilter.FilteredUnitTier)continue;
                    if(selectionFilter.LevelFilterEnabled && (expData.ValueRO.curLevel < selectionFilter.MinLevel || expData.ValueRO.curLevel > selectionFilter.MaxLevel) )continue;
                    
                    SelectOne(ref state, ecb, ref unitSelectionData, entity,
                        true, expData.ValueRO, trans.ValueRO.Position);
                }
                else
                {
                    SelectOne(ref state, ecb, ref unitSelectionData, entity,
                        false, expData.ValueRO, trans.ValueRO.Position);
                }
            }
        }

        private void LockSelected(ref SystemState state, EntityCommandBuffer ecb, in bool isLock)
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

        private void EnableSelectedIndicator(ref SystemState state, EntityCommandBuffer ecb,
            in Entity entity,
            in bool isEnable, in FactionTag curFaction, in ExpData expData, in float3 position)
        {
            // if (!_linkedGroupLookup.TryGetBuffer(entity, out var linkedEntities))
            // {
            //     return;
            // }
            //
            // ecb.SetEnabled(linkedEntities[unitSelectionConfig.SelectedIndicatorIndex].Value, isEnable);
            var request = ecb.CreateEntity();
            ecb.AddComponent(request, new VFXRequest
            {
                VFXName = VFXName.UnitSelectionIndicator,
                Filter = new VFXSubFilter
                {
                    FactionFilterEnable = true,
                    Faction = curFaction,
                    TierFilterEnable = true,
                    Tier = expData.curTier
                },
                SpawnPosition = position,
                KeepDuration = 0,
                StatChangeRequest = default,
                RequestType = isEnable ? VFXRequestType.Spawn : VFXRequestType.Kill,
                VFXTrackTarget = entity
            });
        }

        #endregion
    }
}