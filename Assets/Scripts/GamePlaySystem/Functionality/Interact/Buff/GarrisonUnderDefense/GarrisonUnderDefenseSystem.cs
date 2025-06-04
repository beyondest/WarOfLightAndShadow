using SparFlame.GamePlaySystem.CustomParticleSystem;
using SparFlame.GamePlaySystem.Garrison;
using SparFlame.GamePlaySystem.General;
using SparFlame.GamePlaySystem.Interact.GamePlaySystem.Functionality.Interact.Buff.Authoring;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace SparFlame.GamePlaySystem.Interact.GarrisonUnderDefense
{
    // [UpdateBefore(typeof(StatSystem))]
    public partial struct GarrisonUnderDefenseSystem : ISystem
    {
        public struct GarrisonBuffRegenerationTimeData : IComponentData
        {
            public float Value;
        }

        private ComponentLookup<ExpData> _expLookup;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<GarrisonBuffConfig>();
            state.RequireForUpdate<GameTimeData>();
            state.RequireForUpdate<EndSimulationEntityCommandBufferSystem.Singleton>();
            state.RequireForUpdate<GarrisonAttr>();
            state.RequireForUpdate<GameStatusData>();
            _expLookup = state.GetComponentLookup<ExpData>(true);
            // _defenceLookup = state.GetComponentLookup<UnderDefence>(true);
            state.EntityManager.CreateSingleton(new GarrisonBuffRegenerationTimeData
            {
                Value = 0
            });
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var gameStatus = SystemAPI.GetSingleton<GameStatusData>().Value;
            if (gameStatus == GameStatus.Init)
            {
                SystemAPI.SetSingleton(new GarrisonBuffRegenerationTimeData
                {
                    Value = 0
                });
                return;
            }
            if(gameStatus != GameStatus.Gaming)return;
            _expLookup.Update(ref state);
            ref var value = ref SystemAPI.GetSingletonRW<GarrisonBuffRegenerationTimeData>().ValueRW;
            var curTime = SystemAPI.GetSingleton<GameTimeData>().ElapsedTime;
            var config = SystemAPI.GetSingleton<GarrisonBuffConfig>();
            var shouldRegenerate = false;
            if (curTime > value.Value)
            {
                shouldRegenerate = true;
                value.Value = curTime +config.HpRegenerationInterval ;
            }
            new BuildingUnderDefenseJob
            {
                ECB = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>()
                    .CreateCommandBuffer(state.WorldUnmanaged).AsParallelWriter(),
                Config = config,
                ShouldRecover = shouldRegenerate,
                ExpLookup = _expLookup,
            }.ScheduleParallel();
        }
        [BurstCompile]
        [WithAll(typeof(GarrisonAttr))]
        public partial struct BuildingUnderDefenseJob : IJobEntity
        {
            [ReadOnly] public bool ShouldRecover;
            [ReadOnly] public GarrisonBuffConfig Config;
            public EntityCommandBuffer.ParallelWriter ECB;
            [ReadOnly] public ComponentLookup<ExpData> ExpLookup;

            private void Execute([ChunkIndexInQuery] int index, in LocalTransform transform, Entity selfEntity,
                in GeneralAttr generalAttr, ref StatData statData, in DynamicBuffer<GarrisonEntity> entities)
            {
                var hasExp = ExpLookup.TryGetComponent(selfEntity, out var exp);
                if (entities.Length > Config.MinCountToTriggerGarrisonBuff)
                {
                    var vfxRequest = ECB.CreateEntity(index);
                    ECB.AddComponent(index, vfxRequest, new VFXRequest
                    {
                        Filter = new VFXSubFilter
                        {
                            Faction = generalAttr.FactionTag,
                            FactionFilterEnable = true,
                            TierFilterEnable = true,
                            Tier = hasExp ? exp.CurTier : Tier.Tier1
                        },
                        KeepDuration = float.MaxValue,
                        RequestType = VFXRequestType.Spawn,
                        SpawnPosition = transform.Position,
                        TargetPosition = transform.Position,
                        StatChangeRequest = default,
                        VFXName = hasExp ? VFXName.GarrisonUnderDefense : VFXName.GarrisonUnderDefenseTier4,
                        VFXTrackTarget = selfEntity,
                    });
                    if (ShouldRecover)
                    {
                        statData.CurValue = math.min(statData.MaxValue,
                            statData.CurValue + Config.HpRegenerationAmountRatio * statData.MaxValue);
                    }
                }
                else
                {
                    var vfxRequest = ECB.CreateEntity(index);
                    ECB.AddComponent(index, vfxRequest, new VFXRequest
                    {
                        Filter = new VFXSubFilter
                        {
                            Faction = generalAttr.FactionTag,
                            FactionFilterEnable = true,
                            TierFilterEnable = true,
                            Tier =hasExp? exp.CurTier : Tier.Tier1
                        },
                        KeepDuration = float.MaxValue,
                        RequestType = VFXRequestType.Kill,
                        SpawnPosition = transform.Position,
                        TargetPosition = transform.Position,
                        StatChangeRequest = default,
                        VFXName = hasExp ? VFXName.GarrisonUnderDefense : VFXName.GarrisonUnderDefenseTier4,
                        VFXTrackTarget = selfEntity,
                    });
                }
            }
        }
    }
}