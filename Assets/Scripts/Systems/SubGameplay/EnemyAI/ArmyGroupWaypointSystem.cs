using SparFlame.Components.General;
using SparFlame.Components.MainGameplay;
using SparFlame.Components.SubGameplay;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

// ReSharper disable UseIndexFromEndExpression

namespace SparFlame.Systems.SubGameplay.EnemyAI
{
    public partial struct ArmyGroupWaypointSystem : ISystem
    {
        private EntityQuery _wayPointQuery;
        private ComponentLookup<BasicStateData> _basicStateData;
        private ComponentLookup<LocalTransform> _localTransformLookup;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<FormationConfig>();
            state.RequireForUpdate<GameTimeData>();
            state.RequireForUpdate<EndSimulationEntityCommandBufferSystem.Singleton>();
            state.RequireForUpdate<BattleStartRequest>();
            _wayPointQuery = SystemAPI.QueryBuilder().WithAll<SubGameplayArmyGroupWaypointData>()
                .WithAll<LocalTransform>().WithNone<ArmyGroupAttr>()
                .Build();
            _basicStateData = state.GetComponentLookup<BasicStateData>(true);
            _localTransformLookup = state.GetComponentLookup<LocalTransform>(true);
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var battleRealStart = SystemAPI.GetSingleton<BattleStartRequest>();
            if (!battleRealStart.Initialized)
            {
                InitializeForThisBattleField(ref state);
                SystemAPI.SetSingleton(new BattleStartRequest { Initialized = true });
            }

            _basicStateData.Update(ref state);
            _localTransformLookup.Update(ref state);
            state.Dependency = new SubGameplayArmyGroupStateMachine
            {
                Config = SystemAPI.GetSingleton<FormationConfig>(),
                ElapsedTime = SystemAPI.GetSingleton<GameTimeData>().ElapsedTime,
                StateLookup = _basicStateData,
                TransformLookup = _localTransformLookup,
                ECB = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>()
                    .CreateCommandBuffer(state.WorldUnmanaged).AsParallelWriter(),
            }.ScheduleParallel(state.Dependency);
        }

        private void InitializeForThisBattleField(ref SystemState state)
        {
            using var positions = _wayPointQuery.ToComponentDataArray<LocalTransform>(Allocator.Temp);
            using var entities = _wayPointQuery.ToEntityArray(Allocator.Temp);
            using var hashMap =
                new NativeHashMap<int, NativeList<SubGameplayArmyGroupWaypointData>>(10, Allocator.Temp);

            for (var i = 0; i < positions.Length; i++)
            {
                var entity = entities[i];
                var waypoint = SystemAPI.GetBuffer<SubGameplayArmyGroupWaypointData>(entity)[0];
                var position = positions[i].Position;
                var key = (int)waypoint.iconType;
                waypoint.SelfPosition = position;
                if (!hashMap.ContainsKey(key))
                {
                    hashMap.Add(key, new NativeList<SubGameplayArmyGroupWaypointData>(10, Allocator.Temp));
                    hashMap[key].Add(waypoint);
                }
                else
                {
                    hashMap[key].Add(waypoint);
                }
            }

            foreach (var pair in hashMap)
            {
                pair.Value.Sort();
            }

            using var ecb = new EntityCommandBuffer(Allocator.Temp);
            foreach (var (data, attr, entity) in SystemAPI.Query<RefRW<ArmyGroupAIData>, RefRO<ArmyGroupAttr>>()
                         .WithAll<AITag>().WithAll<InSubGameTag>().WithEntityAccess())
            {
                data.ValueRW.WaypointIndex = 0;
                data.ValueRW.StartWaitSeconds = 0;
                data.ValueRW.State = SubGameplayArmyGroupState.Idle;
                data.ValueRW.IsWaiting = false;
                data.ValueRW.IsContacted = false;
                data.ValueRW.LastFormationWaypointIndex = -1;
                ecb.SetBuffer<SubGameplayArmyGroupWaypointData>(entity);
                var list = hashMap[(int)attr.ValueRO.iconType];
                foreach (var waypointData in list)
                {
                    ecb.AppendToBuffer(entity, waypointData);
                }
            }

            ecb.Playback(state.EntityManager);
        }
    }

    [BurstCompile]
    [WithAll(typeof(AITag))]
    [WithAll(typeof(InSubGameTag))]
    public partial struct SubGameplayArmyGroupStateMachine : IJobEntity
    {
        public EntityCommandBuffer.ParallelWriter ECB;
        [ReadOnly] public FormationConfig Config;
        [ReadOnly] public float ElapsedTime;
        [ReadOnly] public ComponentLookup<BasicStateData> StateLookup;
        [ReadOnly] public ComponentLookup<LocalTransform> TransformLookup;

        private void Execute([ChunkIndexInQuery] int index,
            in DynamicBuffer<ArmyGroupUnit> units, in ArmyGroupAttr armyGroupAttr,
            in DynamicBuffer<SubGameplayArmyGroupWaypointData> waypoints,
            in ArmyGroupSkillTimer timer,
            ref ArmyGroupAIData data, Entity selfEntity)
        {
            var currentArmyGroupState = SubGameplayArmyGroupState.Idle;
            var targetPos = float3.zero;
            var attackTarget = Entity.Null;
            // var noTargetUnits = new NativeList<Entity>(Allocator.Temp);
            var idleUnitsPositions = new NativeList<float3>(Allocator.Temp);
            var idleUnits = new NativeList<Entity>(Allocator.Temp);
            foreach (var unit in units)
            {
                if (!StateLookup.TryGetComponent(unit.Unit, out var unitState))
                    continue;
                if (unitState.CurState != InteractState.Moving && unitState.CurState != InteractState.Idle)
                {
                    if (unitState.CurState == InteractState.CastSkill)
                    {
                        currentArmyGroupState = SubGameplayArmyGroupState.CastSkill;
                        break;
                    }

                    currentArmyGroupState = SubGameplayArmyGroupState.Interacting;
                    if (unitState.CurState == InteractState.Attacking)
                    {
                        attackTarget = unitState.TargetEntity;
                        targetPos = TransformLookup[unit.Unit].Position;
                        data.IsContacted = true;
                    }
                }
                else
                {
                    if (unitState.CurState == InteractState.Moving)
                        currentArmyGroupState = SubGameplayArmyGroupState.Moving;
                    if (unitState.CurState == InteractState.Idle)
                    {
                        idleUnits.Add(unit.Unit);
                        idleUnitsPositions.Add(TransformLookup[unit.Unit].Position);
                    }

                    if (unitState.TargetState == InteractState.Attacking ||
                        unitState.TargetState == InteractState.Healing)
                    {
                        data.IsContacted = true;
                    }
                    else
                    {
                    }
                }
            }

            // Do not interrupt skill casting
            if (currentArmyGroupState == SubGameplayArmyGroupState.CastSkill) return;
            // First, let no target units gather to have target units
            if (currentArmyGroupState == SubGameplayArmyGroupState.Interacting)
            {
                if (attackTarget != Entity.Null && idleUnits.Length != 0)
                {
                    UnitMoveToTarget(index, idleUnits, targetPos);
                }

                //  When at least half of units is in interact, try cast skill
                if (armyGroupAttr.iconType != ArmyGroupIconType.Archer && idleUnits.Length <= 0.5 * units.Length &&
                    timer.ChargeCoolDown <= 0)
                {
                    TryCastSkill(index, selfEntity);
                }

                return;
            }

            var outOfRange = data.WaypointIndex >= waypoints.Length;

            var currentWaypoint = outOfRange ? waypoints[waypoints.Length - 1] : waypoints[data.WaypointIndex];

            // Second, if no one in fight, and someone moving, wait for it
            if (currentArmyGroupState == SubGameplayArmyGroupState.Moving)
            {
                // UnitMoveToTarget(index, idleUnits, currentWaypoint.SelfPosition);
                return;
            }

            // Third, if contact or last waypoint, only gather to last waypoint.
            // Cause that is near the crystal position, and it is sensible to move to it in this case
            if (data.IsContacted || outOfRange)
            {
                var lastWaypoint = waypoints[waypoints.Length - 1];
                UnitMoveToTarget(index, idleUnits, lastWaypoint.SelfPosition);
                return;
            }


            // Last, no contact, not last waypoint, go through waypoints
            if (data.IsWaiting)
            {
                if (data.StartWaitSeconds + currentWaypoint.waitBeforeMovingToThisWaypoint > ElapsedTime)
                {
                    if (data.LastFormationWaypointIndex < data.WaypointIndex)
                    {
                        data.LastFormationWaypointIndex = data.WaypointIndex;
                        if (currentWaypoint.formationBeforeReachThisWaypoint)
                            TryFormation(index, idleUnits, idleUnitsPositions, currentWaypoint.shape,
                                currentWaypoint.direction);
                        return;
                    }

                    if (!data.SkillCasted && currentWaypoint.forceCastSkillBeforeReachThisWaypoint)
                    {
                        data.SkillCasted = true;
                        if (timer.ChargeCoolDown <= 0)
                            TryCastSkill(index, selfEntity);
                        return;
                    }
                }

                UnitMoveToTarget(index, idleUnits, currentWaypoint.SelfPosition);
                data.WaypointIndex++;
                data.IsWaiting = false;
            }
            else
            {
                data.IsWaiting = true;
                data.StartWaitSeconds = ElapsedTime;
            }
        }

        private void TryFormation(int index, NativeList<Entity> noTargetUnits, NativeList<float3> noTargetUnitPositions,
            FormationShape shape, float2 direction)
        {
            var center = FormationUtils.FindModeCenter(noTargetUnitPositions, Config);
            var list = FormationUtils.GenerateFormation(center, direction, shape, noTargetUnits.Length,
                shape == FormationShape.Square ? Config.squareSpacing : Config.triangleSpacing);
            for (var i = 0; i < list.Length; i++)
            {
                var pos = list[i].Position;
                var unit = noTargetUnits[i];
                ECB.SetComponent(index, unit, new AIUnitCommandData
                {
                    TargetPosition = pos,
                    Focus = true
                });
                ECB.SetComponentEnabled<FormationMovingTag>(index, unit, true);
            }
        }

        private void UnitMoveToTarget(int index, NativeList<Entity> noTargetUnits, float3 targetPos)
        {
            foreach (var unit in noTargetUnits)
            {
                ECB.SetComponent(index, unit, new AIUnitCommandData
                {
                    TargetPosition = targetPos,
                    Focus = false
                });
                ECB.SetComponentEnabled<AIUnitCommandData>(index, unit, true);
            }
        }

        private void TryCastSkill(int index, Entity selfEntity)
        {
            var castSkillRequest = ECB.CreateEntity(index);
            ECB.AddComponent<SubGameplayEntityTag>(index, castSkillRequest);
            ECB.AddComponent(index, castSkillRequest, new ArmyGroupCastSkillRequest
            {
                ArmyGroup = selfEntity,
            });
        }
    }
}