using SparFlame.GamePlaySystem.CustomParticleSystem;
using SparFlame.GamePlaySystem.General;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace SparFlame.GamePlaySystem.Interact.Cavalry
{
    public partial struct LightCavalryBuffSystem : ISystem
    {
        private ComponentLookup<LocalTransform> _transformLookup;
        private ComponentLookup<LightCavalryUnderBonus> _lightCavalryBuffLookup;
        private ComponentLookup<StatData> _statLookup;
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<GameTimeData>();
            state.RequireForUpdate<GamingTag>();
            state.RequireForUpdate<LightCavalryBuffGeneralConfig>();
            state.RequireForUpdate<EndSimulationEntityCommandBufferSystem.Singleton>();
            state.RequireForUpdate<LightCavalryBuffConfig>();
            _transformLookup = state.GetComponentLookup<LocalTransform>(true);
            _lightCavalryBuffLookup = state.GetComponentLookup<LightCavalryUnderBonus>(true);
            _statLookup = state.GetComponentLookup<StatData>(true);
        }
        

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            _lightCavalryBuffLookup.Update(ref state);
            _transformLookup.Update(ref state);
            _statLookup.Update(ref state);
            var ecbP = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>()
                .CreateCommandBuffer(state.WorldUnmanaged).AsParallelWriter();
            var config = SystemAPI.GetSingleton<LightCavalryBuffGeneralConfig>();
            var configs = SystemAPI.GetSingletonBuffer<LightCavalryBuffConfig>();
            new LightCavalryBuffJob
            {
                TransformLookup =_transformLookup,
                LightCavalryUnderBonusLookup = _lightCavalryBuffLookup,
                Config = config,
                Configs = configs,
                StatsLookup = _statLookup,
                ECB = ecbP
            }.ScheduleParallel();
            
            new LightCavalryBuffTimerJob
            {
                DeltaTime = SystemAPI.GetSingleton<GameTimeData>().DeltaTime,
                ECB = ecbP,
            }.ScheduleParallel();
        }

        [BurstCompile]
        [WithNone(typeof(UnitDeadTag))]
        [WithAll(typeof(LightCavalryBuff))]
        public partial struct LightCavalryBuffJob : IJobEntity
        {
            [ReadOnly] public ComponentLookup<LocalTransform> TransformLookup;
            [ReadOnly] public ComponentLookup<LightCavalryUnderBonus> LightCavalryUnderBonusLookup;
            [ReadOnly] public LightCavalryBuffGeneralConfig Config;
            [ReadOnly] public DynamicBuffer<LightCavalryBuffConfig> Configs;
            [ReadOnly] public ComponentLookup<StatData> StatsLookup;
            public EntityCommandBuffer.ParallelWriter ECB;

            private void Execute([ChunkIndexInQuery] int index, Entity selfEntity,
                in DynamicBuffer<AoeTarget> targets, in ExpData expData)
            {
                var count = 0;
                var maxCount = Configs[(int)expData.CurTier - 3].maxAllyCount;
                for (int i = 0; i < targets.Length; i++)
                {
                    if (count >= maxCount) break;
                    var target = targets[i].Entity;

                    if (!LightCavalryUnderBonusLookup.HasComponent(target)
                        || LightCavalryUnderBonusLookup.IsComponentEnabled(target)
                        ||!StatsLookup.TryGetComponent(target, out var statData)
                       )
                    {
                        // Target is already taunted by self, then skip and add count, otherwise not add count
                        if (LightCavalryUnderBonusLookup.TryGetComponent(target, out var buff) &&
                            buff.Provider == selfEntity)
                        {
                            ECB.SetComponent(index, target, new LightCavalryUnderBonus
                            {
                                Provider = selfEntity,
                                LastTime = Config.lastTime
                            });
                            count++;
                        }

                        continue;
                    }

                    ECB.SetComponentEnabled<LightCavalryUnderBonus>(index, target, true);
                    ECB.SetComponent(index, target, new LightCavalryUnderBonus
                    {
                        Provider = selfEntity,
                        LastTime = Config.lastTime
                    });
                    ECB.SetComponent(index, target, new InteractAbilityBonus
                    {
                        AmountBonus = Configs[(int)expData.CurTier - 3].amountBonus,
                        RangeBonus = Configs[(int)expData.CurTier - 3].rangeBonus,
                        SpeedBonus = Configs[(int)expData.CurTier - 3].speedBonus,
                        TargetsBonus = Configs[(int)expData.CurTier - 3].targetsBonus,
                        MoveSpeedBonus = Configs[(int)expData.CurTier - 3].moveSpeedBonus,
                    });
                    statData.Bonus = Configs[(int)expData.CurTier - 3].statBonus;
                    ECB.SetComponent(index, target, statData);

                    var shieldVfx = ECB.CreateEntity(index);
                    ECB.AddComponent<GameplayEntityTag>(index, shieldVfx);
                    ECB.AddComponent(index, shieldVfx, new VFXRequest
                    {
                        Filter = new VFXSubFilter
                        {
                            FactionFilterEnable = true,
                            Faction = FactionTag.Enemy
                        },
                        KeepDuration = float.MaxValue,
                        SpawnPosition = TransformLookup[target].Position,
                        RequestType = VFXRequestType.Spawn,
                        VFXName = VFXName.LightCavalryGain,
                        VFXTrackTarget = target
                    });
                    count++;
                }
            }
        }

        [BurstCompile]
        public partial struct LightCavalryBuffTimerJob : IJobEntity
        {
            [ReadOnly] public float DeltaTime;
            public EntityCommandBuffer.ParallelWriter ECB;

            private void Execute([ChunkIndexInQuery] int index, ref LightCavalryUnderBonus lightCavalryUnderBonus,
                Entity selfEntity, ref InteractAbilityBonus bonus, ref StatData statData)
            {
                lightCavalryUnderBonus.LastTime -= DeltaTime;
                if (lightCavalryUnderBonus.LastTime <= 0)
                {
                    lightCavalryUnderBonus.LastTime = 0;
                    lightCavalryUnderBonus.Provider = Entity.Null;
                    ECB.SetComponentEnabled<LightCavalryUnderBonus>(index, selfEntity, false);
                    var shieldVfx = ECB.CreateEntity(index);
                    ECB.AddComponent<GameplayEntityTag>(index, shieldVfx);
                    ECB.AddComponent(index, shieldVfx, new VFXRequest
                    {
                        RequestType = VFXRequestType.Kill,
                        VFXName = VFXName.LightCavalryGain,
                        VFXTrackTarget = selfEntity
                    });
                    bonus = new InteractAbilityBonus();
                    statData.Bonus = 0;
                    statData.CurValue = math.clamp(statData.CurValue, 0, statData.MaxValue);
                }
            }
        }
    }
}