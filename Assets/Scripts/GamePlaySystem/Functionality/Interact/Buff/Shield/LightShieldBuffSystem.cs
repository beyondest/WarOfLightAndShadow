using SparFlame.GamePlaySystem.CustomParticleSystem;
using SparFlame.GamePlaySystem.General;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Transforms;

namespace SparFlame.GamePlaySystem.Interact
{
    public partial struct LightShieldBuffSystem : ISystem
    {
        private ComponentLookup<LightShieldUnderDefend> _defenderData;
        private ComponentLookup<LocalTransform> _transformLookup;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<LightShieldBuffGeneralConfig>();
            state.RequireForUpdate<EndSimulationEntityCommandBufferSystem.Singleton>();
            state.RequireForUpdate<GamingTag>();
            state.RequireForUpdate<GameTimeData>();
            _defenderData = state.GetComponentLookup<LightShieldUnderDefend>();
            _transformLookup = state.GetComponentLookup<LocalTransform>(true);
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            _defenderData.Update(ref state);
            _transformLookup.Update(ref state);
            var ecbP = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>()
                .CreateCommandBuffer(state.WorldUnmanaged).AsParallelWriter();
            new LightShieldBuffJob
            {
                LightShieldUnderDefendLookup = _defenderData,
                TransformLookup = _transformLookup,
                Config = SystemAPI.GetSingleton<LightShieldBuffGeneralConfig>(),
                LightShieldBuffConfigs = SystemAPI.GetSingletonBuffer<LightShieldBuffConfig>(),
                ECB = ecbP
            }.ScheduleParallel();
            new LightShieldDefendBuffTimerJob
            {
                DeltaTime = SystemAPI.GetSingleton<GameTimeData>().DeltaTime,
                ECB = ecbP,
            }.ScheduleParallel();
        }


        [BurstCompile]
        [WithAll(typeof(LightShieldBuff))]
        public partial struct LightShieldBuffJob : IJobEntity
        {
            public EntityCommandBuffer.ParallelWriter ECB;

            [NativeDisableParallelForRestriction]
            public ComponentLookup<LightShieldUnderDefend> LightShieldUnderDefendLookup;

            [ReadOnly] public ComponentLookup<LocalTransform> TransformLookup;
            [ReadOnly] public LightShieldBuffGeneralConfig Config;
            [ReadOnly] public DynamicBuffer<LightShieldBuffConfig> LightShieldBuffConfigs;

            private void Execute([ChunkIndexInQuery] int index,
                in DynamicBuffer<AoeTarget> targets, in BasicStateData stateData, Entity selfEntity,
                in ExpData expData)
            {
                if (stateData.CurState != InteractState.Attacking && stateData.TargetState != InteractState.Attacking)
                {
                    return; // General buff system will remove this buff
                }
                AddNewShieldData(targets, index, selfEntity,
                    LightShieldBuffConfigs[(int)expData.CurTier - 3].maxDefendCount);
            }

            private void AddNewShieldData(in DynamicBuffer<AoeTarget> targets, int index, Entity selfEntity,
                int maxDefendCount)
            {
                var count = 0;
                for (var i = targets.Length - 1; i >= 0; i--)
                {
                    if (count >= maxDefendCount)
                    {
                        break; // Already reached max defend count
                    }

                    var target = targets[i];
                    if (!LightShieldUnderDefendLookup.HasComponent(target.Entity)
                        || LightShieldUnderDefendLookup.IsComponentEnabled(target.Entity))
                    {
                        if (LightShieldUnderDefendLookup.TryGetComponent(target.Entity, out var defenderData) &&
                            defenderData.DefendBy == selfEntity)
                        {
                            ECB.SetComponent(index, target.Entity, new LightShieldUnderDefend
                            {
                                DefendBy = selfEntity,
                                DefendTime = Config.DefendTime
                            });
                            count++; // Target is already defended by self, then skip and add count, otherwise not add count
                        }

                        continue; // Cur ally unit is dead or current unit is not defendable
                    }

                    ECB.SetComponentEnabled<LightShieldUnderDefend>(index, target.Entity, true);
                    ECB.SetComponent(index, target.Entity, new LightShieldUnderDefend
                    {
                        DefendBy = selfEntity,
                        DefendTime = Config.DefendTime
                    });
                    var shieldVfx = ECB.CreateEntity(index);
                    ECB.AddComponent<GameplayEntityTag>(index, shieldVfx);
                    ECB.AddComponent(index, shieldVfx, new VFXRequest
                    {
                        KeepDuration = float.MaxValue,
                        SpawnPosition = TransformLookup[target.Entity].Position,
                        RequestType = VFXRequestType.Spawn,
                        VFXName = VFXName.LightShield,
                        VFXTrackTarget = target.Entity
                    });
                    count++;
                }
            }
        }

        [BurstCompile]
        public partial struct LightShieldDefendBuffTimerJob : IJobEntity
        {
            [ReadOnly] public float DeltaTime;
            public EntityCommandBuffer.ParallelWriter ECB;

            private void Execute([ChunkIndexInQuery] int index, ref LightShieldUnderDefend underDefend,
                Entity selfEntity)
            {
                underDefend.DefendTime -= DeltaTime;
                if (underDefend.DefendTime <= 0)
                {
                    underDefend.DefendTime = 0;
                    underDefend.DefendBy = Entity.Null;
                    ECB.SetComponentEnabled<LightShieldUnderDefend>(index, selfEntity, false);
                    var shieldVfx = ECB.CreateEntity(index);
                    ECB.AddComponent<GameplayEntityTag>(index, shieldVfx);
                    ECB.AddComponent(index, shieldVfx, new VFXRequest
                    {
                        RequestType = VFXRequestType.Kill,
                        VFXName = VFXName.LightShield,
                        VFXTrackTarget = selfEntity
                    });
                }
            }
        }
    }
}