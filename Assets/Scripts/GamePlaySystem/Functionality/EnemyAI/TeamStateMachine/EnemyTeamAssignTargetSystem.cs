using System;
using SparFlame.GamePlaySystem.General;
using SparFlame.GamePlaySystem.Interact;
using SparFlame.GamePlaySystem.Map.GamePlaySystem.Core.Map;
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

        // This is for choose target
        private NativeHashMap<Entity, NativeList<TargetLocPair>> _base2GatherTargets;
        private NativeHashMap<Entity, TargetLocPair> _base2AttackTarget;
        private NativeList<TargetLocPair> _harassTargets;

        // Enemy base entity to (int) value type enum to target pairs
        // This is for calculation job
        private NativeHashMap<Entity, NativeParallelMultiHashMap<int, TargetLocPair>> _base2ValueType2GatherTargets;
        private NativeHashMap<Entity, NativeParallelMultiHashMap<int, TargetLocPair>> _base2ValueType2AttackTargets;
        private NativeParallelMultiHashMap<int, TargetLocPair> _valueType2HarassTargets;

        // Look up
        private ComponentLookup<AttackAbility> _attackAbilityLookUp;
        private ComponentLookup<StatData> _statDataLookUp;

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

            _attackAbilityLookUp = state.GetComponentLookup<AttackAbility>(true);
            _statDataLookUp = state.GetComponentLookup<StatData>(true);

            _base2ValueType2AttackTargets =
                new NativeHashMap<Entity, NativeParallelMultiHashMap<int, TargetLocPair>>(2, Allocator.Persistent);
            _base2ValueType2GatherTargets =
                new NativeHashMap<Entity, NativeParallelMultiHashMap<int, TargetLocPair>>(2, Allocator.Persistent);
            _valueType2HarassTargets = new NativeParallelMultiHashMap<int, TargetLocPair>(3, Allocator.Persistent);
            _base2AttackTarget = new NativeHashMap<Entity, TargetLocPair>(1, Allocator.Persistent);
            _base2GatherTargets = new NativeHashMap<Entity, NativeList<TargetLocPair>>(1, Allocator.Persistent);
            _harassTargets = new NativeList<TargetLocPair>(1, Allocator.Persistent);
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var gameStatusData = SystemAPI.GetSingleton<GameStatusData>();
            var dataRw = SystemAPI.GetSingletonRW<TeamAssignData>();
            if (gameStatusData.Value == GameStatus.Init)
            {
                dataRw.ValueRW.Rnd = new Random(SystemAPI.GetSingletonRW<GeneralRandom>().ValueRW.Rnd.NextUInt());
                return;
            }
            if(gameStatusData.Value != GameStatus.Gaming)return;
            if (_needTargetTeamsQuery.IsEmpty || _enemyBaseQuery.IsEmpty) return;
            // Get Config
            var config = SystemAPI.GetSingleton<EnemyTeamAssignTargetConfig>();
            var findCrystalToBase = SystemAPI.GetSingleton<FindCrystalToBaseConfig>();
            var findResourceToBase = SystemAPI.GetSingleton<FindResourceToBaseConfig>();
            var findOutSideUnitToPlayer = SystemAPI.GetSingleton<FindOutSideUnitToPlayerBaseConfig>();

            // Allocate buffer
            var teamEntities = _needTargetTeamsQuery.ToEntityArray(Allocator.Temp);
            var teamDatas = _needTargetTeamsQuery.ToComponentDataArray<TeamData>(Allocator.Temp);
            var baseEntities = _enemyBaseQuery.ToEntityArray(Allocator.Temp);
            var baseTrans = _enemyBaseQuery.ToComponentDataArray<LocalTransform>(Allocator.Temp);
            var jobBuffer = new NativeList<JobHandle>(Allocator.Temp);

            // Update look up
            _attackAbilityLookUp.Update(ref state);
            _statDataLookUp.Update(ref state);

            // Find out which team type needs target
            var needCalGather = false;
            var needCalAttack = false;
            var needCalHarass = false;
            var attackTeamCount = 0;
            var gatherTeamCount = 0;
            var harassTeamCount = 0;
            if (dataRw.ValueRW.AttackAssembleCount ==
                0) // If attack assemble count is 0, then randomly choose an assembly count
            {
                dataRw.ValueRW.AttackAssembleCount = dataRw.ValueRW.Rnd.NextInt(
                    (int)config.attackTeamAssembleRange.lower,
                    (int)config.attackTeamAssembleRange.upper);
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
            for (var i = 0; i < baseTrans.Length; i++)
            {
                _base2ValueType2GatherTargets.Add(baseEntities[i],
                    new NativeParallelMultiHashMap<int, TargetLocPair>(5, Allocator.TempJob));
                _base2ValueType2AttackTargets.Add(baseEntities[i],
                    new NativeParallelMultiHashMap<int, TargetLocPair>(5, Allocator.TempJob));
                _base2GatherTargets.Add(baseEntities[i],
                    new NativeList<TargetLocPair>(5, Allocator.TempJob));
                if (needCalGather)
                {
                    jobBuffer.Add(new FindResourceToBaseJob
                    {
                        BasePos = baseTrans[i].Position,
                        Config = findResourceToBase,
                        HarvestTargets = _base2ValueType2GatherTargets[baseEntities[i]]
                    }.ScheduleParallel(state.Dependency));
                }

                if (needCalAttack)
                {
                    jobBuffer.Add(new FindCrystalToBaseJob
                    {
                        AttackAbilityLookUp = _attackAbilityLookUp,
                        StatDataLookUp = _statDataLookUp,
                        BasePos = baseTrans[i].Position,
                        Config = findCrystalToBase,
                        AttackTargets = _base2ValueType2AttackTargets[baseEntities[i]]
                    }.ScheduleParallel(state.Dependency));
                }
            }

            if (needCalHarass)
            {
                jobBuffer.Add(new FindOutsideUnitToPlayerBaseJob
                {
                    Config = findOutSideUnitToPlayer,
                    HarassTargets = _valueType2HarassTargets
                }.ScheduleParallel(state.Dependency));
            }

            foreach (var job in jobBuffer)
            {
                job.Complete();
            }

            // Random choose targetValueType : good/normal/bad, and add target to list.
            TargetValueType targetValueType;
            foreach (var baseEntity in baseEntities)
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
                _base2GatherTargets.Add(baseEntity, new NativeList<TargetLocPair>(gatherTeamCount, Allocator.Temp));
                if (needCalGather && EnemyAIUtils.ChooseTargetValueTypeRandomly(ref dataRw.ValueRW.Rnd, config,
                        gatherTargets,
                        out targetValueType))
                {
                    // Gather target needs 
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

            // Calculate if there is target
            var attackHasTarget = _base2AttackTarget.Count > 0;
            var gatherHasTarget = _base2GatherTargets.Count > 0;
            var harassHasTarget = _harassTargets.Length > 0;

            // Now finally assign target to each team
            for (int i = 0; i < teamEntities.Length; i++)
            {
                var team = teamEntities[i];
                var teamData = teamDatas[i];
                ref var stateData = ref SystemAPI.GetComponentRW<TeamStateData>(team).ValueRW;
                TargetLocPair targetLocPair;
                switch (teamData.TeamType)
                {
                    case AITeamType.Gather:
                        if (gatherHasTarget)
                        {
                            targetLocPair =
                                EnemyAIUtils.ChooseTargetAndTryRemove(_base2GatherTargets[teamData.BelongsToBase]);
                            stateData.AssignTarget = true;
                            stateData.TargetEntity = targetLocPair.Target;
                            stateData.TargetPosition = targetLocPair.Location;
                            stateData.Focus = false;
                            stateData.CommandType = EnemyCommandType.March;
                        }
                        else
                        {
                            // Do nothing
                        }

                        break;
                    case AITeamType.Attack:
                        if (!needCalAttack)
                        {
                            // Attack team count not reach the assembly count, then fallback
                            stateData.AssignTarget = true;
                            stateData.TargetEntity = Entity.Null;
                            var baseTransform = SystemAPI.GetComponent<LocalTransform>(teamData.BelongsToBase);
                            stateData.TargetPosition = baseTransform.TransformPoint(SystemAPI
                                .GetComponent<EnemyBasePosData>(teamData.BelongsToBase).FallBackPosBias);
                            stateData.Focus = false;
                            stateData.CommandType = EnemyCommandType.March;
                        }
                        else
                        {
                            // This should always be true
                            if (attackHasTarget)
                            {
                                if (!_base2AttackTarget.TryGetValue(teamData.BelongsToBase,
                                        out targetLocPair)) // if current base not has target then choose other base targets
                                    targetLocPair = _base2AttackTarget.GetValueArray(Allocator.Temp)[0];
                                stateData.AssignTarget = true;
                                stateData.TargetEntity = targetLocPair.Target;
                                stateData.TargetPosition = targetLocPair.Location;
                                stateData.Focus = false;
                                stateData.CommandType = EnemyCommandType.March;
                            }
                        }

                        break;
                    case AITeamType.Defense:
                        // This team assign target in state machine separately
                        break;
                    case AITeamType.Harass:
                        if (harassHasTarget)
                        {
                            targetLocPair = EnemyAIUtils.ChooseTargetAndTryRemove(_harassTargets);
                            stateData.AssignTarget = true;
                            stateData.TargetEntity = targetLocPair.Target;
                            stateData.TargetPosition = targetLocPair.Location;
                            stateData.Focus = true;
                            stateData.CommandType = EnemyCommandType.Attack;
                        }
                        else
                        {
                            var pos = EnemyAIUtils.GetRandomPointOnCircle(SystemAPI.GetSingleton<MapInfo>().WorldCenter,
                                config.harassRadiusToWorldCenter, ref dataRw.ValueRW.Rnd);
                            stateData.AssignTarget = true;
                            stateData.TargetEntity = Entity.Null;
                            stateData.TargetPosition = pos;
                            stateData.Focus = false;
                            stateData.CommandType = EnemyCommandType.March;
                        }

                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }


            // Dispose all temp buffer
            teamEntities.Dispose();
            teamDatas.Dispose();
            baseEntities.Dispose();
            baseTrans.Dispose();
            jobBuffer.Dispose();

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

            _base2GatherTargets.Clear();
            _base2AttackTarget.Clear();
            _harassTargets.Clear();
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

            if (_harassTargets.IsCreated)
                _harassTargets.Dispose();
        }
    }
}