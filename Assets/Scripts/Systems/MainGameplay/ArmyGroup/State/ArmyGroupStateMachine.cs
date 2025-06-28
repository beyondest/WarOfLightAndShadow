using SparFlame.Components.ComponentUtils;
using SparFlame.Components.General;
using SparFlame.Components.MainGameplay;
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
            state.RequireForUpdate<PlayerFactionData>();
            state.RequireForUpdate<EndSimulationEntityCommandBufferSystem.Singleton>();
            state.RequireForUpdate<MainGamingTag>();
            state.RequireForUpdate<ArmyGroupSightTarget>();
            _localTransformLookup = state.GetComponentLookup<LocalTransform>(true);
            _generalAttrLookup = state.GetComponentLookup<MainGameplayGeneralAttr>(true);
            _supportFightTagLookup = state.GetComponentLookup<SupportFightTag>(true);
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
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
                ref LastPassingByPlayerCity lastCity, ref ArmyGroupMovableData movableData,
                ref ArmyGroupStateData stateData,
                Entity selfEntity)
            {
                var generalAttr = GeneralAttrLookup[selfEntity];
                
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
                            // This should never happen
                            break;
                        case ArmyGroupState.Invade:
                            var relationship = FactionUtils.GetRelationship(PlayerFactionData, generalAttr.faction,
                                generalAttr.subFaction);
                            BattleUtils.BeginBattle(
                                relationship == Relationship.Player ? SubGameStatus.PlayerSiege : SubGameStatus.PlayerDefend,
                                selfEntity, stateData.Target, index, ECB
                            );
                            break;
                        case ArmyGroupState.Support:
                            var targetStatus = SupportFightTagLookup.HasComponent(stateData.Target)
                                ? SubGameStatus.Support
                                : SubGameStatus.PlayerSiege;

                            BattleUtils.BeginBattle(targetStatus, selfEntity, stateData.Target, index, ECB);
                            break;
                        case ArmyGroupState.Garrison:
                            var garrisonRequest = ECB.CreateEntity(index);
                            ECB.AddComponent<MainGameplayEntityTag>(index, garrisonRequest);
                            ECB.AddComponent(index, garrisonRequest, new ArmyGroupGarrisonRequest
                            {
                                City = stateData.Target,
                                ArmyGroup = selfEntity,
                                IconType = armyGroupAttr.iconType,
                                IfGarrisonIn = true
                            });
                            break;
                    }

                    stateData.CurState = ArmyGroupState.Idle;
                    stateData.Target = Entity.Null;
                }
                // Only check sight target when army group is idle or moving
                if(stateData.CurState != ArmyGroupState.Idle || stateData.CurState != ArmyGroupState.Moving)return;
                
                // Check should trigger encounter battle
                var finalTarget = Entity.Null;
                if (targets.Length > 1)
                {
                    var minDisSq = float.MaxValue;
                    for (var i = 0; i < targets.Length; i++)
                    {
                        var target = targets[i].Entity;
                        var targetGeneralAttr = GeneralAttrLookup[target];
                        var relationship = FactionUtils.GetRelationship(PlayerFactionData, targetGeneralAttr.faction,
                            targetGeneralAttr.subFaction);
                        // This cases should not trigger encounter battle
                        if (relationship is Relationship.Ally or Relationship.Player or Relationship.Neutral)
                        {
                            // Record the last passing by city
                            if(targetGeneralAttr.baseTag == MainGameBaseTag.City
                               && relationship is Relationship.Ally or Relationship.Player)
                                lastCity.City = target;
                            // Exclude same faction army group
                            continue;
                        }
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
                    BattleUtils.BeginBattle(SubGameStatus.Encounter, selfEntity,finalTarget, index, ECB);
                }
            }
        }
    }
}