using SparFlame.Components.General;
using SparFlame.Components.MainGameplay;
using SparFlame.Core.Utils;
using SparFlame.Systems.General.Battle;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace SparFlame.Systems.MainGameplay.ArmyGroup
{
    public partial struct ArmyGroupStateMachine : ISystem
    {
        private ComponentLookup<LocalTransform> _localTransformLookup;

        private ComponentLookup<MainGameplayGeneralAttr> _generalAttrLookup;
        private ComponentLookup<SupportFightTag> _supportFightTagLookup;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<GameStatusData>();
            state.RequireForUpdate<PlayerFactionData>();
            state.RequireForUpdate<EndSimulationEntityCommandBufferSystem.Singleton>();
            state.RequireForUpdate<ArmyGroupSightTarget>();
            _localTransformLookup = state.GetComponentLookup<LocalTransform>(true);
            _generalAttrLookup = state.GetComponentLookup<MainGameplayGeneralAttr>(true);
            _supportFightTagLookup = state.GetComponentLookup<SupportFightTag>(true);
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var gameStatusData = SystemAPI.GetSingleton<GameStatusData>();
            if (gameStatusData.Value != GameStatus.MainGaming && gameStatusData.Value != GameStatus.SubGaming) return;
            _localTransformLookup.Update(ref state);
            _generalAttrLookup.Update(ref state);
            _supportFightTagLookup.Update(ref state);
            new ArmyGroupStateMachineJob
            {
                TransformLookup = _localTransformLookup,
                ECB = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>()
                    .CreateCommandBuffer(state.WorldUnmanaged).AsParallelWriter(),
                PlayerFactionData = SystemAPI.GetSingleton<PlayerFactionData>(),
                GeneralAttrLookup = _generalAttrLookup,
                SupportFightTagLookup = _supportFightTagLookup
            }.ScheduleParallel();
        }


        [BurstCompile]
        public partial struct ArmyGroupStateMachineJob : IJobEntity
        {
            [ReadOnly] public PlayerFactionData PlayerFactionData;
            public EntityCommandBuffer.ParallelWriter ECB;
            [ReadOnly] public ComponentLookup<LocalTransform> TransformLookup;
            [ReadOnly] public ComponentLookup<MainGameplayGeneralAttr> GeneralAttrLookup;
            [ReadOnly] public ComponentLookup<SupportFightTag> SupportFightTagLookup;

            private void Execute([ChunkIndexInQuery] int index,
                in ArmyGroupAttr armyGroupAttr,
                ref DynamicBuffer<ArmyGroupSightTarget> targets,
                ref LastPassingByCity lastCity, ref ArmyGroupMovableData movableData,
                ref ArmyGroupStateData stateData,
                Entity selfEntity)
            {
                var selfGeneralAttr = GeneralAttrLookup[selfEntity];

                // Check if movement complete
                if(CheckIfMovementComplete(index, ref movableData, ref stateData, selfEntity, selfGeneralAttr))
                    return;
                
                // Only check sight target when army group is idle or moving
                if (stateData.CurState != ArmyGroupState.Idle && stateData.CurState != ArmyGroupState.Moving) return;

                
                // Check should trigger encounter battle
                var finalTarget = Entity.Null;
                if (targets.Length > 0)
                {
                    var minDisSq = float.MaxValue;
                    for (var i = 0; i < targets.Length; i++)
                    {
                        var target = targets[i].Entity;
                        var targetGeneralAttr = GeneralAttrLookup[target];
                        var relationship = FactionUtils.GetRelationship(selfGeneralAttr.faction,
                            selfGeneralAttr.subFaction, targetGeneralAttr.faction,
                            targetGeneralAttr.subFaction);
                        // This cases should not trigger encounter battle
                        if (relationship is Relationship.Self or Relationship.Ally or Relationship.Neutral)
                        {
                            // Record the last passing by city
                            if (targetGeneralAttr.baseTag == MainGameBaseTag.City
                                && relationship == Relationship.Self)
                                lastCity.City = target;
                            // Exclude same faction army group
                            continue;
                        }

                        // If it should trigger invade battle, only triggers when army group moving complete
                        // If army group happens to nearby the city, the battle should be triggered by city state machine
                        if (targetGeneralAttr.baseTag == MainGameBaseTag.City) continue;

                        var dis = math.distancesq(TransformLookup[target].Position,
                            TransformLookup[selfEntity].Position);
                        if (dis < minDisSq)
                        {
                            minDisSq = dis;
                            finalTarget = target;
                        }
                    }
                }

                if (finalTarget != Entity.Null)
                {
                    // Army group only triggers army group
                    BattleUtils.TriggerBattle(SubGameStatus.Encounter, selfEntity, finalTarget, index, ECB);
                }
            }

            private bool CheckIfMovementComplete(int index, ref ArmyGroupMovableData movableData, ref ArmyGroupStateData stateData,
                Entity selfEntity, MainGameplayGeneralAttr selfGeneralAttr)
            {
                if (movableData.movementInfo == ArmyGroupMovementInfo.Complete)
                {
                    movableData.movementInfo = ArmyGroupMovementInfo.None;
                    stateData.CurState = stateData.TargetState;
                    stateData.TargetState = ArmyGroupState.Idle;
                    switch (stateData.CurState)
                    {
                        case ArmyGroupState.Idle:
                            break;
                        case ArmyGroupState.Moving:
                            // This should never happen, because moving complete
                            break;
                        case ArmyGroupState.Invade:
                            var relationship = FactionUtils.GetRelationship(PlayerFactionData.faction,
                                PlayerFactionData.subFaction, selfGeneralAttr.faction,
                                selfGeneralAttr.subFaction);
                            BattleUtils.TriggerBattle(
                                relationship == Relationship.Self
                                    ? SubGameStatus.PlayerSiege
                                    : SubGameStatus.PlayerDefend,
                                selfEntity, stateData.Target, index, ECB
                            );
                            break;
                        case ArmyGroupState.Support:
                            var targetStatus = SupportFightTagLookup.HasComponent(stateData.Target)
                                ? SubGameStatus.Support
                                : SubGameStatus.PlayerSiege;

                            BattleUtils.TriggerBattle(targetStatus, selfEntity, stateData.Target, index, ECB);
                            break;
                        case ArmyGroupState.Garrison:
                            var garrisonRequest = ECB.CreateEntity(index);
                            ECB.AddComponent<MainGameplayEntityTag>(index, garrisonRequest);
                            ECB.AddComponent(index, garrisonRequest, new ArmyGroupGarrisonRequest
                            {
                                City = stateData.Target,
                                ArmyGroup = selfEntity,
                                IfGarrisonIn = true
                            });
                            break;
                        case ArmyGroupState.Station:
                        default:
                            BurstSafe.UnexpectedEnum(stateData.CurState);
                            break;
                    }

                    stateData.CurState = ArmyGroupState.Idle;
                    stateData.Target = Entity.Null;
                    stateData.TargetState = ArmyGroupState.Idle;
                    return true;
                }

                return false;
            }
        }
    }
}