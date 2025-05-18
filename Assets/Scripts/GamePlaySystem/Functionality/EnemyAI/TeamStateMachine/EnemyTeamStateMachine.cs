using SparFlame.GamePlaySystem.Garrison;
using SparFlame.GamePlaySystem.General;
using SparFlame.GamePlaySystem.Interact;
using SparFlame.GamePlaySystem.Ooc;
using SparFlame.GamePlaySystem.State;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace SparFlame.GamePlaySystem.EnemyAI
{
    [UpdateAfter(typeof(EnemyTeamManageSystem))]
    [UpdateAfter(typeof(GarrisonStateMachine))]
    public partial struct EnemyTeamStateMachine : ISystem
    {
        private ComponentLookup<BasicStateData> _basicStateLookup;
        private ComponentLookup<InGarrison> _inGarrisonLookup;
        private ComponentLookup<OocTag> _oocTagLookup;
        private ComponentLookup<EnemyBasePosData> _enemyBasePosLookup;
        private ComponentLookup<LocalTransform> _localTransformLookup;
        private BufferLookup<EnemyBaseAssembleLocs> _enemyAssembleLocsLookup;


        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<EndSimulationEntityCommandBufferSystem.Singleton>();
            state.RequireForUpdate<GamingTag>();
            state.RequireForUpdate<EnemyTeamStateMachineConfig>();
            _basicStateLookup = state.GetComponentLookup<BasicStateData>(true);
            _inGarrisonLookup = state.GetComponentLookup<InGarrison>(true);
            _oocTagLookup = state.GetComponentLookup<OocTag>(true);
            _enemyBasePosLookup = state.GetComponentLookup<EnemyBasePosData>(true);
            _localTransformLookup = state.GetComponentLookup<LocalTransform>(true);
            _enemyAssembleLocsLookup = state.GetBufferLookup<EnemyBaseAssembleLocs>(true);
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var config = SystemAPI.GetSingleton<EnemyTeamStateMachineConfig>();
            var ecb = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>();
            _basicStateLookup.Update(ref state);
            _inGarrisonLookup.Update(ref state);
            _oocTagLookup.Update(ref state);
            _enemyBasePosLookup.Update(ref state);
            _localTransformLookup.Update(ref state);
            _enemyAssembleLocsLookup.Update(ref state);

            new EnemyTeamStateMachineJob
            {
                BasicStateLookup = _basicStateLookup,
                InGarrisonLookup = _inGarrisonLookup,
                OocTagLookup = _oocTagLookup,
                EnemyBasePosDataLookUp = _enemyBasePosLookup,
                LocalTransformLookup = _localTransformLookup,
                TeamTypeToAssembleLocLookup = _enemyAssembleLocsLookup,
                ECB = ecb.CreateCommandBuffer(state.WorldUnmanaged).AsParallelWriter(),
                Config = config
            }.ScheduleParallel();
        }


        [BurstCompile]
        private partial struct EnemyTeamStateMachineJob : IJobEntity
        {
            public EntityCommandBuffer.ParallelWriter ECB;

            // LookUp
            [ReadOnly] public ComponentLookup<BasicStateData> BasicStateLookup;
            [ReadOnly] public ComponentLookup<InGarrison> InGarrisonLookup;
            [ReadOnly] public ComponentLookup<OocTag> OocTagLookup;
            [ReadOnly] public ComponentLookup<EnemyBasePosData> EnemyBasePosDataLookUp;
            [ReadOnly] public ComponentLookup<LocalTransform> LocalTransformLookup;
            [ReadOnly] public BufferLookup<EnemyBaseAssembleLocs> TeamTypeToAssembleLocLookup;
            [ReadOnly] public EnemyTeamStateMachineConfig Config;

            private void Execute([ChunkIndexInQuery] int index, ref TeamData teamData, ref TeamStateData teamStateData,
                ref DynamicBuffer<TeamEntityData> teamEntities, Entity selfEntity)
            {
                // Check if base is destroyed
                if (CheckIfBaseIsDestroyed(index, teamData, teamEntities, selfEntity, out var assembleLocs))
                    return;

                if (teamEntities.Length == 0) return;

                // Check if base is under attack and this team should fall back. If so, new command is not acceptible
                if (CheckShouldGoSaveBase(teamEntities, teamData, ref teamStateData, ECB, index, selfEntity)) return;

                // Check if all units in team is idle. 
                if (!CheckIfTeamIsIdle(ref teamStateData, index, teamEntities, selfEntity))
                    return; // Garrison state machine will check idle first, so it will not cause garrison unit not auto garrison back 

                // Check if this team is in shorthand state. If so, assemble in base
                if (CheckIfTeamShortHand(index, teamData, ref teamStateData, teamEntities, selfEntity, assembleLocs))
                    return;


                // New target is already assigned
                if (teamStateData.AssignTarget)
                {
                    SetTeamUnitsMoveToTarget(index, teamEntities, teamStateData);
                    teamStateData.AssignTarget = false;
                    ECB.SetComponentEnabled<TeamNeedTargetTag>(index, selfEntity, false);
                    return;
                }

                // No new target, require for new target
                teamStateData.Focus = false;
                teamStateData.TargetEntity = Entity.Null;
                teamStateData.TargetPosition = float3.zero;
                teamStateData.CommandType = EnemyCommandType.None;
                ECB.SetComponentEnabled<TeamNeedTargetTag>(index, selfEntity, true);
            }

            private bool CheckShouldGoSaveBase(in DynamicBuffer<TeamEntityData> teamEntities, in TeamData teamData,
                ref TeamStateData teamStateData,
                EntityCommandBuffer.ParallelWriter ecb, int index, Entity selfEntity)
            {
                if (OocTagLookup.HasComponent(teamData.BelongsToBase) &&
                    teamData.TeamType is AITeamType.Harass or AITeamType.Gather or AITeamType.Defense)
                {
                    var basePos = LocalTransformLookup[teamData.BelongsToBase];
                    var targetPos =
                        basePos.TransformPoint(EnemyBasePosDataLookUp[teamData.BelongsToBase].FallBackPosBias);
                    foreach (var teamEntityData in teamEntities)
                    {
                        var unitState = BasicStateLookup[teamEntityData.Unit];
                        if (unitState.CurState == InteractState.Garrison)
                        {
                            var moveOutCommand = ecb.CreateEntity(index);
                            ecb.AddComponent(index, moveOutCommand, new GarrisonMoveOutCommand
                            {
                                BuildingEntity = InGarrisonLookup[teamEntityData.Unit].BuildingEntity,
                                MoveOutAll = true
                            });
                            ecb.AddComponent<GameplayEntityTag>(index,moveOutCommand);
                        }
                        else
                        {
                            // Not attack state unit go back to defend crystal
                            if (unitState.TargetState != InteractState.Attacking &&
                                unitState.CurState != InteractState.Attacking)
                            {
                                ecb.AddComponent(index, teamEntityData.Unit, new EnemyUnitCommandData
                                {
                                    CommandType = EnemyCommandType.March,
                                    TargetPos = targetPos,
                                    TargetEntity = Entity.Null,
                                    Focus = false
                                });
                            }
                        }
                    }

                    ECB.SetComponentEnabled<TeamNeedTargetTag>(index, selfEntity, false);
                    return true;
                }

                return false;
            }

            private bool CheckIfTeamShortHand(int index, TeamData teamData, ref TeamStateData teamStateData,
                in DynamicBuffer<TeamEntityData> teamEntities, Entity selfEntity,
                in DynamicBuffer<EnemyBaseAssembleLocs> assembleLocs)
            {
                if (!teamData.ShortHanded) return false;

                var bias = assembleLocs[(int)teamData.TeamType].TeamAssembleLocationBias;
                var targetPos = LocalTransformLookup[teamData.BelongsToBase].TransformPoint(bias);
                teamStateData.CommandType = EnemyCommandType.March;
                teamStateData.Focus = true;
                teamStateData.TargetPosition = targetPos;
                teamStateData.TargetEntity = Entity.Null;
                SetTeamUnitsMoveToTarget(index, teamEntities, teamStateData);
                ECB.SetComponentEnabled<TeamNeedTargetTag>(index, selfEntity, false);
                return true;
            }

            private bool CheckIfTeamIsIdle(ref TeamStateData teamStateData, int index,
                DynamicBuffer<TeamEntityData> teamEntities, Entity selfEntity)
            {
                var isAllIdle = true;
                for (var i = teamEntities.Length - 1; i >= 0; i--)
                {
                    var data = teamEntities[i];
                    var basicStateData =
                        BasicStateLookup
                            [data.Unit]; // As long as the state machine updates after the team manage system, this should not raise error
                    if (basicStateData.CurState != InteractState.Idle)
                    {
                        isAllIdle = false;
                        break;
                    }
                }

                if (!isAllIdle)
                {
                    teamStateData.Idle = false;
                    ECB.SetComponentEnabled<TeamNeedTargetTag>(index, selfEntity, false);
                    return false;
                }

                teamStateData.Idle = true;
                return true;
            }

            private bool CheckIfBaseIsDestroyed(int index, TeamData teamData,
                DynamicBuffer<TeamEntityData> teamEntities, Entity selfEntity,
                out DynamicBuffer<EnemyBaseAssembleLocs> assembleLocs)
            {
                if (!TeamTypeToAssembleLocLookup.TryGetBuffer(teamData.BelongsToBase, out assembleLocs))
                {
                    foreach (var data in teamEntities)
                    {
                        // UnNormal kill all units of this team
                        var statChangeRequest = new StatChangeRequest
                        {
                            Interactor = Entity.Null,
                            Interactee = data.Unit,
                            AbsAmount = 9999,
                            Type = StatChangeType.UnNormalKill,
                            InteractorGeneralAttr = default
                        };
                        var request = ECB.CreateEntity(index);
                        ECB.AddComponent(index, request, statChangeRequest);
                        ECB.AddComponent<GameplayEntityTag>(index,request);
                    }

                    ECB.DestroyEntity(index, selfEntity);
                    return true;
                }

                return false;
            }

            private void SetTeamUnitsMoveToTarget(int index, in DynamicBuffer<TeamEntityData> teamEntities,
                in TeamStateData stateData)
            {
                foreach (var data in teamEntities)
                {
                    var pos = LocalTransformLookup[data.Unit].Position;
                    if (stateData.CommandType == EnemyCommandType.March &&
                        math.distancesq(pos, stateData.TargetPosition) < Config.reachTargetToleranceDisSq)
                        continue;
                    ECB.SetComponent(index, data.Unit, new EnemyUnitCommandData
                    {
                        CommandType = stateData.CommandType,
                        TargetEntity = stateData.TargetEntity,
                        Focus = stateData.Focus,
                        TargetPos = stateData.TargetPosition
                    });
                    ECB.SetComponentEnabled<EnemyUnitCommandUpdate>(index, data.Unit, true);
                }
            }
        }
    }
}