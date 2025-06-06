using SparFlame.GamePlaySystem.CustomParticleSystem;
using SparFlame.GamePlaySystem.General;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Transforms;

namespace SparFlame.GamePlaySystem.Interact.Cleric
{
    public partial struct DarkCavalryBuffSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<EndSimulationEntityCommandBufferSystem.Singleton>();
            state.RequireForUpdate<GamingTag>();
            state.RequireForUpdate<DarkCavalryBuff>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            new DarkCavalryBuffJob
            {
                ECB = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>().CreateCommandBuffer(state.WorldUnmanaged).AsParallelWriter(),
                Configs = SystemAPI.GetSingletonBuffer<DarkCavalryBuffConfig>(),
            }.ScheduleParallel();
        }


        [BurstCompile]
        [WithAll(typeof(DarkCavalryBuff))]
        [WithNone(typeof(UnitDeadTag))]
        public partial struct DarkCavalryBuffJob : IJobEntity
        {
            public EntityCommandBuffer.ParallelWriter ECB;
            [ReadOnly] public DynamicBuffer<DarkCavalryBuffConfig> Configs;

            private void Execute([ChunkIndexInQuery] int index, ref InteractAbilityBonus bonus, in StatData stat,
                in ExpData expData, in AttackAbility attackAbility, in LocalTransform transform, Entity selfEntity)
            {
                if (stat.CurValue >= (stat.MaxValue + stat.Bonus) * 0.5f)
                {
                    bonus.AmountBonus = 0;
                    var vfxKillRequest = ECB.CreateEntity(index);
                    ECB.AddComponent<GameplayEntityTag>(index, vfxKillRequest);
                    ECB.AddComponent(index, vfxKillRequest, new VFXRequest
                    {
                        RequestType = VFXRequestType.Kill,
                        VFXName = VFXName.DarkCavalryAttackGain,
                        VFXTrackTarget = selfEntity
                    });
                    return;
                }

                var lossPercent = 1f - stat.CurValue / stat.MaxValue;
                var bonusScale = Configs[(int)expData.CurTier - 3].attackAmountBonusWhenFullLossHp;
                if (bonus.AmountBonus == 0)
                {
                    var vfxRequest = ECB.CreateEntity(index);
                    ECB.AddComponent<GameplayEntityTag>(index, vfxRequest);
                    ECB.AddComponent(index, vfxRequest, new VFXRequest
                    {
                        Filter = new VFXSubFilter
                        {
                            TierFilterEnable = true,
                            Tier = expData.CurTier
                        },
                        KeepDuration = float.MaxValue,
                        RequestType = VFXRequestType.Spawn,
                        VFXName = VFXName.DarkCavalryAttackGain,
                        SpawnPosition = transform.Position,
                        VFXTrackTarget = selfEntity
                    });
                }
                bonus.AmountBonus = (int)(attackAbility.Amount * lossPercent * bonusScale);
               
            }
        }
    }
}