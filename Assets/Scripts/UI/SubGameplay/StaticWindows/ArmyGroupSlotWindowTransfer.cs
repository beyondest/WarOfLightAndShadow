using System.Collections.Generic;
using SparFlame.Components.General;
using SparFlame.Components.MainGameplay;
using SparFlame.Components.SubGameplay;
using SparFlame.Components.VFX;
using SparFlame.UI.MainGameplay;
using Unity.Collections;
using Unity.Entities;


namespace SparFlame.UI.SubGameplay.StaticWindows
{
    public partial class ArmyGroupSlotWindowTransfer : SystemBase
    {
        private bool _initialized;

        protected override void OnCreate()
        {
            RequireForUpdate<ArmyGroupConfig>();
            RequireForUpdate<SubGamingTag>();
        }

        protected override void OnStartRunning()
        {
            if (!_initialized)
            {
                ArmyGroupAddTypeSelectWindow.Instance.OnEcsGiveUpAndOnlySelectExistArmyGroupUnits +=
                    GiveUpAndOnlySelectExistArmyGroupUnits;
                ArmyGroupAddTypeSelectWindow.Instance.OnEcsGiveUpAndOnlySelectNoArmyGroupUnits +=
                    GiveUpAndOnlySelectNoArmyGroupUnits;
                ArmyGroupSlotWindow.Instance.OnEcsSelectAllUnitsWithoutArmyGroupAndGarrisoned +=
                    QuickSelectAllUnitsWithNoArmyGroupAndGarrisoned;
                ArmyGroupSlotWindow.Instance.OnEcsCheckSelected += CheckSelected;
                ArmyGroupSlotWindow.Instance.OnEcsAddToArmyGroup += AddToArmyGroup;
                ArmyGroupSlotWindow.Instance.OnEcsRemoveSelectedUnitsFromTheirArmyGroup +=
                    RemoveSelectedUnitsFromTheirArmyGroup;

                ArmyGroupSlotWindow.Instance.OnEcsSelectArmyGroupUnits += SelectArmyGroupUnits;
                ArmyGroupSlotWindow.Instance.OnEcsSprintArmyGroupUnits += SprintArmyGroupUnits;
                ArmyGroupSlotWindow.Instance.OnEcsCastSkill += CastSkill ;
                ArmyGroupSlotWindow.Instance.OnEcsHoldSwitchArmyGroup += HoldSwitchArmyGroup;
                ArmyGroupSlotWindow.Instance.OnEcsUpdateArmyGroupAvgData += UpdateArmyGroupAvgData;
                _initialized = true;
            }
        }

        private void CastSkill(Entity armyGroup)
        {
            var entity =   EntityManager.CreateEntity();
            EntityManager.AddComponent<SubGameplayEntityTag>(entity);
            EntityManager.AddComponent<ArmyGroupCastSkillRequest>(entity);
            EntityManager.SetComponentData(entity,new ArmyGroupCastSkillRequest
            {
                ArmyGroup = armyGroup
            });
        }

        protected override void OnUpdate()
        {
            var infos = new List<ArmyGroupSlotInfo>();
            var subGameStatusData = SystemAPI.GetSingleton<SubGameStatusData>();
            var inBattle = GameStatusUtils.IsInBattle(subGameStatusData);

            foreach (var (attr, skillTimer,
                         statData, armyGroup) in SystemAPI.Query<RefRO<ArmyGroupAttr>
                         , RefRO<ArmyGroupSkillTimer>,
                         RefRO<ArmyGroupStatData>>().WithAll<InSubGameTag>()
                         .WithAll<PlayerTag>().WithEntityAccess())
            {
                var chargeRatio = skillTimer.ValueRO.MaxChargeCoolDown == 0
                    ? 1
                    : 1 - skillTimer.ValueRO.ChargeCoolDown / skillTimer.ValueRO.MaxChargeCoolDown;
                var sprintCoolDownRatio = skillTimer.ValueRO.MaxSprintCoolDown == 0
                    ? 0
                    : skillTimer.ValueRO.SprintCoolDown / skillTimer.ValueRO.MaxSprintCoolDown;

                float hpRatio;
                int startUnitCount;
                if (inBattle)
                {
                    var snapShot = SystemAPI.GetComponent<BeforeBattleArmyGroupSnapShot>(armyGroup);
                    hpRatio = snapShot.MaxHp == 0 ? 0 : statData.ValueRO.totalCurrentHp / snapShot.MaxHp;
                    startUnitCount = snapShot.UnitCount;
                }
                else
                {
                    hpRatio = statData.ValueRO.totalMaxHp == 0
                        ? 0
                        : statData.ValueRO.totalCurrentHp / statData.ValueRO.totalMaxHp;
                    startUnitCount = SystemAPI.GetBuffer<ArmyGroupUnit>(armyGroup).Length;
                }

                infos.Add(new ArmyGroupSlotInfo
                {
                    ArmyGroup = armyGroup,
                    IconType = attr.ValueRO.iconType,
                    ChargeRatio = chargeRatio,
                    HpRatio = hpRatio,
                    CurrentUnitCount = SystemAPI.GetBuffer<ArmyGroupUnit>(armyGroup).Length,
                    IsHolding = SystemAPI.HasComponent<ArmyGroupHoldOnTag>(armyGroup),
                    SprintCooldownRatio = sprintCoolDownRatio,
                    StartingUnitCount = startUnitCount,
                    CreateTimeTotalHours = attr.ValueRO.createTimeInTotalHours,
                });
            }

            ArmyGroupSlotWindow.Instance.UpDateCandidates(infos,
                subGameStatusData.SubGameStatus == SubGameStatus.PlayerCity);
        }

        private void SprintArmyGroupUnits(Entity armyGroup)
        {
            var request = EntityManager.CreateEntity();
            EntityManager.AddComponent<SubGameplayEntityTag>(request);
            EntityManager.AddComponent<ArmyGroupSprintRequest>(request);
            EntityManager.SetComponentData(request, new ArmyGroupSprintRequest
            {
                ArmyGroup = armyGroup
            });
        }

        private void HoldSwitchArmyGroup(Entity armyGroup)
        {
            var request = EntityManager.CreateEntity();
            EntityManager.AddComponent<SubGameplayEntityTag>(request);
            EntityManager.AddComponent<ArmyGroupHoldSwitchRequest>(request);
            EntityManager.SetComponentData(request, new ArmyGroupHoldSwitchRequest
            {
                ArmyGroup = armyGroup
            });
        }

        private void SelectArmyGroupUnits(Entity armyGroup, bool ifAdd)
        {
            if (!ifAdd)
                EntityManager.CreateSingleton<DeselectAllRequest>();
            var ecb = new EntityCommandBuffer(Allocator.Temp);
            foreach (var (inArmyGroup, unit
                         ) in SystemAPI.Query<RefRO<InArmyGroup>>().WithNone<InGarrison>().WithEntityAccess())
            {
                if (inArmyGroup.ValueRO.BelongsTo == armyGroup)
                {
                    var request = ecb.CreateEntity();
                    ecb.AddComponent<SubGameplayEntityTag>(request);
                    ecb.AddComponent(request, new UnitSelectRequest
                    {
                        Unit = unit,
                        IsSelected = true
                    });
                }
            }

            ecb.Playback(EntityManager);
            ecb.Dispose();
        }

        private void CheckSelected()
        {
            var query = SystemAPI.QueryBuilder().WithAll<Selected>().WithAll<InArmyGroup>().Build();
            ArmyGroupSlotWindow.Instance.HasSelectedUnitAlreadyInArmyGroup = !query.IsEmpty;
        }

        private void AddToArmyGroup(Entity armyGroup, AddToArmyGroupType type)
        {
            var request = EntityManager.CreateEntity();
            EntityManager.AddComponent<AddToArmyGroupRequest>(request);
            EntityManager.SetComponentData(request, new AddToArmyGroupRequest
            {
                ArmyGroup = armyGroup,
                Type = type,
                Unit = Entity.Null,
            });
        }

        private void RemoveSelectedUnitsFromTheirArmyGroup()
        {
            var ecb = new EntityCommandBuffer(Allocator.Temp);
            foreach (var (inArmyGroup, prefabId, unit) in SystemAPI
                         .Query<RefRO<InArmyGroup>, RefRO<PrefabId>>().WithAll<Selected>()
                         .WithEntityAccess())
            {
                var request = ecb.CreateEntity();
                ecb.AddComponent<SubGameplayEntityTag>(request);
                ecb.AddComponent(request, new RemoveFromArmyGroupRequest
                {
                    ArmyGroup = inArmyGroup.ValueRO.BelongsTo,
                    Unit = unit,
                    RemoveType = RemoveFromArmyGroupType.RemoveSpecifiedUnitWithoutRemovingInArmyGroup,
                    MoveOutId = prefabId.ValueRO.value
                });
                ecb.RemoveComponent<InArmyGroup>(unit);
            }

            ecb.Playback(EntityManager);
            ecb.Dispose();
        }

        private void QuickSelectAllUnitsWithNoArmyGroupAndGarrisoned(bool tierFilterEnabled,
            Tier filterTier, List<UnitType> unitTypeFilter)
        {
            // Deselect all
            EntityManager.CreateSingleton<DeselectAllRequest>();

            var ecb = new EntityCommandBuffer(Allocator.Temp);
            foreach (var (unitAttr, expData, entity) in SystemAPI.Query<RefRO<UnitAttr>, RefRO<ExpData>>()
                         .WithDisabled<Selected>().WithAll<PlayerTag>()
                         .WithNone<InGarrison>()
                         .WithNone<InArmyGroup>().WithEntityAccess())
            {
                if (tierFilterEnabled && expData.ValueRO.curTier != filterTier) continue;
                if (!unitTypeFilter.Contains(unitAttr.ValueRO.type)) continue;
                var request = ecb.CreateEntity();
                ecb.AddComponent<SubGameplayEntityTag>(request);
                ecb.AddComponent(request, new UnitSelectRequest
                {
                    Unit = entity,
                    IsSelected = true
                });
            }

            ecb.Playback(EntityManager);
            ecb.Dispose();
        }


        private void GiveUpAndOnlySelectNoArmyGroupUnits()
        {
            var ecb = new EntityCommandBuffer(Allocator.Temp);
            foreach (var (_, entity) in SystemAPI.Query<RefRO<Selected>>().WithAll<InArmyGroup>().WithEntityAccess())
            {
                ecb.SetComponentEnabled<Selected>(entity, false);
                var vfxRequest = ecb.CreateEntity();
                ecb.AddComponent<SubGameplayEntityTag>(vfxRequest);
                ecb.AddComponent(vfxRequest, new VFXRequest
                {
                    VFXName = VFXName.UnitSelectionIndicator,
                    RequestType = VFXRequestType.Kill,
                    VFXTrackTarget = entity
                });
            }

            ecb.Playback(EntityManager);
            ecb.Dispose();
        }

        private void GiveUpAndOnlySelectExistArmyGroupUnits()
        {
            var ecb = new EntityCommandBuffer(Allocator.Temp);
            foreach (var (_, entity) in SystemAPI.Query<RefRO<Selected>>().WithNone<InArmyGroup>().WithEntityAccess())
            {
                ecb.SetComponentEnabled<Selected>(entity, false);
                var vfxRequest = ecb.CreateEntity();
                ecb.AddComponent<SubGameplayEntityTag>(vfxRequest);
                ecb.AddComponent(vfxRequest, new VFXRequest
                {
                    VFXName = VFXName.UnitSelectionIndicator,
                    RequestType = VFXRequestType.Kill,
                    VFXTrackTarget = entity
                });
            }

            ecb.Playback(EntityManager);
            ecb.Dispose();
        }

        private void UpdateArmyGroupAvgData()
        {
            var subGameStatusData = SystemAPI.GetSingleton<SubGameStatusData>();
            var garrisonEntities = SystemAPI.GetBuffer<CityGarrisonEntity>(subGameStatusData.City);
            foreach (var armyGroup in garrisonEntities)
            {
                if (!SystemAPI.HasBuffer<ArmyGroupUnit>(armyGroup.ArmyGroup)) continue;
                ArmyGroupUtils.UpdateArmyGroupInfoForCompoChanged(EntityManager, armyGroup.ArmyGroup);
            }
        }
    }
}