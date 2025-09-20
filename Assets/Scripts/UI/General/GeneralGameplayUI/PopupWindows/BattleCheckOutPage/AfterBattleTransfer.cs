using SparFlame.Components.General;
using SparFlame.Components.MainGameplay;
using SparFlame.Components.SubGameplay;
using SparFlame.Core.Utils;
using SparFlame.Database;
using SparFlame.Systems.General.BasicControl;
using SparFlame.Systems.General.Battle;
using SparFlame.UI.General.GeneralGameplayUI.PopupWindows.BattleCheckOutPage;
using Unity.Collections;
using Unity.Entities;
using Unity.Rendering;

namespace SparFlame.UI.General
{
    [UpdateBefore(typeof(BattleEndCheckingSystem))]
    public partial class AfterBattleTransfer : SystemBase
    {
        private bool _isInAfterBattleWindow;
        private bool _initialized;
        private int _endBattleFrameCount;

        protected override void OnCreate()
        {
            RequireForUpdate<BattleEndRequest>();
            RequireForUpdate<BattleRecorder>();
            RequireForUpdate<SubGameStatusData>();
        }

        protected override void OnStartRunning()
        {
            if (!_initialized)
            {
                AfterBattleWindow.Instance.OnEcsReturn += ReturnToMainWorld;
                AfterBattleWindow.Instance.OnEcsStay += StayToCity;
                _initialized = true;
            }
        }

        protected override void OnUpdate()
        {
            if (_isInAfterBattleWindow) return;
            _endBattleFrameCount++;
            if (_endBattleFrameCount < 2) return;

            var battleEndRequest = SystemAPI.GetSingleton<BattleEndRequest>();
            var subGameStatusData = SystemAPI.GetSingleton<SubGameStatusData>();
            var recorder = SystemAPI.GetSingleton<BattleRecorder>();
            var totalSnapShot = SystemAPI.GetSingleton<BeforeBattleTotalSnapShot>();
            var essenceReward = (int)(recorder.DestroyedRewardValue + recorder.KilledRewardValue);

            var playerSideArmyGroups = SystemAPI.QueryBuilder().WithAll<BeforeBattleArmyGroupSnapShot>()
                .WithAll<PlayerTag>().Build()
                .ToEntityArray(Allocator.Temp);
            var enemySideArmyGroups = SystemAPI.QueryBuilder().WithAll<BeforeBattleArmyGroupSnapShot>().WithAll<AITag>()
                .Build()
                .ToEntityArray(Allocator.Temp);
            GameController.Instance.PauseGame(true);
            AfterBattleWindow.Instance.UpdateInfo(recorder,
                battleEndRequest, subGameStatusData, totalSnapShot,
                essenceReward, playerSideArmyGroups, enemySideArmyGroups
            );
            _isInAfterBattleWindow = true;
            _endBattleFrameCount = 0;
        }

        private void ReturnToMainWorld()
        {
            AfterBattleCheckOut(false);
            GameController.Instance.ResumeGame(true);
            GameController.Instance.BackToMainWorld(false);
            _isInAfterBattleWindow = false;
            EntityManager.DestroyEntity(SystemAPI.GetSingletonEntity<BattleEndRequest>());
        }

        private void StayToCity()
        {
            AfterBattleCheckOut(true);

            HideRetreatPortals();

            var subGameStatusData = SystemAPI.GetSingleton<SubGameStatusData>();
            subGameStatusData.SubGameStatus = SubGameStatus.PlayerCity;
            GameController.Instance.ResumeGame(true);
            GameController.Instance.SwitchSubGameStatus(subGameStatusData);
            _isInAfterBattleWindow = false;
            EntityManager.DestroyEntity(SystemAPI.GetSingletonEntity<BattleEndRequest>());
        }

        private void HideRetreatPortals()
        {
            var ecb = new EntityCommandBuffer(Allocator.Temp);
            foreach (var (_, entity) in SystemAPI.Query<RefRO<RetreatPortalTag>>().WithEntityAccess())
            {
                ecb.DestroyEntity(entity);
            }

            ecb.Playback(EntityManager);
            ecb.Dispose();
        }

        private void AfterBattleCheckOut(bool ifStayToCity)
        {
            var subGameStatusData = SystemAPI.GetSingleton<SubGameStatusData>();
            var battleEndRequest = SystemAPI.GetSingleton<BattleEndRequest>();
            var battleRecorder = SystemAPI.GetSingleton<BattleRecorder>();

            var playerFactionData = SystemAPI.GetSingleton<PlayerFactionData>();
            var enemySideSubFactions = new NativeList<int>();
            var enemySideArmyGroups = SystemAPI.QueryBuilder().WithAll<BeforeBattleArmyGroupSnapShot>().WithAll<AITag>()
                .Build()
                .ToEntityArray(Allocator.Temp);
            foreach (var armyGroup in enemySideArmyGroups)
            {
                var generalAttr = SystemAPI.GetComponent<MainGameplayGeneralAttr>(armyGroup);
                if (!enemySideSubFactions.Contains((int)generalAttr.subFaction))
                {
                    enemySideSubFactions.Add((int)generalAttr.subFaction);
                }
            }

            // Change city faction
            CheckChangeCityFaction(subGameStatusData, battleEndRequest, playerFactionData, enemySideSubFactions);

            DealWithArmyGroups(battleEndRequest.Result);

            AddRewards(battleRecorder);

            DestroyBattleSpecifiedSingletons(ifStayToCity, subGameStatusData);

            SaveLoadController.Instance.SyncSaveGame();
        }

        private void CheckChangeCityFaction(in SubGameStatusData subGameStatusData,
            in BattleEndRequest battleEndRequest,
            in PlayerFactionData playerFactionData, NativeList<int> enemySideSubFactions)
        {
            switch (subGameStatusData.SubGameStatus)
            {
                case SubGameStatus.PlayerSiege:
                    if (battleEndRequest.Result is BattleResult.PlayerWin or BattleResult.EnemyRetreat)
                    {
                        SystemAPI.SetComponent(subGameStatusData.City, new MainGameplayGeneralAttr
                        {
                            subFaction = playerFactionData.subFaction,
                            faction = playerFactionData.faction,
                            baseTag = MainGameBaseTag.City
                        });
                        ReplaceCityModel(subGameStatusData.City, playerFactionData.faction);
                        ChangeCityVolumeObstacleRequestFaction(subGameStatusData.City);
                        EntityManager.RemoveComponent<AITag>(subGameStatusData.City);
                        EntityManager.AddComponent<PlayerTag>(subGameStatusData.City);

                        // When player conquer a city, reset city resources, except for available amount
                        var resourceDatas = SystemAPI.GetBuffer<CityResourceEntry>(subGameStatusData.City);
                        for (var i = 0; i < resourceDatas.Length; i++)
                        {
                            var entry = resourceDatas[i];
                            entry.accumulatedHours = 0f;
                            entry.resourceData.amountPerHour = 0f;
                            entry.resourceData.storage = 0;
                            resourceDatas[i] = entry;
                        }
                    }

                    break;
                case SubGameStatus.PlayerDefend:

                    if (battleEndRequest.Result is BattleResult.PlayerLose or BattleResult.PlayerRetreat)
                    {
                        SystemAPI.SetComponent(subGameStatusData.City, new MainGameplayGeneralAttr
                        {
                            subFaction = (SubFactionTag)enemySideSubFactions[0],
                            faction = ~playerFactionData.faction,
                            baseTag = MainGameBaseTag.City
                        });
                        ReplaceCityModel(subGameStatusData.City, ~playerFactionData.faction);
                        ChangeCityVolumeObstacleRequestFaction(subGameStatusData.City);
                        EntityManager.RemoveComponent<PlayerTag>(subGameStatusData.City);
                        EntityManager.AddComponent<AITag>(subGameStatusData.City);

                        // Reassign resource data to enemy init resources
                        var cityAttr = SystemAPI.GetComponent<CityAttr>(subGameStatusData.City);
                        var item = DatabaseManager.CityDatabaseSo.GetItemById(cityAttr.globalId);
                        var resourceDatas = SystemAPI.GetBuffer<CityResourceEntry>(subGameStatusData.City);
                        for (var i = 0; i < resourceDatas.Length; i++)
                        {
                            var entry = resourceDatas[i];
                            entry.accumulatedHours = 0f;
                            foreach (var resourceData in item.initResources)
                            {
                                if (entry.resourceData.resourceType == resourceData.resourceType)
                                {
                                    entry.resourceData = resourceData;
                                    break;
                                }
                            }

                            resourceDatas[i] = entry;
                        }
                    }

                    break;
                case SubGameStatus.Encounter:
                    break;

                case SubGameStatus.Support:
                case SubGameStatus.None:
                case SubGameStatus.PlayerCity:
                default:
                    BurstSafe.UnexpectedEnum(subGameStatusData.SubGameStatus);
                    break;
            }
        }

        private void DestroyBattleSpecifiedSingletons(bool ifStayToCity, SubGameStatusData subGameStatusData)
        {
            EntityManager.DestroyEntity(SystemAPI.GetSingletonEntity<BeforeBattleTotalSnapShot>());
            EntityManager.DestroyEntity(SystemAPI.GetSingletonEntity<CurrentSubMapInfo>());

            var ecb = new EntityCommandBuffer(Allocator.Temp);
            foreach (var (_, entity) in SystemAPI.Query<RefRO<BeforeBattleArmyGroupSnapShot>>().WithEntityAccess())
            {
                ecb.RemoveComponent<BeforeBattleArmyGroupSnapShot>(entity);
                if (ifStayToCity && SystemAPI.HasComponent<PlayerTag>(entity))
                {
                    var garrisonToCityRequest = ecb.CreateEntity();
                    ecb.AddComponent(garrisonToCityRequest, new ArmyGroupGarrisonRequest
                    {
                        City = subGameStatusData.City,
                        ArmyGroup = entity,
                        IfGarrisonIn = true
                    });
                }
            }

            ecb.Playback(EntityManager);
            ecb.Dispose();
        }

        private void AddRewards(in BattleRecorder battleRecorder)
        {
            var essenceAmount = (int)(battleRecorder.DestroyedRewardValue + battleRecorder.KilledRewardValue);
            var resourceDatas = SystemAPI.GetSingletonBuffer<ResourceData>();
            for (int i = 0; i < resourceDatas.Length; i++)
            {
                var data = resourceDatas[i];
                if (data.resourceType == ResourceType.Essence)
                {
                    data.availableAmount += essenceAmount;
                    resourceDatas[i] = data;
                    break;
                }
            }
        }

        /// <summary>
        /// When battle ends, remove dead army groups,
        /// retreat army groups, and deal with enemy army group AI logic
        /// </summary>
        /// <param name="result"></param>
        private void DealWithArmyGroups(BattleResult result)
        {
            var subGameStatusData = SystemAPI.GetSingleton<SubGameStatusData>();
            var ecb = new EntityCommandBuffer(Allocator.Temp);
            var enemySideArmyGroups = SystemAPI.QueryBuilder().WithAll<AITag>()
                .WithAll<BeforeBattleArmyGroupSnapShot>()
                .WithAll<EnemyArmyGroupBelongsToCity>()
                .Build();
            var playerSideArmyGroups = SystemAPI.QueryBuilder().WithAll<PlayerTag>()
                .WithAll<BeforeBattleArmyGroupSnapShot>()
                .Build();


            var enemyArmyGroups = enemySideArmyGroups.ToEntityArray(Allocator.Temp);
            var enemyArmyGroupsBelongsToCity =
                enemySideArmyGroups.ToComponentDataArray<EnemyArmyGroupBelongsToCity>(Allocator.Temp);
            var playerArmyGroups = playerSideArmyGroups.ToEntityArray(Allocator.Temp);

            for (var i = 0; i < enemyArmyGroups.Length; i++)
            {
                var entity = enemyArmyGroups[i];
                // var hasEnemyRetreated = false;

                if (SystemAPI.GetBuffer<ArmyGroupUnit>(entity).Length == 0)
                {
                    ArmyGroupUtils.DestroyArmyGroup(entity, ecb, EntityManager);
                }
                else
                {
                    // Player win, enemy retreat, this should never happen because there is no enemy retreat logic
                    if (result is BattleResult.PlayerWin or BattleResult.EnemyRetreat)
                    {
                        /*hasEnemyRetreated = true;
                        var lastPassByCity = SystemAPI.GetComponent<LastPassingByCity>(entity);
                        var armyGroupGarrisonRequest = ecb.CreateEntity();
                        ecb.AddComponent(armyGroupGarrisonRequest, new ArmyGroupGarrisonRequest
                        {
                            City = lastPassByCity.City,
                            ArmyGroup = entity,
                            IfGarrisonIn = true
                        });*/
                    }
                    // Player lose or retreat, enemy army group garrison into new city and add to extra army group buffer
                    else
                    {
                        if (subGameStatusData.SubGameStatus == SubGameStatus.PlayerDefend)
                        {
                            ecb.AppendToBuffer(subGameStatusData.City, new ExtraArmyGroup
                            {
                                ArmyGroup = entity,
                            });
                            var garrisonRequest = ecb.CreateEntity();
                            ecb.AddComponent(garrisonRequest, new ArmyGroupGarrisonRequest
                            {
                                ArmyGroup = entity,
                                City = subGameStatusData.City,
                                IfGarrisonIn = true
                            });
                            ecb.AddComponent<MainGameplayEntityTag>(garrisonRequest);
                        }

                        var checkFocusPlayerRequest = ecb.CreateEntity();
                        ecb.AddComponent<MainGameplayEntityTag>(checkFocusPlayerRequest);
                        ecb.AddComponent(checkFocusPlayerRequest, new CheckFocusPlayerRequest
                        {
                            EnemyCity = enemyArmyGroupsBelongsToCity[i].City
                        });
                    }
                }

                /*if (hasEnemyRetreated)
                {
                    var hint = ecb.CreateEntity();
                    ecb.AddComponent(hint, new HintRequest
                    {
                        Name = HintName.EnemyRetreatedArmyGroupBackToLastPassingByCity
                    });
                    ecb.AddComponent<MainGameplayEntityTag>(hint);
                }*/
            }

            foreach (var entity in playerArmyGroups)
            {
                var hasPlayerRetreated = false;
                if (SystemAPI.GetBuffer<ArmyGroupUnit>(entity).Length == 0)
                {
                    ArmyGroupUtils.DestroyArmyGroup(entity, ecb, EntityManager);
                    
                }
                else
                {
                    if (result is BattleResult.PlayerLose or BattleResult.PlayerRetreat)
                    {
                        hasPlayerRetreated = true;
                        var lastPassByCity = SystemAPI.GetComponent<LastPassingByCity>(entity);
                        var armyGroupGarrisonRequest = ecb.CreateEntity();
                        ecb.AddComponent(armyGroupGarrisonRequest, new ArmyGroupGarrisonRequest
                        {
                            City = lastPassByCity.City,
                            ArmyGroup = entity,
                            IfGarrisonIn = true
                        });
                    }
                }

                if (hasPlayerRetreated)
                {
                    var hint = ecb.CreateEntity();
                    ecb.AddComponent(hint, new HintRequest
                    {
                        Name = HintName.PlayerRetreatedArmyGroupBackToLastPassingByCity
                    });
                    ecb.AddComponent<MainGameplayEntityTag>(hint);
                }
            }

            ecb.Playback(EntityManager);
            ecb.Dispose();
        }


        private void ReplaceCityModel(Entity city, FactionTag turnIntoFaction)
        {
            var ecb = new EntityCommandBuffer(Allocator.Temp);

            var cityAttr = SystemAPI.GetComponent<CityAttr>(city);
            var children = SystemAPI.GetBuffer<LinkedEntityGroup>(city);
            var activeIndex = turnIntoFaction == FactionTag.Dark ? cityAttr.darkModelIndex : cityAttr.lightModelIndex;
            var inactiveIndex = turnIntoFaction == FactionTag.Dark ? cityAttr.lightModelIndex : cityAttr.darkModelIndex;

            ecb.AddComponent<DisableRendering>(children[inactiveIndex].Value);
            ecb.RemoveComponent<DisableRendering>(children[activeIndex].Value);

            ecb.Playback(EntityManager);
            ecb.Dispose();
        }

        private void ChangeCityVolumeObstacleRequestFaction(Entity city)
        {
            var request = SystemAPI.GetComponentRW<VolumeObstacleSpawnRequest>(city);
            request.ValueRW.RequestFromFaction = ~request.ValueRO.RequestFromFaction;
        }
    }
}