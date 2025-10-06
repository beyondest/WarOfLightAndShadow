using SparFlame.Components.General;
using SparFlame.Components.MainGameplay;
using SparFlame.Components.SubGameplay;
using SparFlame.Core.Utils;
using SparFlame.Database;
using SparFlame.Systems.General.BasicControl;
using SparFlame.Systems.General.BasicControl.GlobalMonos;
using SparFlame.Systems.General.Battle;
using SparFlame.Systems.General.Camera;
using SparFlame.UI.General.GeneralGameplayUI.PopupWindows.BattleCheckOutPage;
using Unity.Collections;
using Unity.Entities;
using Unity.Rendering;
using Unity.Transforms;

namespace SparFlame.UI.General
{
    // TODO : Change this system to battle system assembly
    [UpdateBefore(typeof(BattleEndCheckingSystem))]
    public partial class AfterBattleTransfer : SystemBase
    {
        private bool _isInAfterBattleWindow;
        private bool _initialized;
        private int _endBattleFrameCount;
        private bool _crystalAnimationPlayed;
        private bool _isWaitingForCrystalSwitchAnimationComplete;

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
                RoamingCameraController.Instance.OnCrystalAnimationEnd +=
                    () =>
                    {
                        _isWaitingForCrystalSwitchAnimationComplete = false;
                        _crystalAnimationPlayed = true;
                    };
                _initialized = true;
            }
        }

        protected override void OnUpdate()
        {
            if (_isInAfterBattleWindow) return;
            _endBattleFrameCount++; // Wait for units, buildings to be destroyed.
            if (_endBattleFrameCount < 2) return;

            var battleEndRequest = SystemAPI.GetSingleton<BattleEndRequest>();
            var subGameStatusData = SystemAPI.GetSingleton<SubGameStatusData>();
            var recorder = SystemAPI.GetSingleton<BattleRecorder>();
            if (!_crystalAnimationPlayed)
            {
                if (_isWaitingForCrystalSwitchAnimationComplete) return;
                if (subGameStatusData.SubGameStatus is SubGameStatus.PlayerDefend or SubGameStatus.PlayerSiege)
                {
                    var playerFaction = SystemAPI.GetSingleton<PlayerFactionData>().faction;
                    var winFaction =
                        battleEndRequest.Result is BattleResult.PlayerWin or BattleResult.EnemyRetreat
                            ? playerFaction
                            : ~playerFaction;
                    var playerWin = winFaction == playerFaction;
                    var shouldCreateCrystal =
                        (playerWin && subGameStatusData.SubGameStatus == SubGameStatus.PlayerSiege)
                        || (!playerWin && subGameStatusData.SubGameStatus ==
                            SubGameStatus.PlayerDefend);
                    if (shouldCreateCrystal)
                    {
                        var crystalPrefabs = SystemAPI.GetSingleton<
                            CrystalPrefab>();
                        var newCrystal = EntityManager.Instantiate(winFaction == FactionTag.Light
                            ? crystalPrefabs.LightCrystalPrefab
                            : crystalPrefabs.DarkCrystalPrefab);
                        EntityManager.AddComponent<SubGameplayEntityTag>(newCrystal);
                        RoamingCameraController.Instance.PlayCrystalSwitchAnimation(newCrystal,
                            recorder.CrystalPosition);
                        _isWaitingForCrystalSwitchAnimationComplete = true;
                        return;
                    }
                }

                _crystalAnimationPlayed = true;
            }

            recorder.EndTime = (float)SystemAPI.Time.ElapsedTime;
            SystemAPI.SetSingleton(recorder);

            var totalSnapShot = SystemAPI.GetSingleton<BeforeBattleArmyGroupTotalSnapshot>();
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
            _isWaitingForCrystalSwitchAnimationComplete = false;
            _crystalAnimationPlayed = false;
        }

        private void ReturnToMainWorld()
        {
            AfterBattleCheckOut(false);
            // GameController.Instance.ResumeGame(true);
            _isInAfterBattleWindow = false;
            EntityManager.DestroyEntity(SystemAPI.GetSingletonEntity<BattleEndRequest>());
            CustomCoroutineRunner.Instance.StartCoroutine(GameController.Instance.SubWorldToMainWorld());
        }

        private void StayToCity()
        {
            AfterBattleCheckOut(true);
            HideRetreatPortals();
            GameController.Instance.ResumeGame(true);
            _isInAfterBattleWindow = false;
            EntityManager.DestroyEntity(SystemAPI.GetSingletonEntity<BattleEndRequest>());
            GameController.Instance.StayToCityAfterBattle();
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
            using var enemySideSubFactions = new NativeList<int>(Allocator.Persistent);
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

            CustomCoroutineRunner.Instance.StartCoroutine(
                SaveLoadController.Instance.SaveAsync(SaveType.Automatic, -1));
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
                        var changeCityFactionRequest = EntityManager.CreateEntity();
                        EntityManager.AddComponent<MainGameplayEntityTag>(changeCityFactionRequest);
                        EntityManager.AddComponent<ChangeCityFactionRequest>(changeCityFactionRequest);
                        EntityManager.SetComponentData(changeCityFactionRequest, new ChangeCityFactionRequest
                        {
                            CityEntity = subGameStatusData.City,
                        });
                        ClearCityBuffer(subGameStatusData);

                        SystemAPI.SetComponent(subGameStatusData.City, new MainGameplayGeneralAttr
                        {
                            subFaction = playerFactionData.subFaction,
                            faction = playerFactionData.faction,
                            baseTag = MainGameBaseTag.City
                        });
                        ReplaceCityModel(subGameStatusData.City, playerFactionData.faction);
                        // ChangeCityVolumeObstacleRequestFaction(subGameStatusData.City);
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
                        // ChangeCityVolumeObstacleRequestFaction(subGameStatusData.City);
                        EntityManager.RemoveComponent<PlayerTag>(subGameStatusData.City);
                        EntityManager.AddComponent<AITag>(subGameStatusData.City);

                        // Reassign resource data to enemy init resources
                        var cityAttr = SystemAPI.GetComponent<PrefabId>(subGameStatusData.City);
                        var item = DatabaseManager.CityDatabaseSo.GetItemById(cityAttr.value);
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

        private void ClearCityBuffer(SubGameStatusData subGameStatusData)
        {
            var buffer = SystemAPI.GetBuffer<ArmyGroupConjureStack>(subGameStatusData.City);
            buffer.Clear();
            var buffer2 = SystemAPI.GetBuffer<ExtraArmyGroup>(subGameStatusData.City);
            buffer2.Clear();
            var buffer3 = SystemAPI.GetBuffer<CityResourceEntry>(subGameStatusData.City);
            for (var index = 0; index < buffer3.Length; index++)
            {
                var resourceEntry = buffer3[index];
                resourceEntry.resourceData.availableAmount = 0;
                resourceEntry.resourceData.storage = 0;
                resourceEntry.resourceData.amountPerHour = 0f;
                buffer3[index] = resourceEntry;
            }

            var buffer4 = SystemAPI.GetBuffer<AttackArmyGroup>(subGameStatusData.City);
            buffer4.Clear();
            var buffer5 = SystemAPI.GetBuffer<DefendArmyGroup>(subGameStatusData.City);
            buffer5.Clear();
            var buffer6 = SystemAPI.GetBuffer<InvadingArmyGroup>(subGameStatusData.City);
            buffer6.Clear();
        }

        private void DestroyBattleSpecifiedSingletons(bool ifStayToCity, SubGameStatusData subGameStatusData)
        {
            EntityManager.DestroyEntity(SystemAPI.GetSingletonEntity<BeforeBattleArmyGroupTotalSnapshot>());
            EntityManager.DestroyEntity(SystemAPI.GetSingletonEntity<CurrentSubMapInfo>());
            EntityManager.DestroyEntity(SystemAPI.GetSingletonEntity<BattleRealStart>());
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

            if (subGameStatusData.SubGameStatus is SubGameStatus.PlayerDefend or SubGameStatus.PlayerSiege)
            {
                var removeCityFutureInvaderRequest = ecb.CreateEntity();
                ecb.AddComponent<MainGameplayEntityTag>(removeCityFutureInvaderRequest);
                ecb.AddComponent(removeCityFutureInvaderRequest, new ClearCityFutureInvadersRequest
                {
                    City = subGameStatusData.City,
                });
            }

            ecb.Playback(EntityManager);
            ecb.Dispose();
        }


        private void ReplaceCityModel(Entity city, FactionTag turnIntoFaction)
        {
            var ecb = new EntityCommandBuffer(Allocator.Temp);

            var children = SystemAPI.GetBuffer<Child>(city);
            var darkIndex = 0;
            var lightIndex = 0;
            for (int i = 0; i < children.Length; i++)
            {
                if (SystemAPI.HasComponent<CityDarkModelRoot>(children[i].Value))
                {
                    darkIndex = i;
                    break;
                }
            }

            for (int i = 0; i < children.Length; i++)
            {
                if (SystemAPI.HasComponent<CityLightModelRoot>(children[i].Value))
                {
                    lightIndex = i;
                    break;
                }
            }

            var activeIndex = turnIntoFaction == FactionTag.Dark ? darkIndex : lightIndex;
            var inactiveIndex = turnIntoFaction == FactionTag.Dark ? lightIndex : darkIndex;

            var inactiveModels = SystemAPI.GetBuffer<Child>(children[inactiveIndex].Value);
            var activeModels = SystemAPI.GetBuffer<Child>(children[activeIndex].Value);
            for (var i = 0; i < inactiveModels.Length; i++)
            {
                ecb.AddComponent<DisableRendering>(inactiveModels[i].Value);
            }

            for (var i = 0; i < activeModels.Length; i++)
            {
                ecb.RemoveComponent<DisableRendering>(activeModels[i].Value);
            }

            ecb.Playback(EntityManager);
            ecb.Dispose();
        }

        // private void ChangeCityVolumeObstacleRequestFaction(Entity city)
        // {
        //     var request = SystemAPI.GetComponentRW<VolumeObstacleSpawnRequest>(city);
        //     request.ValueRW.RequestFromFaction = ~request.ValueRO.RequestFromFaction;
        // }
    }
}