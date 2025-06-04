using System;
using SparFlame.GamePlaySystem.CameraControl;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Collections;
using SparFlame.GamePlaySystem.General;
using SparFlame.GamePlaySystem.CustomInput;
using SparFlame.GamePlaySystem.CustomParticleSystem;
using SparFlame.GamePlaySystem.Garrison;
using SparFlame.GamePlaySystem.Units;
using Unity.Burst;
using Unity.Transforms;

namespace SparFlame.GamePlaySystem.UnitSelection
{
    [BurstCompile]
    public partial struct UnitSelectionPlusSystem : ISystem
    {
        private BufferLookup<LinkedEntityGroup> _linkedGroupLookup;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<GamingTag>();
            state.RequireForUpdate<InputUnitControlData>();
            state.RequireForUpdate<InputMouseData>();
            state.RequireForUpdate<UnitSelectionData>();
            state.RequireForUpdate<UnitSelectionConfig>();
            state.RequireForUpdate<PlayerFactionData>();
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

            // if (unitSelectionConfig.EnableDebugSwitch && inputUnitSelectionData.ChangeFaction)
            // {
            //     DeselectAll(ref state, ref ecb,ref unitSelectionData,unitSelectionConfig);
            //     unitSelectionData.ValueRW.CurrentSelectFaction = ~unitSelectionData.ValueRW.CurrentSelectFaction;
            //     unitSelectionData.ValueRW.CurrentSelectCount = 0;
            // }
            unitSelectionData.ValueRW.CurrentSelectFaction = SystemAPI.GetSingleton<PlayerFactionData>().Value;
            // Left Click Start
            
             if (inputUnitSelectionData.ClassSelection)
            {
                var selectable = UnitSelectionUtils.IsSelectable(state.EntityManager, in unitSelectionData.ValueRO,
                    inputMouseData.HitEntity);
                if (selectable)
                {
                    DeselectAll(ref state, ref ecb, ref unitSelectionData, unitSelectionConfig);
                    var unitAttr = SystemAPI.GetComponent<UnitAttr>(inputMouseData.HitEntity);
                    switch (unitAttr.Type)
                    {
                        case UnitType.Shield:
                            foreach (var (trans,exp, entity) in SystemAPI.Query<RefRO<LocalTransform>,RefRO<ExpData>>().WithAll<InCameraView>()
                                         .WithAll<PlayerTag>().WithNone<UnitDeadTag>().WithAll<ShieldTag>().WithEntityAccess())
                            {
                                SelectOne(ref state, ref ecb, ref unitSelectionData, entity,true,exp.ValueRO,
                                    trans.ValueRO.Position);
                            }
                            break;
                        case UnitType.Ranged:
                            
                            foreach (var (trans,exp, entity) in SystemAPI.Query<RefRO<LocalTransform>,RefRO<ExpData>>().WithAll<InCameraView>()
                                         .WithAll<PlayerTag>().WithNone<UnitDeadTag>().WithAll<RangedTag>().WithEntityAccess())
                            {
                                SelectOne(ref state, ref ecb, ref unitSelectionData, entity,true,exp.ValueRO,
                                    trans.ValueRO.Position);
                            }
                            break;
                        case UnitType.Magic:
                            if (unitAttr.SubTypeIndex == (int)MagicType.Cleric)
                            {
                                foreach (var (trans,exp, entity) in SystemAPI.Query<RefRO<LocalTransform>,RefRO<ExpData>>().WithAll<InCameraView>()
                                             .WithAll<PlayerTag>().WithNone<UnitDeadTag>().WithAll<ClericTag>().WithEntityAccess())
                                {
                                    SelectOne(ref state, ref ecb, ref unitSelectionData, entity,true,exp.ValueRO,
                                        trans.ValueRO.Position);
                                }
                            }
                            else if(unitAttr.SubTypeIndex == (int)MagicType.Mage)
                            {
                                foreach (var (trans,exp, entity) in SystemAPI.Query<RefRO<LocalTransform>,RefRO<ExpData>>().WithAll<InCameraView>()
                                             .WithAll<PlayerTag>().WithNone<UnitDeadTag>().WithAll<MageTag>().WithEntityAccess())
                                {
                                    SelectOne(ref state, ref ecb, ref unitSelectionData, entity,true,exp.ValueRO,
                                        trans.ValueRO.Position);
                                }
                            }

                            break;
                        case UnitType.Cavalry:
                            foreach (var (trans,exp, entity) in SystemAPI.Query<RefRO<LocalTransform>,RefRO<ExpData>>().WithAll<InCameraView>()
                                         .WithAll<PlayerTag>().WithNone<UnitDeadTag>().WithAll<CavalryTag>().WithEntityAccess())
                            {
                                SelectOne(ref state, ref ecb, ref unitSelectionData, entity,true,exp.ValueRO,
                                    trans.ValueRO.Position);
                            }
                            break;
                        case UnitType.Worker:
                            foreach (var (trans,exp, entity) in SystemAPI.Query<RefRO<LocalTransform>,RefRO<ExpData>>().WithAll<InCameraView>()
                                         .WithAll<PlayerTag>().WithNone<UnitDeadTag>().WithAll<WorkerTag>().WithEntityAccess())
                            {
                                SelectOne(ref state, ref ecb, ref unitSelectionData, entity,true,exp.ValueRO,
                                    trans.ValueRO.Position);
                            }
                            break;
                        default:
                            throw new ArgumentOutOfRangeException();
                    }
                }
            }
            else if (inputUnitSelectionData.SingleSelect)
            {
                var selectable = UnitSelectionUtils.IsSelectable(state.EntityManager, in unitSelectionData.ValueRO,
                    inputMouseData.HitEntity);
                // Press AddUnitKey
                if (!inputUnitSelectionData.AddUnit)
                {
                    DeselectAll(ref state, ref ecb, ref unitSelectionData, unitSelectionConfig);
                }

                if (selectable)
                {
                    var expData = SystemAPI.GetComponent<ExpData>(inputMouseData.HitEntity);
                    var position = SystemAPI.GetComponent<LocalTransform>(inputMouseData.HitEntity).Position;
                    ToggleOne(ref state, ref ecb, ref unitSelectionData, inputMouseData.HitEntity, expData,
                        position);
                }
            }
            
           

            if (inputUnitSelectionData.DragSelectStart)
            {
                if (inputUnitSelectionData.AddUnit)
                    LockSelected(ref state, ref ecb, true);
                else
                {
                    DeselectAll(ref state, ref ecb, ref unitSelectionData, unitSelectionConfig);
                }

                unitSelectionData.ValueRW.DragSelectStart = true;
                StartSelectionBox(ref unitSelectionData, inputMouseData);
            }

            if (inputUnitSelectionData.DraggingSelect && unitSelectionData.ValueRO.DragSelectStart)
            {
                RecordSelectionBox(ref unitSelectionData, inputMouseData);
                DragSelect(ref state, ref ecb, ref unitSelectionData, inputUnitSelectionData.AddUnit,
                    unitSelectionConfig);
            }

            if (inputUnitSelectionData.DragSelectEnd)
            {
                unitSelectionData.ValueRW.DragSelectStart = false;
                ResetSelectionBox(ref unitSelectionData, inputMouseData);
                LockSelected(ref state, ref ecb, false);
            }


            // Reduce selection count when they are dead
            // foreach (var (request, entity) in SystemAPI.Query<RefRO<UnitSelectReduceRequest>>().WithEntityAccess())
            // {
            //     if (!request.ValueRO.IsDead)
            //     {
            //         if (SystemAPI.HasComponent<Selected>(request.ValueRO.SelectedEntity))
            //         {
            //             var expData = SystemAPI.GetComponent<ExpData>(request.ValueRO.SelectedEntity);
            //             var position = SystemAPI.GetComponent<LocalTransform>(request.ValueRO.SelectedEntity).Position;
            //             SelectOne(ref state, ref ecb, ref unitSelectionData, request.ValueRO.SelectedEntity, false,
            //                 expData, position);
            //         }
            //     }
            //     else
            //     {
            //         unitSelectionData.ValueRW.CurrentSelectCount -= 1;
            //     }
            //
            //     ecb.DestroyEntity(entity);
            // }
            var query = SystemAPI.QueryBuilder().WithAll<Selected>()
                .Build();
            var entities = query.ToEntityArray(Allocator.Temp);
            unitSelectionData.ValueRW.CurrentSelectCount = entities.Length;

            if (unitSelectionData.ValueRW.CurrentSelectCount < 0)
                unitSelectionData.ValueRW.CurrentSelectCount = 0;
            ecb.Playback(state.EntityManager);
            ecb.Dispose();
            entities.Dispose();
        }


        #region SelectMethods

        private void SelectOne(ref SystemState state, ref EntityCommandBuffer ecb,
            ref RefRW<UnitSelectionData> unitSelectionData, Entity entity, in bool isSelected,
            in ExpData expData, in float3 position)
        {
            if (!SystemAPI.HasComponent<Selected>(entity)) return;
            if (state.EntityManager.IsComponentEnabled<Selected>(entity) == isSelected) return;

            ecb.SetComponentEnabled<Selected>(entity, isSelected);
            var addValue = isSelected ? 1 : -1;
            unitSelectionData.ValueRW.CurrentSelectCount += addValue;
            EnableSelectedIndicator(ref state, ref ecb, entity, isSelected,
                unitSelectionData.ValueRO.CurrentSelectFaction, expData, position);
        }

        private void ToggleOne(ref SystemState state, ref EntityCommandBuffer ecb,
            ref RefRW<UnitSelectionData> unitSelectionData, Entity entity, in ExpData expData, in float3 position)
        {
            var isSelected = state.EntityManager.IsComponentEnabled<Selected>(entity);
            ecb.SetComponentEnabled<Selected>(entity, !isSelected);
            var addValue = !isSelected ? 1 : -1;
            unitSelectionData.ValueRW.CurrentSelectCount += addValue;
            EnableSelectedIndicator(ref state, ref ecb, entity, !isSelected,
                unitSelectionData.ValueRO.CurrentSelectFaction,
                expData, position);
        }

        private void DeselectAll(ref SystemState state, ref EntityCommandBuffer ecb,
            ref RefRW<UnitSelectionData> unitSelectionData, in UnitSelectionConfig unitSelectionConfig)
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
                SelectOne(ref state, ref ecb, ref unitSelectionData, selectedEntity, false,
                    exp, pos
                );
            }
            unitSelectionData.ValueRW.CurrentSelectCount = 0;
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

            foreach (var (screenPos, trans, expData, entity) in SystemAPI.Query<RefRO<ScreenPos>, RefRO<LocalTransform>,
                             RefRO<ExpData>>().WithAll<InCameraView>()
                         .WithDisabled<LockSelectedWorkForDrag>().WithEntityAccess().WithNone<InGarrison>()
                         .WithAll<PlayerTag>())
            {
                // Inside selection box
                if (IsInsideBox(screenPos.ValueRO.ScreenPosition, min, max))
                {
                    SelectOne(ref state, ref ecb, ref unitSelectionData, entity,
                        true, expData.ValueRO, trans.ValueRO.Position);
                }
                else
                {
                    SelectOne(ref state, ref ecb, ref unitSelectionData, entity,
                        false, expData.ValueRO, trans.ValueRO.Position);
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
                VFXName = VFXName.SelectionIndicator,
                Filter = new VFXSubFilter
                {
                    FactionFilterEnable = true,
                    Faction = curFaction,
                    TierFilterEnable = true,
                    Tier = expData.CurTier
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