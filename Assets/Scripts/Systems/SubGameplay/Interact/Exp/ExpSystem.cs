using SparFlame.Components.General;
using SparFlame.Components.SubGameplay;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;

namespace SparFlame.Systems.SubGameplay.Interact
{
    partial struct ExpSystem : ISystem
    {
        private NativeHashMap<int, int> _expGainTypeToGainAmount;
        private ComponentLookup<ExpData> _expDataLookup;
        private ComponentLookup<SubGameplayGeneralAttr> _generalAttrLookup;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<PlayerFactionData>();
            state.RequireForUpdate<EndSimulationEntityCommandBufferSystem.Singleton>();
            state.RequireForUpdate<SubGamingTag>();
            state.RequireForUpdate<ExpGainRequest>();
            _expDataLookup = state.GetComponentLookup<ExpData>();
            _generalAttrLookup = state.GetComponentLookup<SubGameplayGeneralAttr>(true);
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            if (!_expGainTypeToGainAmount.IsCreated)
            {
                Initialize();
            }

            _expDataLookup.Update(ref state);
            _generalAttrLookup.Update(ref state);
            new ExpGainRequestJob
            {
                ExpLookup = _expDataLookup,
                GeneralAttrLookup = _generalAttrLookup,
                ExpGainTypeToGainAmount = _expGainTypeToGainAmount,
                ECB = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>()
                    .CreateCommandBuffer(state.WorldUnmanaged).AsParallelWriter(),
                ExpDebug = SystemAPI.HasSingleton<DebugTag>() && SystemAPI.TryGetSingleton(out ExpDebug expDebug)
                    ? expDebug
                    : new ExpDebug
                    {
                        aiExpGainScale = 1f,
                        playerExpGainScale = 1f
                    },
                PlayerFaction = SystemAPI.GetSingleton<PlayerFactionData>().Value,
            }.Schedule();
        }

        private void Initialize()
        {
            var buffer = SystemAPI.GetSingletonBuffer<ExpSystemConfig>();
            _expGainTypeToGainAmount = new NativeHashMap<int, int>(buffer.Length, Allocator.Persistent);
            foreach (var config in buffer)
            {
                _expGainTypeToGainAmount.Add((int)config.type, config.gainAmount);
            }
        }

        [BurstCompile]
        public void OnDestroy(ref SystemState state)
        {
            if (_expGainTypeToGainAmount.IsCreated)
                _expGainTypeToGainAmount.Dispose();
        }

        // This job cannot parallel
        [BurstCompile]
        public partial struct ExpGainRequestJob : IJobEntity
        {
            public EntityCommandBuffer.ParallelWriter ECB;
            [ReadOnly] public NativeHashMap<int, int> ExpGainTypeToGainAmount;
            [ReadOnly] public ExpDebug ExpDebug;
            [ReadOnly] public FactionTag PlayerFaction;
            [NativeDisableParallelForRestriction] public ComponentLookup<ExpData> ExpLookup;
            [ReadOnly] public ComponentLookup<SubGameplayGeneralAttr> GeneralAttrLookup;


            private void Execute([ChunkIndexInQuery] int index, ref ExpGainRequest request, Entity selfEntity)
            {
                ECB.DestroyEntity(index, selfEntity);
                if (!GeneralAttrLookup.TryGetComponent(request.GainEntity, out var generalAttr)) return;
                if (!ExpLookup.HasComponent(request.GainEntity)) return;
                var expData = ExpLookup.GetRefRW(request.GainEntity);
                var debugScale = generalAttr.FactionTag == PlayerFaction
                    ? ExpDebug.playerExpGainScale
                    : ExpDebug.aiExpGainScale;
                if (request.Multiplier == 0) request.Multiplier = 1f;
                expData.ValueRW.curValue +=
                    ExpGainTypeToGainAmount[(int)request.Type] * request.Multiplier * debugScale;
                if (expData.ValueRO.curValue >= expData.ValueRO.maxValue)
                {
                    var upgradeRequest = ECB.CreateEntity(index);
                    ECB.AddComponent(index, upgradeRequest, new UpgradeRequest
                    {
                        FromEntity = request.GainEntity
                    });
                    expData.ValueRW.curValue -= expData.ValueRO.maxValue;
                }
            }
        }
    }
}