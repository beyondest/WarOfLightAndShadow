using System.Collections.Generic;
using SparFlame.Components.General;
using SparFlame.Components.MainGameplay;
using SparFlame.Core.GlobalMono;
using SparFlame.Core.Utils;
using SparFlame.UI.SubGameplay.StaticWindows.Buttons;
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
            RequireForUpdate<ArmyGroupConfig>();
        }

        protected override void OnStartRunning()
        {
            if (!_initialized)
            {
                ArmyGroupNewWindow.Instance.OnEcsNewArmyGroup += NewArmyGroup;
                ArmyGroupManageWindow.Instance.OnEcsUpdateStaticData += () => UpdateStaticData();
                ArmyGroupManageWindow.Instance.OnEcsDeleteArmyGroup += DeleteArmyGroup;
              
                ArmyGroupManageWindow.Instance.OnEcsTryNewArmyGroup += TryNewArmyGroup;
   
                
                

                
                _initialized = true;
            }
        }


        protected override void OnUpdate()
        {
        }

        private void NewArmyGroup(string name, ArmyGroupIconType iconType)
        {
            var config = SystemAPI.GetSingleton<ArmyGroupConfig>();
            var currentSubStatus = SystemAPI.GetSingleton<SubGameStatusData>();
            var cityGeneralAttr = SystemAPI.GetComponent<MainGameplayGeneralAttr>(currentSubStatus.City);
            var playerFactionData = SystemAPI.GetSingleton<PlayerFactionData>();
            var garrisonConfig = SystemAPI.GetSingleton<ArmyGroupGarrisonSystemConfig>();
            var prefab = playerFactionData.faction == FactionTag.Light
                ? config.LightArmyGroupPrefab
                : config.DarkArmyGroupPrefab;
            var relationship =
                FactionUtils.GetRelationship(playerFactionData, cityGeneralAttr.faction, cityGeneralAttr.subFaction);

            // Instantiate army group
            var armyGroup = EntityManager.Instantiate(prefab);
            var armyGroupTransform = SystemAPI.GetComponent<LocalTransform>(armyGroup);
            var transform = SystemAPI.GetComponent<LocalTransform>(currentSubStatus.City);

            armyGroupTransform.Position = transform.Position + garrisonConfig.hidePositionBias;
            EntityManager.SetComponentData(armyGroup, armyGroupTransform);
            EntityManager.AddComponent<InSubGameTag>(armyGroup);
            EntityManager.SetComponentData(armyGroup, transform);
            EntityManager.AddComponent<MainGameplayEntityTag>(armyGroup);
            EntityManager.SetComponentData(armyGroup, new ArmyGroupAttr
            {
                gameplayName = name,
                iconType = iconType,
                saveId = SingleIdGenerator.GetNewArmyGroupId(),
                createTimeInTotalHours = SystemAPI.GetSingleton<WorldTimeData>().totalHours
            });

            EntityManager.SetComponentData(armyGroup, new MainGameplayGeneralAttr
            {
                faction = playerFactionData.faction,
                baseTag = MainGameBaseTag.ArmyGroup,
                subFaction = relationship is Relationship.Player ? cityGeneralAttr.subFaction : SubFactionTag.None,
            });

            // Create garrison request
            var garrisonRequest = EntityManager.CreateEntity();
            EntityManager.AddComponent<MainGameplayEntityTag>(garrisonRequest);
            EntityManager.AddComponent<ArmyGroupGarrisonRequest>(garrisonRequest);
            EntityManager.SetComponentData(garrisonRequest, new ArmyGroupGarrisonRequest
            {
                ArmyGroup = armyGroup,
                City = SystemAPI.GetSingleton<SubGameStatusData>().City,
                IfGarrisonIn = true
            });
            FrameDelayInvoker.Instance.InvokeAfterFrames(1, () => UpdateStaticData(false));
        }


        private void UpdateStaticData(bool shouldRecalculateAvgUnitData = true)
        {
            var infos = new List<ArmyGroupManageInfo>();
            var subGameStatusData = SystemAPI.GetSingleton<SubGameStatusData>();
            var cityAttr = SystemAPI.GetComponent<CityAttr>(subGameStatusData.City);
            var garrisonEntities = SystemAPI.GetBuffer<CityGarrisonEntity>(subGameStatusData.City);
            foreach (var armyGroup in garrisonEntities)
            {
                if (!SystemAPI.HasBuffer<ArmyGroupUnit>(armyGroup.ArmyGroup)) continue;
                infos.Add(new ArmyGroupManageInfo
                {
                    ArmyGroupEntity = armyGroup.ArmyGroup,
                });
                if (shouldRecalculateAvgUnitData)
                    ArmyGroupUtils.UpdateArmyGroupInfoForCompoChanged(EntityManager, armyGroup.ArmyGroup);
            }

            ArmyGroupManageWindow.Instance.UpdateStaticData(infos, cityAttr.maxGarrisonCount);
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
                    City = SystemAPI.GetComponent<ArmyGroupInGarrison>(armyGroup).City,
                    IfGarrisonIn = false
                });
            }

            ecb.Playback(EntityManager);
            ecb.Dispose();
            UpdateStaticData(false);
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


       
    }
}