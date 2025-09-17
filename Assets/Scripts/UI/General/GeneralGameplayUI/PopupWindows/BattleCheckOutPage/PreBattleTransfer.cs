using SparFlame.Components.General;
using SparFlame.Components.MainGameplay;
using SparFlame.Core.Utils;
using SparFlame.Systems.General.BasicControl;
using SparFlame.Systems.General.Battle;
using SparFlame.UI.General;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace SparFlame.Systems.General
{
    public partial class PreBattleTransfer : SystemBase
    {
        private EntityQuery _battleCheckSightQuery;
        private bool _initialized;

        private NativeList<Entity> _playerSideArmyGroups;
        private NativeList<Entity> _enemySideArmyGroups;
        private NativeList<Entity> _notReachedPlayerSideArmyGroups;

        private NativeList<int> _playerSideLoadingPositions;
        private NativeList<int> _enemySideLoadingPositions;

        private bool _isInPreBattleStatus;
        private SubGameStatus _targetSubGameStatus;
        private Entity _city;
        private EcoType _ecoType;

        protected override void OnCreate()
        {
            RequireForUpdate<BattleTriggerConfig>();
            RequireForUpdate<BattleCheckSightData>();
            _battleCheckSightQuery = SystemAPI.QueryBuilder().WithAll<BattleCheckSightTarget>().Build();
            _playerSideArmyGroups = new NativeList<Entity>(Allocator.Persistent);
            _enemySideArmyGroups = new NativeList<Entity>(Allocator.Persistent);
            _playerSideLoadingPositions = new NativeList<int>(Allocator.Persistent);
            _enemySideLoadingPositions = new NativeList<int>(Allocator.Persistent);
            _notReachedPlayerSideArmyGroups = new NativeList<Entity>(Allocator.Persistent);
        }

        protected override void OnDestroy()
        {
            _playerSideArmyGroups.Dispose();
            _enemySideArmyGroups.Dispose();
            _playerSideLoadingPositions.Dispose();
            _enemySideLoadingPositions.Dispose();
            _notReachedPlayerSideArmyGroups.Dispose();
        }


        protected override void OnStartRunning()
        {
            if (!_initialized)
            {
                PrebattleWindow.Instance.OnEcsAssault += AssaultAndFight;
                PrebattleWindow.Instance.OnEcsStation += StationCity;
                PrebattleWindow.Instance.OnEcsBesiege += BesiegeCity;
                PrebattleWindow.Instance.OnEcsFight += AssaultAndFight;
                _initialized = true;
            }
        }

        protected override void OnUpdate()
        {
            CheckShouldShowPrebattleWindow();
        }

        private void CheckShouldShowPrebattleWindow()
        {
            var entities = _battleCheckSightQuery.ToEntityArray(Allocator.Temp);
            var entity = entities[0];
            var data = SystemAPI.GetComponent<BattleCheckSightData>(entity);
            var targets = SystemAPI.GetBuffer<BattleCheckSightTarget>(entity);
            if (targets.Length == 0 || _isInPreBattleStatus) return;

            GameController.Instance.PauseGame(true);
            _playerSideArmyGroups.Clear();
            _enemySideArmyGroups.Clear();
            _notReachedPlayerSideArmyGroups.Clear();
            _isInPreBattleStatus = true;
            _targetSubGameStatus = data.TargetSubGameStatusData.SubGameStatus;
            var playerFactionData = SystemAPI.GetSingleton<PlayerFactionData>();
            _city = data.TargetSubGameStatusData.City;
            var battlePos = SystemAPI.GetComponent<LocalTransform>(entity).Position;
            targets = SystemAPI
                .GetBuffer<BattleCheckSightTarget>(entity); // Reassign to avoid structural change invalidity
            foreach (var target in targets)
            {
                var generalAttr = SystemAPI.GetComponent<MainGameplayGeneralAttr>(target.Entity);
                var relationship =
                    FactionUtils.GetRelationship(playerFactionData.faction,
                        playerFactionData.subFaction, generalAttr.faction, generalAttr.subFaction);
                // If battle includes a city, pass, because invade fight only happens when an army group switch status
                if (generalAttr.baseTag == MainGameBaseTag.City) continue;
                // Add Reached ArmyGroups
                switch (relationship)
                {
                    case Relationship.Neutral:
                        break;
                    case Relationship.Hostile:
                        _enemySideArmyGroups.Add(target.Entity);
                        break;
                    case Relationship.Self:
                    case Relationship.Ally:
                        _playerSideArmyGroups.Add(target.Entity);
                        break;
                    default:
                        BurstSafe.UnexpectedEnum(relationship);
                        break;
                }
            }


            if (_targetSubGameStatus == SubGameStatus.PlayerSiege)
            {
                var cityFutureAttackers = SystemAPI.GetBuffer<CityFutureInvaders>(_city);
                for (int i = cityFutureAttackers.Length - 1; i >= 0; i--)
                {
                    var cityFutureAttacker = cityFutureAttackers[i];
                    // Check if this army group died on the way
                    if (!SystemAPI.HasComponent<MainGameplayGeneralAttr>(cityFutureAttacker.ArmyGroup))
                    {
                        cityFutureAttackers.RemoveAt(i);
                        continue;
                    }

                    var generalAttr = SystemAPI.GetComponent<MainGameplayGeneralAttr>(cityFutureAttacker.ArmyGroup);
                    var relationShip =
                        FactionUtils.GetRelationship(playerFactionData.faction,
                            playerFactionData.subFaction, generalAttr.faction,
                            generalAttr.subFaction);
                    if (relationShip == Relationship.Self &&
                        !NativeContainerUtils.ContainsEq(_playerSideArmyGroups, cityFutureAttacker.ArmyGroup))
                    {
                        _notReachedPlayerSideArmyGroups.Add(cityFutureAttacker.ArmyGroup);
                    }
                }
            }

            if (_targetSubGameStatus == SubGameStatus.PlayerDefend)
            {
                EnemyAICheckShouldAssaultOrNot();
            }

            var connectTo = SystemAPI.GetComponent<BattleCheckSightData>(entity);
            EntityManager.DestroyEntity(connectTo.Value);
            EntityManager.DestroyEntity(entity);

            CalculateLoadingPositions();

            _ecoType = EcoMaskMapper.Instance.GetEcoAtPosition(battlePos);
            PrebattleWindow.Instance.Show();
            PrebattleWindow.Instance.UpdatePrebattleWindow(_targetSubGameStatus, _city, _playerSideArmyGroups,
                _enemySideArmyGroups, _notReachedPlayerSideArmyGroups, _ecoType,
                _playerSideLoadingPositions,
                _enemySideLoadingPositions);
        }

        private void CalculateLoadingPositions()
        {
            LocalTransform cityTrans;
            _playerSideLoadingPositions.Clear();
            _enemySideLoadingPositions.Clear();
            // Calculate loading positions for each side, each situation
            switch (_targetSubGameStatus)
            {
                case SubGameStatus.Encounter:
                    foreach (var _ in _playerSideArmyGroups)
                    {
                        _playerSideLoadingPositions.Add(7);
                    }

                    foreach (var _ in _enemySideArmyGroups)
                    {
                        _enemySideLoadingPositions.Add(3);
                    }

                    break;
                case SubGameStatus.PlayerSiege:
                case SubGameStatus.Support:
                    cityTrans = SystemAPI.GetComponent<LocalTransform>(_city);

                    foreach (var armyGroup in _playerSideArmyGroups)
                    {
                        // A support fight will have no ally army groups garrisoned
                        var armyGroupPos = SystemAPI.GetComponent<LocalTransform>(armyGroup).Position;
                        var gridIndex = BattleUtils.GetClosestGrids(armyGroupPos,
                            cityTrans
                        );
                        _playerSideLoadingPositions.Add(gridIndex);
                    }

                    var cyclicCount = 0;
                    foreach (var _ in _enemySideArmyGroups)
                    {
                        // If player siege/support, enemy army groups auto assign to player side loading positions inner to city
                        if (_targetSubGameStatus == SubGameStatus.PlayerSiege)
                        {
                            _enemySideLoadingPositions.Add(_playerSideLoadingPositions[cyclicCount]);
                            cyclicCount++;
                            if (cyclicCount == _playerSideArmyGroups.Length)
                                cyclicCount = 0;
                        }
                    }

                    break;

                case SubGameStatus.PlayerDefend:
                    cityTrans = SystemAPI.GetComponent<LocalTransform>(_city);
                    foreach (var _ in _playerSideArmyGroups)
                    {
                        // Garrisoned army groups will remain their positions
                        _playerSideLoadingPositions.Add(-1);
                    }

                    foreach (var armyGroup in _enemySideArmyGroups)
                    {
                        var armyGroupPos = SystemAPI.GetComponent<LocalTransform>(armyGroup).Position;
                        var gridIndex = BattleUtils.GetClosestGrids(armyGroupPos,
                            cityTrans
                        );
                        _enemySideLoadingPositions.Add(gridIndex);
                    }


                    break;
                default:
                case SubGameStatus.None:
                case SubGameStatus.PlayerCity:
                    BurstSafe.UnexpectedEnum(_targetSubGameStatus);
                    break;
            }
        }

        private void EnemyAICheckShouldAssaultOrNot()
        {
        }


        private void AssaultAndFight()
        {
            var ecb = new EntityCommandBuffer(Allocator.Temp);
            var ecoEntities = SystemAPI.GetSingletonBuffer<EcoEntityData>();

            // Get map info 
            var buffer = SystemAPI.GetBuffer<LoadingGridInfo>(ecoEntities[0].EcoEntity);
            var mapInfo = new MapInfo();
            if (_targetSubGameStatus is SubGameStatus.Encounter)
            {
                foreach (var ecoEntity in ecoEntities)
                {
                    if (ecoEntity.EcoType == _ecoType)
                    {
                        buffer = SystemAPI.GetBuffer<LoadingGridInfo>(ecoEntity.EcoEntity);
                        mapInfo = SystemAPI.GetComponent<MapInfo>(ecoEntity.EcoEntity);
                        break;
                    }
                }
            }
            else
            {
                buffer = SystemAPI.GetBuffer<LoadingGridInfo>(_city);
                mapInfo = SystemAPI.GetComponent<MapInfo>(_city);
            }

            var playerSideLoadingPositions = new NativeList<float3>(Allocator.Temp);
            var enemySideLoadingPositions = new NativeList<float3>(Allocator.Temp);
            var playerSideTotalUnitCount = 0;
            var enemySideTotalUnitCount = 0;

            // Player side snapshot and loading positions

            for (var i = 0; i < _playerSideArmyGroups.Length; i++)
            {
                var armyGroup = _playerSideArmyGroups[i];
                // Save snapshot
                var statData = SystemAPI.GetComponent<ArmyGroupStatData>(armyGroup);
                var unitCount = SystemAPI.GetBuffer<ArmyGroupUnit>(armyGroup).Length;
                var armyGroupAttr = SystemAPI.GetComponent<ArmyGroupAttr>(armyGroup);

                playerSideTotalUnitCount += unitCount;
                ecb.AddComponent(armyGroup, new BeforeBattleArmyGroupSnapShot
                {
                    MaxHp = statData.totalMaxHp,
                    UnitCount = unitCount
                });

                // Assign loading positions
                var gridIndex = _playerSideLoadingPositions[i];
                if (gridIndex != -1) // -1 means same as last time loading position
                {
                    var info = buffer[gridIndex];
                    armyGroupAttr.loadingCenter = info.outerCenter;
                    var maxSide = math.max(armyGroupAttr.boundingBoxDelta.x, armyGroupAttr.boundingBoxDelta.y);
                    armyGroupAttr.loadingScale = maxSide == 0 ? 1 : info.outerSize / maxSide;
                    armyGroupAttr.loadingScale = math.min(1, armyGroupAttr.loadingScale);
                    SystemAPI.SetComponent(armyGroup, armyGroupAttr);
                }

                var loadingPosition = armyGroupAttr.loadingCenter;


                var alreadyHasNear = false;
                foreach (var pos in playerSideLoadingPositions)
                {
                    if (math.distancesq(loadingPosition, pos) < 10f)
                    {
                        alreadyHasNear = true;
                        break;
                    }
                }

                if (!alreadyHasNear) playerSideLoadingPositions.Add(loadingPosition);
            }

            // Set enemy side snapshot and loading positions
            for (var i = 0; i < _enemySideArmyGroups.Length; i++)
            {
                var armyGroup = _enemySideArmyGroups[i];
                var statData = SystemAPI.GetComponent<ArmyGroupStatData>(armyGroup);
                var unitCount = SystemAPI.GetBuffer<ArmyGroupUnit>(armyGroup).Length;
                var armyGroupAttr = SystemAPI.GetComponent<ArmyGroupAttr>(armyGroup);

                enemySideTotalUnitCount += unitCount;
                ecb.AddComponent(armyGroup, new BeforeBattleArmyGroupSnapShot
                {
                    MaxHp = statData.totalMaxHp,
                    UnitCount = unitCount
                });

                var gridIndex = _enemySideLoadingPositions[i];
                if (gridIndex != -1)
                {
                    var info = buffer[gridIndex];
                    armyGroupAttr.loadingCenter =
                        _targetSubGameStatus is SubGameStatus.PlayerSiege or SubGameStatus.Support
                            ? info.innerCenter
                            : info.outerCenter;
                    var loadingSize = _targetSubGameStatus is SubGameStatus.PlayerSiege or SubGameStatus.Support
                        ? info.innerSize
                        : info.outerSize;
                    var maxSide = math.max(armyGroupAttr.boundingBoxDelta.x, armyGroupAttr.boundingBoxDelta.y);
                    armyGroupAttr.loadingScale = maxSide == 0 ? 1 : loadingSize / maxSide;
                    armyGroupAttr.loadingScale = math.min(1, armyGroupAttr.loadingScale);
                    SystemAPI.SetComponent(armyGroup, armyGroupAttr);
                }

                var loadingPosition = armyGroupAttr.loadingCenter;
                var alreadyHasNear = false;
                foreach (var pos in enemySideLoadingPositions)
                {
                    if (math.distancesq(loadingPosition, pos) < 10f)
                    {
                        alreadyHasNear = true;
                        break;
                    }
                }

                if (!alreadyHasNear) enemySideLoadingPositions.Add(loadingPosition);
            }


            ecb.Playback(EntityManager);
            ecb.Dispose();

            // Create battle specified singleton
            CreateBattleSpecifiedSingletons(enemySideTotalUnitCount, playerSideTotalUnitCount, mapInfo,
                playerSideLoadingPositions, enemySideLoadingPositions);


            _isInPreBattleStatus = false;
            GameController.Instance.ResumeGame(true);
            GameController.Instance.EnterBattleScene(_city, _ecoType, _targetSubGameStatus);
        }

        private void CreateBattleSpecifiedSingletons(int enemySideTotalUnitCount, int playerSideTotalUnitCount,
            MapInfo mapInfo,
            NativeList<float3> playerSideLoadingPositions, NativeList<float3> enemySideLoadingPositions)
        {
            EntityManager.CreateSingleton(new BeforeBattleTotalSnapShot
            {
                EnemySideUnitCount = enemySideTotalUnitCount,
                PlayerSideUnitCount = playerSideTotalUnitCount,
            });

            EntityManager.CreateSingleton(new CurrentSubMapInfo
            {
                MapInfo = mapInfo
            });
            var cameraRoamingPositions = SystemAPI.GetSingletonBuffer<CameraRoamingPosition>();
            foreach (var pos in playerSideLoadingPositions)
            {
                cameraRoamingPositions.Add(new CameraRoamingPosition
                {
                    Value = pos,
                    IsEnemy = false
                });
            }

            foreach (var pos in enemySideLoadingPositions)
            {
                cameraRoamingPositions.Add(new CameraRoamingPosition
                {
                    Value = pos,
                    IsEnemy = true
                });
            }
        }

        private void StationCity()
        {
            var ecb = new EntityCommandBuffer(Allocator.Temp);
            foreach (var armyGroup in _playerSideArmyGroups)
            {
                ecb.SetComponent(armyGroup, new ArmyGroupStateData
                {
                    CurState = ArmyGroupState.Station,
                    Target = _city,
                    TargetState = ArmyGroupState.Invade
                });
            }

            ecb.Playback(EntityManager);
            ecb.Dispose();
            _isInPreBattleStatus = false;
            GameController.Instance.ResumeGame(true);
        }

        private void BesiegeCity()
        {
        }
    }
}