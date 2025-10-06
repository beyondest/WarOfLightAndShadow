using SparFlame.Components.General;
using SparFlame.Components.SubGameplay;
using SparFlame.Components.VFX;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;

namespace SparFlame.Systems.SubGameplay.Interact
{
    // [UpdateAfter(typeof(SightUpdateListSystem))]
    [UpdateBefore(typeof(TransformSystemGroup))]
    public partial struct DarkShieldBuffSystem : ISystem
    {
        private ComponentLookup<LocalTransform> _transformLookup;
        private ComponentLookup<DarkShieldTauntedBuff> _darkShieldReflectDamagaLookup;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<SightSystemConfig>();
            state.RequireForUpdate<GameTimeData>();
            state.RequireForUpdate<SubGamingTag>();
            state.RequireForUpdate<DarkShieldBuffGeneralConfig>();
            state.RequireForUpdate<EndSimulationEntityCommandBufferSystem.Singleton>();
            _transformLookup = state.GetComponentLookup<LocalTransform>(true);
            _darkShieldReflectDamagaLookup = state.GetComponentLookup<DarkShieldTauntedBuff>(true);
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            _darkShieldReflectDamagaLookup.Update(ref state);
            _transformLookup.Update(ref state);
            var ecb = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>()
                .CreateCommandBuffer(state.WorldUnmanaged).AsParallelWriter();
            state.Dependency = new DarkShieldTauntBuffApplyJob
            {
                ECB = ecb,
                DarkShieldReflectDamageLookup = _darkShieldReflectDamagaLookup,
                TransformLookup = _transformLookup,
                Configs = SystemAPI.GetSingletonBuffer<DarkShieldBuffConfig>(),
                Config = SystemAPI.GetSingleton<DarkShieldBuffGeneralConfig>(),
                ElapsedTime = SystemAPI.GetSingleton<GameTimeData>().ElapsedTime,
                SightConfig = SystemAPI.GetSingleton<SightSystemConfig>()
            }.ScheduleParallel(state.Dependency);

           state.Dependency = new DarkShieldTauntBuffTimerJob
            {
                DeltaTime = SystemAPI.GetSingleton<GameTimeData>().DeltaTime,
                ECB = ecb,
            }.ScheduleParallel(state.Dependency);
        }


        [BurstCompile]
        [WithNone(typeof(UnitDeadTag))]
        public partial struct DarkShieldTauntBuffApplyJob : IJobEntity
        {
            [ReadOnly] public ComponentLookup<LocalTransform> TransformLookup;
            [ReadOnly] public ComponentLookup<DarkShieldTauntedBuff> DarkShieldReflectDamageLookup;
            [ReadOnly] public DarkShieldBuffGeneralConfig Config;
            [ReadOnly] public DynamicBuffer<DarkShieldBuffConfig> Configs;
            [ReadOnly] public float ElapsedTime;
            [ReadOnly] public SightSystemConfig SightConfig;
            public EntityCommandBuffer.ParallelWriter ECB;

            private void Execute([ChunkIndexInQuery] int index, Entity selfEntity, in DarkShieldTauntBuff buff,
                in DynamicBuffer<InsightTarget> targets, in ExpData expData, in BasicStateData stateData)
            {
                if (buff.StopTime <= ElapsedTime)
                {
                    ECB.RemoveComponent<DarkShieldTauntBuff>(index, selfEntity);
                    return;
                }
                if(!(stateData.CurState == InteractState.Attacking || stateData.TargetState == InteractState.Attacking))return;
                var count = 0;
                var maxTauntCount = Configs[(int)expData.curTier - 3].maxTauntCount;
                var l = math.min(SightConfig.ValidTargetRemainedCount, targets.Length);
                for (var i = 0; i < l; i++)
                {
                    if (count >= maxTauntCount) break;
                    var target = targets[i].Entity;

                    if (!DarkShieldReflectDamageLookup.HasComponent(target)
                        || DarkShieldReflectDamageLookup.IsComponentEnabled(target)
                       )
                    {
                        // Target is already taunted by self, then skip and add count, otherwise not add count
                        if (DarkShieldReflectDamageLookup.TryGetComponent(target, out var reflectDamage) &&
                            reflectDamage.TauntedBy == selfEntity)
                        {
                            ECB.SetComponent(index, target, new DarkShieldTauntedBuff
                            {
                                TauntedBy = selfEntity,
                                TauntTime = Config.TauntedDuration
                            });
                            count++;
                        }

                        continue;
                    }

                    ECB.SetComponentEnabled<DarkShieldTauntedBuff>(index, target, true);
                    ECB.SetComponent(index, target, new DarkShieldTauntedBuff
                    {
                        TauntedBy = selfEntity,
                        TauntTime = Config.TauntedDuration
                    });
                    var shieldVfx = ECB.CreateEntity(index);
                    ECB.AddComponent<SubGameplayEntityTag>(index, shieldVfx);
                    ECB.AddComponent(index, shieldVfx, new VFXRequest
                    {
                        Filter = new VFXSubFilter
                        {
                            FactionFilterEnable = true,
                            Faction = FactionTag.Dark
                        },
                        KeepDuration = float.MaxValue,
                        SpawnPosition = TransformLookup[target].Position,
                        RequestType = VFXRequestType.Spawn,
                        VFXName = VFXName.DarkShield,
                        VFXTrackTarget = target
                    });
                    count++;
                }
            }
        }

        [BurstCompile]
        public partial struct DarkShieldTauntBuffTimerJob : IJobEntity
        {
            [ReadOnly] public float DeltaTime;
            public EntityCommandBuffer.ParallelWriter ECB;

            private void Execute([ChunkIndexInQuery] int index, ref DarkShieldTauntedBuff tauntedBuff,
                Entity selfEntity)
            {
                tauntedBuff.TauntTime -= DeltaTime;
                if (tauntedBuff.TauntTime <= 0)
                {
                    tauntedBuff.TauntTime = 0;
                    tauntedBuff.TauntedBy = Entity.Null;
                    ECB.SetComponentEnabled<DarkShieldTauntedBuff>(index, selfEntity, false);
                    var shieldVfx = ECB.CreateEntity(index);
                    ECB.AddComponent<SubGameplayEntityTag>(index, shieldVfx);
                    ECB.AddComponent(index, shieldVfx, new VFXRequest
                    {
                        RequestType = VFXRequestType.Kill,
                        VFXName = VFXName.DarkShield,
                        VFXTrackTarget = selfEntity
                    });
                }
            }
        }
    }
}