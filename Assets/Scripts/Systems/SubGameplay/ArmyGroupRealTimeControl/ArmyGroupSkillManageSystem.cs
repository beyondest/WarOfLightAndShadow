using System;
using SparFlame.Components.General;
using SparFlame.Components.MainGameplay;
using SparFlame.Components.SubGameplay;
using SparFlame.Core.Utils;
using SparFlame.Systems.SubGameplay;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace Systems.SubGameplay.ArmyGroupRealTimeControl
{
    public partial struct ArmyGroupSkillManageSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<ArcherSkillGeneralConfig>();
            state.RequireForUpdate<PlayerFactionData>();
            state.RequireForUpdate<ArmyGroupSkillConfig>();
            state.RequireForUpdate<GameTimeData>();
            state.RequireForUpdate<SprintBuffConfig>();
            state.RequireForUpdate<SubGamingTag>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            DealArmyGroupSkillRequest(ref state);
            new ArmyGroupSkillTimerJob
            {
                DeltaTime = SystemAPI.GetSingleton<GameTimeData>().DeltaTime,
            }.ScheduleParallel();

        }

        [BurstCompile]
        public void OnDestroy(ref SystemState state)
        {
        }

        private void DealArmyGroupSkillRequest(ref SystemState state)
        {
            var ecb = new EntityCommandBuffer(Allocator.Temp);

            var sprintConfig = SystemAPI.GetSingleton<SprintBuffConfig>();
            var skillConfig = SystemAPI.GetSingleton<ArmyGroupSkillConfig>();
            var playerFactionData = SystemAPI.GetSingleton<PlayerFactionData>();
            var archerSkillConfig = SystemAPI.GetSingleton<ArcherSkillGeneralConfig>();
            // Check sprint request
            foreach (var (request, entity) in SystemAPI.Query<RefRO<ArmyGroupSprintRequest>>().WithEntityAccess())
            {
                ecb.DestroyEntity(entity);
                var timer = SystemAPI.GetComponent<ArmyGroupSkillTimer>(request.ValueRO.ArmyGroup);
                timer.MaxSprintCoolDown = sprintConfig.sprintCoolDown;
                timer.SprintCoolDown = timer.MaxSprintCoolDown;
                SystemAPI.SetComponent(request.ValueRO.ArmyGroup, timer);
                foreach (var (inArmyGroup, bonus,unit) in SystemAPI.Query<RefRO<InArmyGroup>,
                         RefRW<InteractAbilityBonus>>().WithEntityAccess())
                {
                    if (inArmyGroup.ValueRO.BelongsTo == request.ValueRO.ArmyGroup)
                    {
                        ecb.SetComponentEnabled<SprintBuff>(unit, true);
                        ecb.SetComponent(unit, new SprintBuff
                        {
                            LastTime = sprintConfig.sprintDuration
                        });
                        bonus.ValueRW.MoveSpeedBonus += sprintConfig.sprintSpeedBonusAmount;
                    }
                }
            }

            // Check hold request
            foreach (var (request, entity) in SystemAPI.Query<RefRO<ArmyGroupHoldSwitchRequest>>().WithEntityAccess())
            {
                ecb.DestroyEntity(entity);
                if (!SystemAPI.HasComponent<ArmyGroupHoldOnTag>(request.ValueRO.ArmyGroup))
                {
                    ecb.AddComponent<ArmyGroupHoldOnTag>(request.ValueRO.ArmyGroup);
                    foreach (var (inArmyGroup, transform, unit) in SystemAPI.Query<RefRO<InArmyGroup>,
                                 RefRO<LocalTransform>>().WithEntityAccess().WithNone<HoldOnPosition>())
                    {
                        if (inArmyGroup.ValueRO.BelongsTo == request.ValueRO.ArmyGroup)
                        {
                            ecb.AddComponent(unit, new HoldOnPosition
                            {
                                Position = transform.ValueRO.Position
                            });
                        }
                    }
                }
                else
                {
                    ecb.RemoveComponent<ArmyGroupHoldOnTag>(request.ValueRO.ArmyGroup);   
                    foreach (var (inArmyGroup, transform, unit) in SystemAPI.Query<RefRO<InArmyGroup>,
                                 RefRO<LocalTransform>>().WithEntityAccess().WithAll<HoldOnPosition>())
                    {
                        if (inArmyGroup.ValueRO.BelongsTo == request.ValueRO.ArmyGroup)
                        {
                            ecb.RemoveComponent<HoldOnPosition>(unit);
                        }
                    }
                }
            }

            // Check cast skill request
            foreach (var (request, entity) in SystemAPI.Query<RefRO<ArmyGroupCastSkillRequest>>().WithEntityAccess())
            {
                ecb.DestroyEntity(entity);
                var armyGroupAttr = SystemAPI.GetComponent<ArmyGroupAttr>(request.ValueRO.ArmyGroup);
                var buffer = SystemAPI.GetBuffer<ArmyGroupUnit>(request.ValueRO.ArmyGroup);
                var generalAttr = SystemAPI.GetComponent<MainGameplayGeneralAttr>(request.ValueRO.ArmyGroup);
                var relationShipWithPlayer =
                    FactionUtils.GetRelationshipSimple(generalAttr.faction, playerFactionData.faction);
                var isEnemy = relationShipWithPlayer == Relationship.Hostile;
                if (armyGroupAttr.iconType == ArmyGroupIconType.Archer)
                {
                    if(isEnemy)continue; // Only cast archer skill for ally army group
                    if (!TryCastArcherSkill(buffer, archerSkillConfig, relationShipWithPlayer, ecb, request)) continue;
                }
                else
                {
                    var timer = SystemAPI.GetComponent<ArmyGroupSkillTimer>(request.ValueRO.ArmyGroup);
                    timer.MaxChargeCoolDown = skillConfig.maxChargeCoolDown;
                    timer.ChargeCoolDown = timer.MaxChargeCoolDown;
                    SystemAPI.SetComponent(request.ValueRO.ArmyGroup, timer);
                }

                var hintRequest = ecb.CreateEntity();
                ecb.AddComponent<SubGameplayEntityTag>(hintRequest);
                ecb.AddComponent(hintRequest, new HintRequest
                {
                    Name = armyGroupAttr.iconType switch
                    {
                        ArmyGroupIconType.Archer => isEnemy ? HintName.EnemyArcherCastSkill : HintName.AllyArcherArmyGroupCastSkill ,
                        ArmyGroupIconType.Shield => isEnemy ? HintName.EnemyShieldArmyGroupCastSkill : HintName.AllyShieldArmyGroupCastSkill,
                        ArmyGroupIconType.Cleric => isEnemy ? HintName.EnemyClericArmyGroupCastSkill : HintName.AllyClericArmyGroupCastSkill,
                        ArmyGroupIconType.DualSpear => isEnemy ? HintName.EnemyDualSpearArmyGroupCastSkill : HintName.AllyDualSpearArmyGroupCastSkill,
                        ArmyGroupIconType.Worker => isEnemy ? HintName.EnemyWorkerArmyGroupCastSkill : HintName.AllyWorkerArmyGroupCastSkill,
                        ArmyGroupIconType.SpellSword => isEnemy ? HintName.EnemySpellSwordArmyGroupCastSkill : HintName.AllySpellSwordArmyGroupCastSkill,
                        ArmyGroupIconType.GreatSword => isEnemy ? HintName.EnemyGreatSwordArmyGroupCastSkill : HintName.AllyGreatSwordArmyGroupCastSkill,
                        ArmyGroupIconType.Mage => isEnemy ? HintName.EnemyMageArmyGroupCastSkill : HintName.AllyMageArmyGroupCastSkill,
                        _ => BurstSafe.UnexpectedEnum(armyGroupAttr.iconType, HintName.None)
                    }
                });
                foreach (var unit in buffer)
                {
                    ref var stateData =ref SystemAPI.GetComponentRW<BasicStateData>(unit.Unit).ValueRW;
                    stateData.TargetState = InteractState.CastSkill;
                    StateUtils.SwitchState(ref stateData,ecb,unit.Unit);
                }
            }
            
            ecb.Playback(state.EntityManager);
            ecb.Dispose();
        }

        private static bool TryCastArcherSkill(DynamicBuffer<ArmyGroupUnit> buffer, ArcherSkillGeneralConfig archerSkillConfig,
            Relationship relationShipWithPlayer, EntityCommandBuffer ecb, RefRO<ArmyGroupCastSkillRequest> request)
        {
            var unitsCount = buffer.Length;
            var tier = unitsCount / archerSkillConfig.ArrowRainPreTierUnitCount;
            tier = math.clamp(tier, 0, (int)Tier.Tier3 - 3);
            if (tier < 1 )
            {
                if (relationShipWithPlayer is Relationship.Ally or Relationship.Self)
                {
                    var hintRequest = ecb.CreateEntity();
                    ecb.AddComponent(hintRequest, new HintRequest
                    {
                        Name = HintName.ArcherUnitCountNotEnoughToCastArrowRain
                    });
                    ecb.AddComponent<SubGameplayEntityTag>(hintRequest);
                }

                return false;
            }
            var e = ecb.CreateEntity();
            ecb.AddComponent<SubGameplayEntityTag>(e);
            if (relationShipWithPlayer is Relationship.Self or Relationship.Ally)
            {
                ecb.AddComponent(e, new PlayerArcherSkillSettingRequest
                {
                    ArrowRainTier = (Tier)(tier + 3),
                    ArmyGroup = request.ValueRO.ArmyGroup
                });
            }
            else
            {
                ecb.AddComponent<EnemyArcherSkillSettingRequest>(e);
                ecb.AddComponent(e, new EnemyArcherSkillSettingRequest
                {
                    ArrowRainTier = (Tier)(tier + 3),
                    ArmyGroup = request.ValueRO.ArmyGroup
                });
            }

            return true;
        }


        [BurstCompile]
        [WithAll(typeof(InSubGameTag))]
        public partial struct ArmyGroupSkillTimerJob : IJobEntity
        {
            public float DeltaTime;

            private void Execute(ref ArmyGroupSkillTimer timer)
            {
                timer.ChargeCoolDown = math.max(0, timer.ChargeCoolDown - DeltaTime);
                timer.SprintCoolDown = math.max(0, timer.SprintCoolDown - DeltaTime);
            }
        }
    }
}