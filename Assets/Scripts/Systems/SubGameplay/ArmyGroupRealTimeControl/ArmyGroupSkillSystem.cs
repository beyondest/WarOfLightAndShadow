using SparFlame.Components.General;
using SparFlame.Components.MainGameplay;
using SparFlame.Components.SubGameplay;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace Systems.SubGameplay.ArmyGroupRealTimeControl
{
    public partial struct ArmyGroupSkillSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
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

            ecb.Playback(state.EntityManager);
            ecb.Dispose();
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