using SparFlame.Components.General;
using SparFlame.Components.SubGameplay;
using SparFlame.Components.VFX;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Transforms;

namespace SparFlame.Systems.SubGameplay.Interact
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
            state.RequireForUpdate<SubGamingTag>();
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
           state.Dependency= new LightShieldBuffJob
            {
                LightShieldUnderDefendLookup = _defenderData,
                TransformLookup = _transformLookup,
                ElapsedTime = SystemAPI.GetSingleton<GameTimeData>().ElapsedTime,
                Config = SystemAPI.GetSingleton<LightShieldBuffGeneralConfig>(),
                LightShieldBuffConfigs = SystemAPI.GetSingletonBuffer<LightShieldBuffConfig>(),
                ECB = ecbP
            }.ScheduleParallel(state.Dependency);
           state.Dependency = new LightShieldDefendBuffTimerJob
            {
                DeltaTime = SystemAPI.GetSingleton<GameTimeData>().DeltaTime,
                ECB = ecbP,
            }.ScheduleParallel(state.Dependency);
        }


        [BurstCompile]
        [WithNone(typeof(UnitDeadTag))]
        public partial struct LightShieldBuffJob : IJobEntity
        {
            public EntityCommandBuffer.ParallelWriter ECB;

            [NativeDisableParallelForRestriction]
            public ComponentLookup<LightShieldUnderDefend> LightShieldUnderDefendLookup;

            [ReadOnly] public float ElapsedTime;
            [ReadOnly] public ComponentLookup<LocalTransform> TransformLookup;
            [ReadOnly] public LightShieldBuffGeneralConfig Config;
            [ReadOnly] public DynamicBuffer<LightShieldBuffConfig> LightShieldBuffConfigs;

            private void Execute([ChunkIndexInQuery] int index,
                in DynamicBuffer<AoeTarget> targets, Entity selfEntity,
                in ExpData expData, in LightShieldBuff buff)
            {
                if (buff.StopTime <= ElapsedTime)
                {
                    ECB.RemoveComponent<AoeTarget>(index, selfEntity);
                    ECB.RemoveComponent<LightShieldBuff>(index, selfEntity);
                    return;
                }

                AddNewShieldData(targets, index, selfEntity,
                    LightShieldBuffConfigs[(int)expData.curTier - 3].maxDefendCount);
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
                                DefendTime = Config.UnderDefendDuration
                            });
                            count++; // Target is already defended by self, then skip and add count, otherwise not add count
                        }

                        continue; // Cur ally unit is dead or current unit is not defendable
                    }

                    ECB.SetComponentEnabled<LightShieldUnderDefend>(index, target.Entity, true);
                    ECB.SetComponent(index, target.Entity, new LightShieldUnderDefend
                    {
                        DefendBy = selfEntity,
                        DefendTime = Config.UnderDefendDuration
                    });
                    var shieldVfx = ECB.CreateEntity(index);
                    ECB.AddComponent<SubGameplayEntityTag>(index, shieldVfx);
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
                    ECB.AddComponent<SubGameplayEntityTag>(index, shieldVfx);
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