using SparFlame.GamePlaySystem.General;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;

namespace SparFlame.GamePlaySystem.Interact
{
    public partial struct AoeInteractBuffSystem : ISystem
    {
        private ComponentLookup<SubGameplayGeneralAttr> _generalAttrLookup;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<EndSimulationEntityCommandBufferSystem.Singleton>();
            state.RequireForUpdate<GameTimeData>();
            state.RequireForUpdate<SubGamingTag>();
            state.RequireForUpdate<AoeInteractData>();
            _generalAttrLookup = state.GetComponentLookup<SubGameplayGeneralAttr>(true);
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            _generalAttrLookup.Update(ref state);
            var ecb = new EntityCommandBuffer(Allocator.TempJob);
            var job = new AoeInteractJob
            {
                ECB = ecb,
                GeneralAttrLookup = _generalAttrLookup,
                CurTime = SystemAPI.GetSingleton<GameTimeData>().ElapsedTime,
            }.Schedule(state.Dependency);
            job.Complete();
            ecb.Playback(state.EntityManager);
            ecb.Dispose();
        }
    }

    [BurstCompile]
    public partial struct AoeInteractJob : IJobEntity
    {
        [ReadOnly] public ComponentLookup<SubGameplayGeneralAttr> GeneralAttrLookup;

        [ReadOnly] public float CurTime;

        // [NativeDisableParallelForRestriction] public BufferLookup<LightShieldUnderDefend> DefenderDataLookup;
        public EntityCommandBuffer ECB;

        private void Execute(ref AoeInteractData data, Entity selEntity, ref DynamicBuffer<AoeTarget> targets)
        {
            if (CurTime > data.TriggerTime)
            {
                for (var i = targets.Length - 1; i >= 0; i--)
                {
                    var target = targets[i];
                    // remove dead targets
                    if (!GeneralAttrLookup.TryGetComponent(target.Entity, out var attr)
                        || attr.FactionTag != data.TargetFaction)
                    {
                        targets.RemoveAt(i);
                        continue;
                    }
                    var request = ECB.CreateEntity();
                    ECB.AddComponent<SubGameplayEntityTag>(request);
                    var requestData = data.StatChangeRequest;
                    requestData.Interactee = target.Entity;
                    ECB.AddComponent(request, requestData);
                }

                data.CurrentTriggerCount++;
                data.TriggerTime = CurTime + data.TriggerDuration;
            }

            if (data.CurrentTriggerCount >= data.MaxTriggerCount)
            {
                ECB.DestroyEntity(selEntity);
            }
        }
    }
}