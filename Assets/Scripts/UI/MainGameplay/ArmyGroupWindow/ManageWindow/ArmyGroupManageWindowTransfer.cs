using System.Collections.Generic;
using SparFlame.Components.General;
using SparFlame.Components.MainGameplay;
using SparFlame.Components.SubGameplay;
using SparFlame.Components.VFX;
using SparFlame.Core.Utils;
using Unity.Collections;
using Unity.Entities;
using Unity.Transforms;

// ReSharper disable Unity.Entities.MustBeSurroundedWithRefRwRo

namespace SparFlame.UI.MainGameplay
{
    public partial class ArmyGroupManageWindowTransfer : SystemBase
    {
        private bool _initialized;

        protected override void OnCreate()
        {
            RequireForUpdate<ArmyGroupManageConfig>();
        }

        protected override void OnStartRunning()
        {
            if (!_initialized)
            {
                ArmyGroupNewWindow.Instance.OnEcsNewArmyGroup += NewArmyGroup;
                ArmyGroupManageWindow.Instance.OnEcsUpdateStaticData += UpdateStaticData;
                ArmyGroupManageWindow.Instance.OnEcsCheckSelected += CheckSelected;
                ArmyGroupManageWindow.Instance.OnEcsAddToArmyGroup += AddToArmyGroup;
                ArmyGroupManageWindow.Instance.OnEcsDeleteArmyGroup += DeleteArmyGroup;
                ArmyGroupAddTypeSelectWindow.Instance.OnEcsGiveUpAndOnlySelectExistArmyGroupUnits +=
                    GiveUpAndOnlySelectExistArmyGroupUnits;
                ArmyGroupAddTypeSelectWindow.Instance.OnEcsGiveUpAndOnlySelectNoArmyGroupUnits +=
                    GiveUpAndOnlySelectNoArmyGroupUnits;
                ArmyGroupManageWindow.Instance.OnEcsTryNewArmyGroup += TryNewArmyGroup;
                ArmyGroupManageWindow.Instance.OnEcsSelectAllUnitsWithoutArmyGroupAndGarrisoned +=
                    SelectAllUnitsWithNoArmyGroupAndGarrisoned;
                
                ArmyGroupManageCompositionWindow.Instance.OnEcsRemoveSelectedFromArmyGroup += RemoveSelectedUnitTypeFromArmyGroup;
                _initialized = true;
            }
        }


        protected override void OnUpdate()
        {
        }

        private void NewArmyGroup(string name, ArmyGroupIconType iconType)
        {
            var config = SystemAPI.GetSingleton<ArmyGroupManageConfig>();
            var currentSubStatus = SystemAPI.GetSingleton<SubGameStatusData>();
            var cityGeneralAttr = SystemAPI.GetComponent<MainGameplayGeneralAttr>(currentSubStatus.City);
            var playerFactionData = SystemAPI.GetSingleton<PlayerFactionData>();
            var garrisonConfig = SystemAPI.GetSingleton<ArmyGroupGarrisonSystemConfig>();
            var prefab = playerFactionData.faction == FactionTag.Light ? config.LightArmyGroupPrefab : config.DarkArmyGroupPrefab;
            var relationship = FactionUtils.GetRelationship(playerFactionData, cityGeneralAttr.faction, cityGeneralAttr.subFaction);
            
            // Instantiate army group
            var armyGroup = EntityManager.Instantiate(prefab);
            var armyGroupTransform = SystemAPI.GetComponent<LocalTransform>(armyGroup);
            var transform = SystemAPI.GetComponent<LocalTransform>(currentSubStatus.City);
            
            armyGroupTransform.Position = transform.Position + garrisonConfig.hidePositionBias;
            EntityManager.SetComponentData(armyGroup, armyGroupTransform);
            EntityManager.AddComponent<InSubGameTag>(armyGroup);
            EntityManager.SetComponentData(armyGroup,transform);
            EntityManager.AddComponent<MainGameplayEntityTag>(armyGroup);
            EntityManager.SetComponentData(armyGroup, new ArmyGroupAttr
            {
                gameplayName = name,
                iconType = iconType,
                saveId = SingleIdGenerator.GetNewArmyGroupId(),
            });
           
            EntityManager.SetComponentData(armyGroup, new MainGameplayGeneralAttr
            {
                faction = playerFactionData.faction,
                baseTag = MainGameBaseTag.ArmyGroup,
                subFaction = relationship is  Relationship.Player? cityGeneralAttr.subFaction : SubFactionTag.None,
            });

            // Create garrison request
            var garrisonRequest = EntityManager.CreateEntity();
            EntityManager.AddComponent<MainGameplayEntityTag>(garrisonRequest);
            EntityManager.AddComponent<ArmyGroupGarrisonRequest>(garrisonRequest);
            EntityManager.SetComponentData(garrisonRequest, new ArmyGroupGarrisonRequest
            {
                ArmyGroup = armyGroup,
                IconType = iconType,
                City = SystemAPI.GetSingleton<SubGameStatusData>().City,
                IfGarrisonIn = true
            });
            UpdateStaticData();
        }

        private void SelectAllUnitsWithNoArmyGroupAndGarrisoned()
        {
            var ecb = new EntityCommandBuffer(Allocator.Temp);
            foreach (var (_, entity) in SystemAPI.Query<RefRO<Selected>>().WithEntityAccess())
            {
                ecb.SetComponentEnabled<Selected>(entity, false);
                var vfxRequest = ecb.CreateEntity();
                ecb.AddComponent(vfxRequest, new VFXRequest
                {
                    VFXName = VFXName.UnitSelectionIndicator,
                    RequestType = VFXRequestType.Kill,
                    VFXTrackTarget = entity,
                });
                ecb.AddComponent<SubGameplayEntityTag>(vfxRequest);
            }

            ecb.Playback(EntityManager);
            ecb.Dispose();

            var ecb2 = new EntityCommandBuffer(Allocator.Temp);
            var playerFaction = SystemAPI.GetSingleton<PlayerFactionData>().faction;
            foreach (var (transform, expData, entity) in SystemAPI.Query<RefRO<LocalTransform>,
                             RefRO<ExpData>>().WithDisabled<Selected>().WithNone<InGarrison>()
                         .WithNone<InArmyGroup>().WithEntityAccess())
            {
                ecb2.SetComponentEnabled<Selected>(entity, true);
                var vfxRequest = ecb2.CreateEntity();
                ecb2.AddComponent(vfxRequest, new VFXRequest
                {
                    VFXName = VFXName.UnitSelectionIndicator,
                    RequestType = VFXRequestType.Spawn,
                    VFXTrackTarget = entity,
                    KeepDuration = 0,
                    SpawnPosition = transform.ValueRO.Position,
                    Filter = new VFXSubFilter
                    {
                        Faction = playerFaction,
                        FactionFilterEnable = true,
                        Tier = expData.ValueRO.curTier,
                        TierFilterEnable = true
                    }
                });
                ecb2.AddComponent<SubGameplayEntityTag>(vfxRequest);
            }
            ecb2.Playback(EntityManager);
            ecb2.Dispose();
        }

        private void UpdateStaticData()
        {
            var infos = new List<ArmyGroupManageInfo>();
            var subGameStatusData = SystemAPI.GetSingleton<SubGameStatusData>();
            var cityAttr = SystemAPI.GetComponent<CityAttr>(subGameStatusData.City);
            foreach (var (_, armyGroup) in SystemAPI
                         .Query<
                             RefRO<ArmyGroupAttr>>().WithEntityAccess())
            {
                // var attr = roAttr.ValueRO;
                infos.Add(new ArmyGroupManageInfo
                {
                    // IconType = attr.IconType,
                    // GameplayName = attr.GameplayName.ToString(),
                    // Speed = movableData.ValueRO.Speed,
                    // TypeDatas = unitTypeDatas,
                    ArmyGroupEntity = armyGroup,
                    TotalUnitCount = SystemAPI.GetBuffer<ArmyGroupUnit>(armyGroup).Length
                    // TotalUnitCount = units.Length
                });
            }

            ArmyGroupManageWindow.Instance.UpdateStaticData(infos, cityAttr.maxGarrisonCount);
        }

        private void CheckSelected()
        {
            var query = SystemAPI.QueryBuilder().WithAll<Selected>().WithAll<InArmyGroup>().Build();
            ArmyGroupManageWindow.Instance.HasSelectedUnitAlreadyInArmyGroup = !query.IsEmpty;
        }


        private void AddToArmyGroup(Entity armyGroup, AddToArmyGroupType type)
        {
            var ecb = new EntityCommandBuffer(Allocator.Temp);
            foreach (var (generalAttr, unitAttr, unit) in SystemAPI
                         .Query<RefRO<SubGameplayGeneralAttr>, RefRO<UnitAttr>>().WithAll<Selected>()
                         .WithEntityAccess())
            {
                if (SystemAPI.HasComponent<InArmyGroup>(unit))
                {
                    if (type == AddToArmyGroupType.AllSelectedOverrideAlreadyIn)
                    {
                        ref var inArmyGroup = ref SystemAPI.GetComponentRW<InArmyGroup>(unit).ValueRW;
                        // The unit has army group already in the same army group, then do nothing
                        if(inArmyGroup.BelongsTo ==  armyGroup)continue;
                        var removeRequest = ecb.CreateEntity();
                        ecb.AddComponent(removeRequest, new RemoveFromArmyGroupRequest
                        {
                            ArmyGroup = inArmyGroup.BelongsTo,
                            Unit = unit,
                            RemoveType = RemoveFromArmyGroupType.RemoveSpecifiedUnitWithoutRemovingInArmyGroup
                        });
                        inArmyGroup.BelongsTo = armyGroup;
                    }
                    else
                    {
                        continue;
                    }
                }
                else
                {
                    ecb.AddComponent(unit, new InArmyGroup
                    {
                        BelongsTo = armyGroup,
                    });
                }

                var datas = SystemAPI.GetBuffer<ArmyGroupUnitTypeData>(armyGroup);
                var units = SystemAPI.GetBuffer<ArmyGroupUnit>(armyGroup);
                int i;
                // Add unit type count if this unit type already exists
                for (i = 0; i < datas.Length; i++)
                {
                    var data = datas[i];
                    if (data.Id == generalAttr.ValueRO.PrefabID)
                    {
                        break;
                    }
                }

                // If not exists, add this unit type
                if (i == datas.Length)
                {
                    datas.Add(new ArmyGroupUnitTypeData
                    {
                        UnitType = unitAttr.ValueRO.Type,
                        Id = generalAttr.ValueRO.PrefabID,
                        Count = 1
                    });
                }
                else
                {
                    var data = datas[i];
                    data.Count++;
                    datas[i] = data;
                }

                // Add to buffer
                units.Add(new ArmyGroupUnit
                {
                    Unit = unit,
                    GlobalId = generalAttr.ValueRO.PrefabID
                });
            }

            ecb.Playback(EntityManager);
            ecb.Dispose();
            UpdateStaticData();
        }

        private void DeleteArmyGroup(Entity armyGroup)
        {
            var ecb = new EntityCommandBuffer(Allocator.Temp);
            ecb.DestroyEntity(armyGroup);
            var armyGroupUnits = SystemAPI.GetBuffer<ArmyGroupUnit>(armyGroup);
            foreach (var armyGroupUnit in armyGroupUnits)
            {
                if (!SystemAPI.HasComponent<InArmyGroup>(armyGroupUnit.Unit)) continue; // This should never happen
                ecb.RemoveComponent<InArmyGroup>(armyGroupUnit.Unit);
            }

            // Garrison get out of city
            if (SystemAPI.HasComponent<ArmyGroupInGarrison>(armyGroup))
            {
                var garrisonRequest = ecb.CreateEntity();
                ecb.AddComponent<MainGameplayEntityTag>(garrisonRequest);
                ecb.AddComponent(garrisonRequest, new ArmyGroupGarrisonRequest
                {
                    ArmyGroup = armyGroup,
                    IconType = SystemAPI.GetComponent<ArmyGroupAttr>(armyGroup).iconType,
                    City = SystemAPI.GetComponent<ArmyGroupInGarrison>(armyGroup).City,
                    IfGarrisonIn = false
                });
            }

            ecb.Playback(EntityManager);
            ecb.Dispose();
            UpdateStaticData();
        }

        private void TryNewArmyGroup(int currentGarrisonArmyGroupCount, int maxArmyGroupCountForSlots)
        {
            if (currentGarrisonArmyGroupCount >= maxArmyGroupCountForSlots
                || currentGarrisonArmyGroupCount >= SystemAPI
                    .GetComponent<CityAttr>(SystemAPI.GetSingleton<SubGameStatusData>().City).maxGarrisonCount)
            {
                var hintRequest = EntityManager.CreateEntity();
                EntityManager.AddComponent<HintRequest>(hintRequest);
                EntityManager.SetComponentData(hintRequest, new HintRequest
                {
                    Name = HintName.ArmyGroupCountExceededInCity
                });
                EntityManager.AddComponent<SubGameplayEntityTag>(hintRequest);
            }
            else
            {
                ArmyGroupNewWindow.Instance.Show();
            }
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

        private void RemoveSelectedUnitTypeFromArmyGroup(List<ArmyGroupUnitTypeData> unitTypeDatas, Entity armyGroup)
        {
            var ecb = new EntityCommandBuffer(Allocator.Temp);
            foreach (var typeData in unitTypeDatas)
            {
                var request = ecb.CreateEntity();
                ecb.AddComponent(request, new RemoveFromArmyGroupRequest
                {
                    ArmyGroup = armyGroup,
                    RemoveType = RemoveFromArmyGroupType.MoveOutAllSameId,
                    MoveOutId = typeData.Id
                });
                ecb.AddComponent<SubGameplayEntityTag>(request);
            }
            ecb.Playback(EntityManager);
            ecb.Dispose();
        }
    }
}