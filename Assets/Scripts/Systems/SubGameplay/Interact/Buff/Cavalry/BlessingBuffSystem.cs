using SparFlame.Components.General;
using SparFlame.Components.SubGameplay;
using SparFlame.Components.VFX;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Transforms;

namespace SparFlame.Systems.SubGameplay.Interact.Cavalry
{
    public partial struct BlessingBuffSystem : ISystem
    {
        private ComponentLookup<LocalTransform> _transformLookup;

        private ComponentLookup<UnderBlessingBonus> _lightCavalryBuffLookup;

        // private ComponentLookup<StatData> _statLookup;
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<GameTimeData>();
            state.RequireForUpdate<SubGamingTag>();
            state.RequireForUpdate<BlessingBuffGeneralConfig>();
            state.RequireForUpdate<EndSimulationEntityCommandBufferSystem.Singleton>();
            state.RequireForUpdate<BlessingBuffConfig>();
            _transformLookup = state.GetComponentLookup<LocalTransform>(true);
            _lightCavalryBuffLookup = state.GetComponentLookup<UnderBlessingBonus>(true);
        }


        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            _lightCavalryBuffLookup.Update(ref state);
            _transformLookup.Update(ref state);
            // _statLookup.Update(ref state);
            var ecbP = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>()
                .CreateCommandBuffer(state.WorldUnmanaged).AsParallelWriter();
            var config = SystemAPI.GetSingleton<BlessingBuffGeneralConfig>();
            var configs = SystemAPI.GetSingletonBuffer<BlessingBuffConfig>();
            new BlessingBuffJob
            {
                TransformLookup = _transformLookup,
                LightCavalryUnderBonusLookup = _lightCavalryBuffLookup,
                Config = config,
                Configs = configs,
                ElapsedTime = SystemAPI.GetSingleton<GameTimeData>().ElapsedTime,
                ECB = ecbP
            }.ScheduleParallel();

            new UnderBlessBonusTimer
            {
                DeltaTime = SystemAPI.GetSingleton<GameTimeData>().DeltaTime,
                ECB = ecbP,
            }.ScheduleParallel();
        }

        [BurstCompile]
        [WithNone(typeof(UnitDeadTag))]
        public partial struct BlessingBuffJob : IJobEntity
        {
            [ReadOnly] public ComponentLookup<LocalTransform> TransformLookup;
            [ReadOnly] public ComponentLookup<UnderBlessingBonus> LightCavalryUnderBonusLookup;
            [ReadOnly] public BlessingBuffGeneralConfig Config;
            [ReadOnly] public DynamicBuffer<BlessingBuffConfig> Configs;
            [ReadOnly] public float ElapsedTime;
            public EntityCommandBuffer.ParallelWriter ECB;

            private void Execute([ChunkIndexInQuery] int index, Entity selfEntity,
                in DynamicBuffer<AoeTarget> targets, in ExpData expData, in BlessingBuff blessingBuff)
            {
                if (blessingBuff.StopTime <= ElapsedTime)
                {
                    ECB.RemoveComponent<BlessingBuff>(index, selfEntity);
                    ECB.RemoveComponent<AoeTarget>(index, selfEntity);
                    return;
                }

                var count = 0;
                var maxCount = Configs[(int)expData.curTier - 3].maxAllyCount;
                for (int i = 0; i < targets.Length; i++)
                {
                    if (count >= maxCount) break;
                    var target = targets[i].Entity;

                    if (!LightCavalryUnderBonusLookup.HasComponent(target)
                        || LightCavalryUnderBonusLookup.IsComponentEnabled(target))
                    {
                        // Target is already taunted by self, then skip and add count, otherwise not add count
                        if (LightCavalryUnderBonusLookup.TryGetComponent(target, out var buff) &&
                            buff.Provider == selfEntity)
                        {
                            ECB.SetComponent(index, target, new UnderBlessingBonus
                            {
                                Provider = selfEntity,
                                LastTime = Config.UnderBlessingBonusDuration
                            });
                            count++;
                        }

                        continue;
                    }

                    ECB.SetComponentEnabled<UnderBlessingBonus>(index, target, true);
                    ECB.SetComponent(index, target, new UnderBlessingBonus
                    {
                        Provider = selfEntity,
                        LastTime = Config.UnderBlessingBonusDuration
                    });
                    ECB.SetComponent(index, target, new InteractAbilityBonus
                    {
                        AmountBonus = Configs[(int)expData.curTier - 3].amountBonus,
                        RangeBonus = Configs[(int)expData.curTier - 3].rangeBonus,
                        SpeedBonus = Configs[(int)expData.curTier - 3].speedBonus,
                        TargetsBonus = Configs[(int)expData.curTier - 3].targetsBonus,
                        MoveSpeedBonus = Configs[(int)expData.curTier - 3].moveSpeedBonus,
                    });
                    var up = ECB.CreateEntity(index);
                    ECB.AddComponent(index, up, new UpgradeRequest
                    {
                        FromEntity = target,
                    });
                    ECB.AddComponent<SubGameplayEntityTag>(index, up);
                    // statData.bonus = Configs[(int)expData.curTier - 3].statBonus;
                    // ECB.SetComponent(index, target, statData);

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
                        VFXName = VFXName.LightCavalryGain,
                        VFXTrackTarget = target
                    });
                    count++;
                }
            }
        }

        [BurstCompile]
        public partial struct UnderBlessBonusTimer : IJobEntity
        {
            [ReadOnly] public float DeltaTime;
            public EntityCommandBuffer.ParallelWriter ECB;

            private void Execute([ChunkIndexInQuery] int index, ref UnderBlessingBonus underBlessingBonus,
                Entity selfEntity, ref InteractAbilityBonus bonus)
            {
                underBlessingBonus.LastTime -= DeltaTime;
                if (underBlessingBonus.LastTime <= 0)
                {
                    underBlessingBonus.LastTime = 0;
                    underBlessingBonus.Provider = Entity.Null;
                    ECB.SetComponentEnabled<UnderBlessingBonus>(index, selfEntity, false);
                    var shieldVfx = ECB.CreateEntity(index);
                    ECB.AddComponent<SubGameplayEntityTag>(index, shieldVfx);
                    ECB.AddComponent(index, shieldVfx, new VFXRequest
                    {
                        RequestType = VFXRequestType.Kill,
                        VFXName = VFXName.LightCavalryGain,
                        VFXTrackTarget = selfEntity
                    });
                    bonus = new InteractAbilityBonus();
                }
            }
        }
    }
}