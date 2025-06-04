using System;
using SparFlame.GamePlaySystem.General;
using SparFlame.GamePlaySystem.Interact;
using SparFlame.GamePlaySystem.Map;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Transforms;
using Random = Unity.Mathematics.Random;

namespace SparFlame.GamePlaySystem.EnemyAI
{
    [UpdateBefore(
        typeof(EnemyTeamStateMachine))] // When a team needs target, calculate its target next frame. In this way we only calculate once
    public partial struct EnemyTeamAssignTargetSystem : ISystem
    {
        // Query
        private EntityQuery _enemyBaseQuery;
        private EntityQuery _needTargetTeamsQuery;
        private EntityQuery _playerBaseQuery;

        // This is for choose target
        private NativeHashMap<Entity, NativeList<TargetLocPair>> _base2GatherTargets;
        private NativeHashMap<Entity, TargetLocPair> _base2AttackTarget;
        private NativeList<TargetLocPair> _harassTargets;
        private NativeHashMap<Entity, NativeList<DefendTarget>> _base2DefendTargets;

        // Enemy base entity to (int) value type enum to target pairs
        // This is for calculation job
        private NativeHashMap<Entity, NativeParallelMultiHashMap<int, TargetLocPair>> _base2ValueType2GatherTargets;
        private NativeHashMap<Entity, NativeParallelMultiHashMap<int, TargetLocPair>> _base2ValueType2AttackTargets;
        private NativeParallelMultiHashMap<int, TargetLocPair> _valueType2HarassTargets;

        // Look up
        private ComponentLookup<AttackAbility> _attackAbilityLookUp;
        private ComponentLookup<StatData> _statDataLookUp;
        private ComponentLookup<EnemyBasePosData> _basePosDataLookUp;
        private ComponentLookup<LocalTransform> _localTransformLookUp;
        private BufferLookup<EnemyBaseGarrisonTowerData> _garrisonTowerDataLookUp;
        private BufferLookup<TeamEntityData> _teamEntityDataLookUp;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<MapInfo>();
            state.RequireForUpdate<GeneralRandom>();
            state.RequireForUpdate<GameStatusData>();
            state.RequireForUpdate<EnemyTeamAssignTargetConfig>();
            state.RequireForUpdate<FindCrystalToBaseConfig>();
            state.RequireForUpdate<FindResourceToBaseConfig>();
            state.RequireForUpdate<FindOutSideUnitToPlayerBaseConfig>();

            _enemyBaseQuery = SystemAPI.QueryBuilder().WithAll<LocalTransform>().WithAll<EnemyBasePosData>().Build();
            _needTargetTeamsQuery = SystemAPI.QueryBuilder().WithAll<TeamNeedTargetTag>()
                .WithAllRW<TeamStateData>().WithAll<TeamData>().Build();
            _playerBaseQuery = SystemAPI.QueryBuilder().WithAll<LocalTransform>().WithAll<CoreCrystalTag>()
                .WithAll<PlayerTag>().Build();

            _attackAbilityLookUp = state.GetComponentLookup<AttackAbility>(true);
            _statDataLookUp = state.GetComponentLookup<StatData>(true);
            _garrisonTowerDataLookUp = state.GetBufferLookup<EnemyBaseGarrisonTowerData>(true);
            _teamEntityDataLookUp = state.GetBufferLookup<TeamEntityData>(true);
            _basePosDataLookUp = state.GetComponentLookup<EnemyBasePosData>(true);
            _localTransformLookUp = state.GetComponentLookup<LocalTransform>(true);

            _base2ValueType2AttackTargets =
                new NativeHashMap<Entity, NativeParallelMultiHashMap<int, TargetLocPair>>(2, Allocator.Persistent);
            _base2ValueType2GatherTargets =
                new NativeHashMap<Entity, NativeParallelMultiHashMap<int, TargetLocPair>>(2, Allocator.Persistent);
            _valueType2HarassTargets = new NativeParallelMultiHashMap<int, TargetLocPair>(3, Allocator.Persistent);
            _base2AttackTarget = new NativeHashMap<Entity, TargetLocPair>(1, Allocator.Persistent);
            _base2GatherTargets = new NativeHashMap<Entity, NativeList<TargetLocPair>>(1, Allocator.Persistent);
            _harassTargets = new NativeList<TargetLocPair>(1, Allocator.Persistent);
            _base2DefendTargets = new NativeHashMap<Entity, NativeList<DefendTarget>>(1, Allocator.Persistent);
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var gameStatusData = SystemAPI.GetSingleton<GameStatusData>();
            var dataRw = SystemAPI.GetSingletonRW<TeamAssignData>();
            ref var generalRnd = ref SystemAPI.GetSingletonRW<GeneralRandom>().ValueRW;
            if (gameStatusData.Value == GameStatus.Init)
            {
                dataRw.ValueRW.Rnd = new Random(generalRnd.Rnd.NextUInt());
                return;
            }

            if (gameStatusData.Value != GameStatus.Gaming) return;
            if (_needTargetTeamsQuery.IsEmpty || _enemyBaseQuery.IsEmpty || _playerBaseQuery.IsEmpty) return;
            // Get Config
            var config = SystemAPI.GetSingleton<EnemyTeamAssignTargetConfig>();
            var findCrystalToBase = SystemAPI.GetSingleton<FindCrystalToBaseConfig>();
            var findResourceToBase = SystemAPI.GetSingleton<FindResourceToBaseConfig>();
            var findOutSideUnitToPlayer = SystemAPI.GetSingleton<FindOutSideUnitToPlayerBaseConfig>();

            // Allocate buffer
            var teamEntities = _needTargetTeamsQuery.ToEntityArray(Allocator.TempJob);
            var teamDatas = _needTargetTeamsQuery.ToComponentDataArray<TeamData>(Allocator.TempJob);
            var enemyBaseEntities = _enemyBaseQuery.ToEntityArray(Allocator.TempJob);
            var enemyBaseTrans = _enemyBaseQuery.ToComponentDataArray<LocalTransform>(Allocator.TempJob);
            var playerBaseTrans = _playerBaseQuery.ToComponentDataArray<LocalTransform>(Allocator.TempJob);
            var jobBuffer = new NativeList<JobHandle>(Allocator.TempJob);

            // Update look up
            _attackAbilityLookUp.Update(ref state);
            _statDataLookUp.Update(ref state);
            _garrisonTowerDataLookUp.Update(ref state);
            _teamEntityDataLookUp.Update(ref state);
            _basePosDataLookUp.Update(ref state);
            _localTransformLookUp.Update(ref state);
            // Find out which team type needs target
            var needCalGather = false;
            var needCalAttack = false;
            var needCalHarass = false;
            var needCalDefend = false;
            var attackTeamCount = 0;
            var gatherTeamCount = 0;
            var harassTeamCount = 0;
            if (dataRw.ValueRW.AttackAssembleCount ==
                0) // If attack assemble count is 0, then randomly choose an assembly count
            {
                dataRw.ValueRW.AttackAssembleCount = dataRw.ValueRW.Rnd.NextInt(
                    (int)config.AttackTeamAssembleRange.lower,
                    (int)config.AttackTeamAssembleRange.upper);
            }

            foreach (var teamData in teamDatas)
            {
                switch (teamData.TeamType)
                {
                    case AITeamType.Gather:
                        needCalGather = true;
                        gatherTeamCount++;
                        break;
                    case AITeamType.Attack:
                        attackTeamCount++;
                        break;
                    case AITeamType.Defense:
                        needCalDefend = true;
                        break;
                    case AITeamType.Harass:
                        harassTeamCount++;
                        needCalHarass = true;
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }

            if (attackTeamCount >= dataRw.ValueRW.AttackAssembleCount)
            {
                needCalAttack = true;
                dataRw.ValueRW.AttackAssembleCount = 0;
            }

            // Allocate calculation buffer and begin calculation job
            for (var i = 0; i < enemyBaseTrans.Length; i++)
            {
                _base2ValueType2GatherTargets.Add(enemyBaseEntities[i],
                    new NativeParallelMultiHashMap<int, TargetLocPair>(5, Allocator.TempJob));
                _base2ValueType2AttackTargets.Add(enemyBaseEntities[i],
                    new NativeParallelMultiHashMap<int, TargetLocPair>(5, Allocator.TempJob));
                _base2DefendTargets.Add(enemyBaseEntities[i], new NativeList<DefendTarget>(5, Allocator.TempJob));
                if (needCalGather)
                {
                    jobBuffer.Add(new FindResourceToBaseJob
                    {
                        BasePos = enemyBaseTrans[i].Position,
                        Config = findResourceToBase,
                        HarvestTargets = _base2ValueType2GatherTargets[enemyBaseEntities[i]]
                    }.Schedule(state.Dependency));
                }

                if (needCalAttack)
                {
                    var job = new FindCrystalToBaseJob
                    {
                        AttackAbilityLookUp = _attackAbilityLookUp,
                        StatDataLookUp = _statDataLookUp,
                        BasePos = enemyBaseTrans[i].Position,
                        Config = findCrystalToBase,
                        AttackTargets = _base2ValueType2AttackTargets[enemyBaseEntities[i]]
                    }.Schedule(state.Dependency);
                    state.Dependency = job;
                    jobBuffer.Add(job);
                    
                }
            }

            if (needCalHarass)
            {
                var job = new FindOutsideUnitToPlayerBaseJob
                {
                    Config = findOutSideUnitToPlayer,
                    HarassTargets = _valueType2HarassTargets
                }.Schedule(state.Dependency);
                state.Dependency = job;
                jobBuffer.Add(job);
            }

            if (needCalDefend)
            {
                CalculateDefendTowerSequence(enemyBaseEntities);
            }

            foreach (var job in jobBuffer)
            {
                job.Complete();
            }

            // Enemy choose target strategy : random choose targetValueType : good/normal/bad, and add target to list.
            TargetValueType targetValueType;
            foreach (var baseEntity in enemyBaseEntities)
            {
                var attackTargets = _base2ValueType2AttackTargets[baseEntity];
                if (needCalAttack && EnemyAIUtils.ChooseTargetValueTypeRandomly(ref dataRw.ValueRW.Rnd, config,
                        attackTargets,
                        out targetValueType))
                {
                    // Attack target only needs one target for each base
                    attackTargets.TryGetFirstValue((int)targetValueType, out var targetLocPair, out _);
                    _base2AttackTarget.Add(baseEntity, targetLocPair);
                }

                var gatherTargets = _base2ValueType2GatherTargets[baseEntity];
                if (needCalGather && EnemyAIUtils.ChooseTargetValueTypeRandomly(ref dataRw.ValueRW.Rnd, config,
                        gatherTargets,
                        out targetValueType))
                {
                    // Gather target needs 
                    _base2GatherTargets.Add(baseEntity, new NativeList<TargetLocPair>(gatherTeamCount, Allocator.Temp));
                    var list = _base2GatherTargets[baseEntity];
                    GeneralUtils.GetAllValuesForKey(gatherTargets, ref list,
                        (int)targetValueType, gatherTeamCount
                    );
                }
            }
            if (needCalHarass && EnemyAIUtils.ChooseTargetValueTypeRandomly(ref dataRw.ValueRW.Rnd, config,
                    _valueType2HarassTargets, out targetValueType))
            {
                GeneralUtils.GetAllValuesForKey(_valueType2HarassTargets, ref _harassTargets,
                    (int)targetValueType, harassTeamCount);
            }


            // Now finally assign target to each team
            for (var i = 0; i < teamEntities.Length; i++)
            {
                var team = teamEntities[i];
                var teamData = teamDatas[i];
                ref var stateData = ref SystemAPI.GetComponentRW<TeamStateData>(team).ValueRW;
                switch (teamData.TeamType)
                {
                    case AITeamType.Gather:
                        if (_base2GatherTargets.Count > 0) // Have resource available to gather
                        {
                            GatherResource(teamData, ref stateData);
                        }
                        else
                        {
                            FallbackToBase(teamData, ref stateData);
                        }
                        break;
                    case AITeamType.Attack:
                        if (!needCalAttack)
                        {
                            // Attack team count not reach the assembly count, then fallback
                            FallbackToBase(teamData, ref stateData);
                        }
                        else
                        {
                            AttackCrystal(teamData, ref stateData);
                        }

                        break;
                    case AITeamType.Defense:
                        var towerList = _base2DefendTargets[teamData.BelongsToBase];
                        if (towerList.Length > 0)
                        {
                            GarrisonToDefend(towerList, team, ref stateData);
                        }
                        else
                        {
                            // Defend no tower available, choose a random pos in defense range
                            RandomDefendOnCircle(teamData, ref dataRw.ValueRW.Rnd, ref stateData);
                        }
                        break;
                    case AITeamType.Harass:
                        if (_harassTargets.Length > 0)
                        {
                            HarassTarget(ref stateData);
                        }
                        else
                        {
                            RandomMarchToPosAroundPlayerBase(ref dataRw.ValueRW.Rnd, playerBaseTrans, config,ref stateData);
                        }
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }

            // Dispose all temp buffer
            teamEntities.Dispose();
            teamDatas.Dispose();
            enemyBaseEntities.Dispose();
            enemyBaseTrans.Dispose();
            jobBuffer.Dispose();
            playerBaseTrans.Dispose();

            // Clear calculation buffer
            foreach (var pair in _base2ValueType2GatherTargets)
            {
                pair.Value.Dispose();
            }

            foreach (var pair in _base2ValueType2AttackTargets)
            {
                pair.Value.Dispose();
            }

            _base2ValueType2GatherTargets.Clear();
            _base2ValueType2AttackTargets.Clear();
            _valueType2HarassTargets.Clear();


            // Clear targets buffer
            foreach (var pair in _base2GatherTargets)
            {
                pair.Value.Dispose();
            }

            foreach (var pair in _base2DefendTargets)
            {
                pair.Value.Dispose();
            }

            _base2GatherTargets.Clear();
            _base2DefendTargets.Clear();
            _base2AttackTarget.Clear();
            _harassTargets.Clear();
        }

        private void GatherResource(TeamData teamData, ref TeamStateData stateData)
        {
            if (!_base2GatherTargets.TryGetValue(teamData.BelongsToBase,
                    out var list)) // This should never happen, because resource is valid for all enemy base
                list = _base2GatherTargets.GetValueArray(Allocator.Temp)[0];
            var targetLocPair = EnemyAIUtils.ChooseTargetAndTryRemove(list);
            stateData.AssignTarget = true;
            stateData.TargetEntity = targetLocPair.Target;
            stateData.TargetPosition = targetLocPair.Location;
            stateData.Focus = false;
            stateData.CommandType = EnemyCommandType.March;
        }

        private static void RandomMarchToPosAroundPlayerBase(ref Random rnd, NativeArray<LocalTransform> playerBaseTrans,
            in EnemyTeamAssignTargetConfig config, ref TeamStateData stateData)
        {
            var idx = rnd.NextInt(0, playerBaseTrans.Length -1);
            var pos = EnemyAIUtils.GetRandomPointOnCircle(playerBaseTrans[idx].Position,
                config.HarassRadiusToRndPlayerBase, ref rnd);
            stateData.AssignTarget = true;
            stateData.TargetEntity = Entity.Null;
            stateData.TargetPosition = pos;
            stateData.Focus = false;
            stateData.CommandType = EnemyCommandType.March;
        }

        private void HarassTarget(ref TeamStateData stateData)
        {
            var targetLocPair = EnemyAIUtils.ChooseTargetAndTryRemove(_harassTargets);
            stateData.AssignTarget = true;
            stateData.TargetEntity = targetLocPair.Target;
            stateData.TargetPosition = targetLocPair.Location;
            stateData.Focus = true;
            stateData.CommandType = EnemyCommandType.Attack; // Interact movement must focus when target is too far, or enemy AI will lose aggro
        }

        private void RandomDefendOnCircle(in TeamData teamData, ref Random rnd,
            ref TeamStateData stateData)
        {
            var marchPos = EnemyAIUtils.GetRandomPointOnCircle(_localTransformLookUp[teamData.BelongsToBase].Position,
                _basePosDataLookUp[teamData.BelongsToBase].DefenseRadius,
                ref rnd);
            var targetLocPair = new TargetLocPair
            {
                Location = marchPos,
                Target = Entity.Null
            };
            stateData.CommandType = EnemyCommandType.March;
            stateData.AssignTarget = true;
            stateData.TargetEntity = targetLocPair.Target;
            stateData.TargetPosition = targetLocPair.Location;
            stateData.Focus = false;
        }

        private void GarrisonToDefend(NativeList<DefendTarget> towerList, Entity team,
            ref TeamStateData stateData)
        {
            var bestTarget = towerList[0];
            bestTarget.AvailableCount -= _teamEntityDataLookUp[team].Length;
            if (bestTarget.AvailableCount <= 0)
            {
                towerList.RemoveAt(0);
            }
            else
            {
                towerList.Sort(new TowerAvailableCountComparer());
            }

            var targetLocPair = bestTarget.Pair;
            stateData.CommandType = EnemyCommandType.Garrison;
            stateData.AssignTarget = true;
            stateData.TargetEntity = targetLocPair.Target;
            stateData.TargetPosition = targetLocPair.Location;
            stateData.Focus = false;
        }

        private void AttackCrystal(in TeamData teamData, ref TeamStateData teamStateData)
        {
            if (!_base2AttackTarget.TryGetValue(teamData.BelongsToBase,
                    out var targetLocPair)) //This should never happen, because player base is valid target to all enemy bases
                targetLocPair = _base2AttackTarget.GetValueArray(Allocator.Temp)[0];
            teamStateData.AssignTarget = true;
            teamStateData.TargetEntity = targetLocPair.Target;
            teamStateData.TargetPosition = targetLocPair.Location;
            teamStateData.Focus = false;
            teamStateData.CommandType =
                EnemyCommandType.March; // Interact move will drop aggro when no focus and target too far
        }

        private void FallbackToBase(in TeamData teamData, ref TeamStateData teamStateData)
        {
            var baseTransform = _localTransformLookUp[teamData.BelongsToBase];

            teamStateData.AssignTarget = true;
            teamStateData.TargetEntity = Entity.Null;
            teamStateData.TargetPosition =
                baseTransform.TransformPoint(_basePosDataLookUp[teamData.BelongsToBase].FallBackPosBias);
            teamStateData.Focus = false;
            teamStateData.CommandType = EnemyCommandType.March;
        }

        private void CalculateDefendTowerSequence(NativeArray<Entity> baseEntities)
        {
            for (var i = 0; i < baseEntities.Length; i++)
            {
                var target = baseEntities[i];
                var garrisonDatas = _garrisonTowerDataLookUp[target];
                var list = _base2DefendTargets[target];
                foreach (var data in garrisonDatas)
                {
                    if(data.AvailableCount <= 0)continue;
                    list.Add(new DefendTarget
                    {
                        AvailableCount = data.AvailableCount,
                        Pair = new TargetLocPair
                        {
                            Location = _localTransformLookUp[data.Tower].Position,
                            Target =data.Tower 
                        },
                        BaseIndexInQuery = i
                    });
                }

                list.Sort(new TowerAvailableCountComparer());
            }
        }

        [BurstCompile]
        public void OnDestroy(ref SystemState state)
        {
            if (_base2ValueType2AttackTargets.IsCreated)
            {
                foreach (var pair in _base2ValueType2AttackTargets)
                {
                    pair.Value.Dispose();
                }

                _base2ValueType2AttackTargets.Dispose();
            }

            if (_base2ValueType2GatherTargets.IsCreated)
            {
                foreach (var pair in _base2ValueType2GatherTargets)
                {
                    pair.Value.Dispose();
                }

                _base2ValueType2GatherTargets.Dispose();
            }

            if (_valueType2HarassTargets.IsCreated)
                _valueType2HarassTargets.Dispose();


            if (_base2AttackTarget.IsCreated)
                _base2AttackTarget.Dispose();
            if (_base2GatherTargets.IsCreated)
            {
                foreach (var pair in _base2GatherTargets)
                {
                    pair.Value.Dispose();
                }

                _base2GatherTargets.Dispose();
            }

            if (_base2DefendTargets.IsCreated)
            {
                foreach (var pair in _base2DefendTargets)
                    pair.Value.Dispose();
                _base2DefendTargets.Dispose();
            }

            if (_harassTargets.IsCreated)
                _harassTargets.Dispose();
        }
    }
}