using SparFlame.Components.General;
using SparFlame.Components.MainGameplay;
using SparFlame.Components.SubGameplay;
using SparFlame.Core.Utils;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Transforms;

// ReSharper disable Unity.Entities.SingletonMustBeRequested

namespace SparFlame.Systems.General.Battle
{
    public partial struct BattleEndCheckingSystem : ISystem
    {
        public struct BattleEndCheckData : IComponentData
        {
            public bool IsAllResourceLoaded;
        }

        private EntityQuery _playerSideUnitQuery;
        private EntityQuery _enemySideUnitQuery;
        private EntityQuery _playerSideCrystalQuery;
        private EntityQuery _enemySideCrystalQuery;

        private EntityQuery _playerSideRetreatedUnitQuery;
        private EntityQuery _enemySideRetreatedUnitQuery;


        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<RetreatSystemConfig>();
            state.EntityManager.CreateSingleton<BattleEndCheckData>();
            state.RequireForUpdate<BattleEndCheckData>();
            state.RequireForUpdate<SubGameStatusData>();
            state.RequireForUpdate<GameStatusData>();
            _playerSideCrystalQuery = SystemAPI.QueryBuilder().WithAll<CrystalDef>().WithAll<PlayerTag>().Build();
            _enemySideCrystalQuery = SystemAPI.QueryBuilder().WithAll<CrystalDef>().WithAll<AITag>().Build();
            _playerSideUnitQuery = SystemAPI.QueryBuilder().WithAll<UnitAttr>().WithNone<UnitRetreatTag>()
                .WithAll<PlayerTag>().Build();
            _enemySideUnitQuery = SystemAPI.QueryBuilder().WithAll<UnitAttr>().WithNone<UnitRetreatTag>()
                .WithAll<AITag>().Build();
            _playerSideRetreatedUnitQuery = SystemAPI.QueryBuilder().WithAll<UnitAttr>().WithAll<UnitRetreatTag>()
                .WithAll<PlayerTag>().Build();
            _enemySideRetreatedUnitQuery = SystemAPI.QueryBuilder().WithAll<UnitAttr>().WithAll<UnitRetreatTag>()
                .WithAll<AITag>().Build();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var subGameStatusData = SystemAPI.GetSingleton<SubGameStatusData>();
            ref var data = ref SystemAPI.GetSingletonRW<BattleEndCheckData>().ValueRW;
            if (!GameStatusUtils.IsInBattle(subGameStatusData))
            {
                data.IsAllResourceLoaded = false;
                return;
            }

            var gameStatusData = SystemAPI.GetSingleton<GameStatusData>();
            if(gameStatusData.Value != GameStatus.SubGaming)return;
           
            // Check is all resource loaded
            if (!data.IsAllResourceLoaded)
            {
                switch (subGameStatusData.SubGameStatus)
                {
                    case SubGameStatus.PlayerSiege:
                        if (_enemySideCrystalQuery.IsEmpty || _playerSideUnitQuery.IsEmpty) return;
                        data.IsAllResourceLoaded = true;
                        break;
                    case SubGameStatus.PlayerDefend:
                        if (_playerSideCrystalQuery.IsEmpty || _enemySideUnitQuery.IsEmpty) return;
                        data.IsAllResourceLoaded = true;
                        break;
                    case SubGameStatus.Encounter:
                        if (_playerSideUnitQuery.IsEmpty || _enemySideUnitQuery.IsEmpty) return;
                        data.IsAllResourceLoaded = true;
                        break;
                    case SubGameStatus.Support:
                        if (_playerSideCrystalQuery.IsEmpty) return;
                        data.IsAllResourceLoaded = true;
                        break;
                    case SubGameStatus.None:
                    case SubGameStatus.PlayerCity:
                    default:
                        BurstSafe.UnexpectedEnum(subGameStatusData.SubGameStatus);
                        break;
                }
            }

            CheckShouldBattleEnd(ref state, subGameStatusData);

            // Check if player retreat
            if (SystemAPI.HasSingleton<PlayerRetreatRequest>())
            {
                state.EntityManager.DestroyEntity(SystemAPI.GetSingletonEntity<PlayerRetreatRequest>());
                KillUnitsAndBuildingsNotRetreated(ref state, true); 
                EndBattle(ref state, BattleResult.PlayerRetreat);
            }
        }

        private void CheckShouldBattleEnd(ref SystemState state, SubGameStatusData subGameStatusData)
        {
            switch (subGameStatusData.SubGameStatus)
            {
                case SubGameStatus.PlayerSiege:
                    if (_enemySideCrystalQuery.IsEmpty)
                    {
                        KillUnitsAndBuildingsNotRetreated(ref state, false); 
                        if (_enemySideRetreatedUnitQuery.IsEmpty)
                        {
                            EndBattle(ref state, BattleResult.PlayerWin);
                        }
                        else
                        {
                            EndBattle(ref state, BattleResult.EnemyRetreat);
                        }
                        break;
                    }

                    if (_playerSideUnitQuery.IsEmpty)
                    {
                        if (_playerSideRetreatedUnitQuery.IsEmpty)
                        {
                            EndBattle(ref state, BattleResult.PlayerLose);
                        }
                        else
                        {
                            EndBattle(ref state, BattleResult.PlayerRetreat);
                        }
                    }

                    break;
                case SubGameStatus.Support:
                case SubGameStatus.PlayerDefend:
                    if (_playerSideCrystalQuery.IsEmpty)
                    {
                        KillUnitsAndBuildingsNotRetreated(ref state, true);
                        if (_playerSideRetreatedUnitQuery.IsEmpty)
                        {
                            EndBattle(ref state, BattleResult.PlayerLose);
                        }
                        else
                        {
                            EndBattle(ref state, BattleResult.PlayerRetreat);
                        }

                        break;
                    }

                    if (_enemySideUnitQuery.IsEmpty)
                    {
                        if (_enemySideRetreatedUnitQuery.IsEmpty)
                        {
                            EndBattle(ref state, BattleResult.PlayerWin);
                        }
                        else
                        {
                            EndBattle(ref state, BattleResult.EnemyRetreat);
                        }
                    }

                    break;
                case SubGameStatus.Encounter:
                    if (_playerSideUnitQuery.IsEmpty)
                    {
                        if (_playerSideRetreatedUnitQuery.IsEmpty)
                        {
                            EndBattle(ref state, BattleResult.PlayerLose);
                        }
                        else
                        {
                            EndBattle(ref state, BattleResult.PlayerRetreat);
                        }

                        break;
                    }

                    if (_enemySideUnitQuery.IsEmpty)
                    {
                        if (_enemySideRetreatedUnitQuery.IsEmpty)
                        {
                            EndBattle(ref state, BattleResult.PlayerWin);
                        }
                        else
                        {
                            EndBattle(ref state, BattleResult.PlayerRetreat);
                        }
                    }

                    break;

                // This should never happen
                case SubGameStatus.None:
                case SubGameStatus.PlayerCity:
                default:
                    BurstSafe.UnexpectedEnum(subGameStatusData.SubGameStatus);
                    break;
            }
        }

        private void EndBattle(ref SystemState state,BattleResult result)
        {
            if(SystemAPI.HasSingleton<BattleEndRequest>())return;
            
            SetPlayerRetreatedUnits(ref state, result is BattleResult.EnemyRetreat or BattleResult.PlayerWin);
            ClearRetreatPortals(ref state);

            state.EntityManager.CreateSingleton(new BattleEndRequest
            {
                Result = result
            });
        }

        private void SetPlayerRetreatedUnits(ref SystemState state, bool ifWin)
        {
            var ecb = new EntityCommandBuffer(Allocator.Temp);
            var query = SystemAPI.QueryBuilder().WithAll<InSubGameTag>().WithAll<PlayerTag>().Build();
            var armyGroups = query.ToEntityArray(Allocator.Temp);
            var armyGroupToUnitCount = new NativeHashMap<Entity, int>(armyGroups.Length, Allocator.Temp);
            var armyGroupToPositionsIndex = new NativeHashMap<Entity, int>(armyGroups.Length, Allocator.Temp);
            var config = SystemAPI.GetSingleton<RetreatSystemConfig>();
            var retreatPortals = SystemAPI.QueryBuilder().WithAll<RetreatPortalTag>().WithAll<LocalTransform>().Build();
            var portalTransform = retreatPortals.ToComponentDataArray<LocalTransform>(Allocator.Temp)[0];


            foreach (var armyGroup in armyGroups)
            {
                var buffer = SystemAPI.GetBuffer<ArmyGroupUnit>(armyGroup);
                if (buffer.Length == 0) continue;
                armyGroupToUnitCount.TryAdd(armyGroup, buffer.Length);
            }

            var armyGroupToSquarePositions = ArmyGroupUtils.GenerateArmyGroupSquareFormations(armyGroupToUnitCount,
                config.retreatSquareInterval, ifWin ? portalTransform.Position : config.firstBias
            );
            foreach (var pair in armyGroupToUnitCount)
            {
                armyGroupToPositionsIndex.TryAdd(pair.Key, 0);
            }

            foreach (var ( localTransform, inArmyGroup, unit
                         ) in SystemAPI.Query< RefRW<LocalTransform>,
                             RefRO<InArmyGroup>>().WithAll<PlayerTag>().WithAll<InGarrison>()
                         .WithAll<UnitRetreatTag>().WithEntityAccess())
            {
                var index = armyGroupToPositionsIndex[inArmyGroup.ValueRO.BelongsTo];
                localTransform.ValueRW.Position = armyGroupToSquarePositions[inArmyGroup.ValueRO.BelongsTo][index];
                armyGroupToPositionsIndex[inArmyGroup.ValueRO.BelongsTo] = index + 1;
                ecb.RemoveComponent<InGarrison>(unit);
                ecb.SetComponentEnabled<GarrisonStateTag>(unit, false);
                ecb.SetComponent(unit, new BasicStateData
                {
                    TargetEntity = Entity.Null,
                    CurState = InteractState.Idle,
                    TargetState = InteractState.Idle,
                    Focus = false,
                    InteractCounter = 0
                });
            }

            armyGroups.Dispose();
            armyGroupToUnitCount.Dispose();
            armyGroupToPositionsIndex.Dispose();
            foreach (var pair in armyGroupToSquarePositions)
            {
                pair.Value.Dispose();
            }

            armyGroupToSquarePositions.Dispose();
            ecb.Playback(state.EntityManager);
            ecb.Dispose();
        }

        private void ClearRetreatPortals(ref SystemState state)
        {
            var ecb = new EntityCommandBuffer(Allocator.Temp);
            foreach (var (_,entity) in SystemAPI.Query<RefRO<RetreatPortalTag>>().WithEntityAccess())
            {
                var removeObstacleRequest = new VolumeObstacleDestroyRequest
                {
                    RequestFromFaction = FactionTag.Neutral,
                    FromEntity = entity,
                };
                var request = ecb.CreateEntity();
                ecb.AddComponent<SubGameplayEntityTag>(request);
                ecb.AddComponent(request, removeObstacleRequest);
                ecb.DestroyEntity(entity);
            }
            ecb.Playback(state.EntityManager);
        }
        
        private void KillUnitsAndBuildingsNotRetreated(ref SystemState state, bool isPlayerLose)
        {
            var ecb = new EntityCommandBuffer(Allocator.Temp);

            if (isPlayerLose)
            {
                foreach (var (_, entity) in SystemAPI.Query<RefRO<SubGameplayGeneralAttr>>().WithAll<PlayerTag>()
                             .WithNone<UnitRetreatTag>()
                             .WithEntityAccess())
                {
                    var statChangeRequest = ecb.CreateEntity();
                    ecb.AddComponent(statChangeRequest, new StatChangeRequest
                    {
                        Interactee = entity,
                        Interactor = Entity.Null,
                        Type = StatChangeType.UnNormalKill,
                        AbsAmount = 9999,
                        DamageType = DamageType.None,
                        InteractorSubGameplayGeneralAttr = default
                    });
                    ecb.AddComponent<SubGameplayEntityTag>(statChangeRequest);
                }
            }
            else
            {
                foreach (var (_, entity) in SystemAPI.Query<RefRO<SubGameplayGeneralAttr>>().WithAll<AITag>()
                             .WithNone<UnitRetreatTag>()
                             .WithEntityAccess())
                {
                    var statChangeRequest = ecb.CreateEntity();
                    ecb.AddComponent(statChangeRequest, new StatChangeRequest
                    {
                        Interactee = entity,
                        Interactor = Entity.Null,
                        Type = StatChangeType.UnNormalKill,
                        AbsAmount = 9999,
                        DamageType = DamageType.None,
                        InteractorSubGameplayGeneralAttr = default
                    });
                    ecb.AddComponent<SubGameplayEntityTag>(statChangeRequest);
                }
            }

            ecb.Playback(state.EntityManager);
            ecb.Dispose();
        }
    }
}