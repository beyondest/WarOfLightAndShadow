using SparFlame.GamePlaySystem.General;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;

namespace SparFlame.GamePlaySystem.Interact
{
    public partial struct AoeInteractBuffSystem : ISystem
    {
        private ComponentLookup<GeneralAttr> _generalAttrLookup;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<EndSimulationEntityCommandBufferSystem.Singleton>();
            state.RequireForUpdate<GameTimeData>();
            state.RequireForUpdate<GamingTag>();
            state.RequireForUpdate<AoeInteractData>();
            _generalAttrLookup = state.GetComponentLookup<GeneralAttr>(true);
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
        [ReadOnly] public ComponentLookup<GeneralAttr> GeneralAttrLookup;

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

                    var underDefendTargetGetDamageScale = 1f;
                    // var ifDefendByTier3 = false;
                    /*
                    // When this is attack aoe and this unit can be defended. Shield cannot be defended by shield.
                    if (data.StatChangeRequest.Type == StatChangeType.Attack && DefenderDataLookup.TryGetBuffer(target.Entity, out var buffer))
                    {
                        var totalDamage = targets.Length * data.StatChangeRequest.AbsAmount;
                        // Split the damage to all shields
                        var defenderGetDamage = buffer.Length == 0 ? 0 : totalDamage / buffer.Length;
                        for (int j = buffer.Length - 1; j >=0; j--)
                        {
                            var defender = buffer[j];
                            // Invalid defender, defender is already dead
                            if (!GeneralAttrLookup.HasComponent(defender.DefendBy))
                            {
                                buffer.RemoveAt(j);
                                continue;
                            }

                            if (underDefendTargetGetDamageScale > defender.GetDamageScale) underDefendTargetGetDamageScale = defender.GetDamageScale;
                            var thisShieldGetDamage = defenderGetDamage * defender.SelfGetDamageScale;
                            if(defender.IfTier3) ifDefendByTier3 = true;
                            var shieldDamageRequest = ECB.CreateEntity();
                            ECB.AddComponent<GameplayEntityTag>(shieldDamageRequest);
                            var shieldDamageRequestData = data.StatChangeRequest;
                            shieldDamageRequestData.Interactee = defender.DefendBy;
                            shieldDamageRequestData.AbsAmount = (int)thisShieldGetDamage;
                            ECB.AddComponent( shieldDamageRequest,shieldDamageRequestData);
                        }
                    }
                    */

                    // When this unit is defended by tier3, it will not get damage until tier 3 shield dead
                    // if (!ifDefendByTier3 || data.StatChangeRequest.Type != StatChangeType.Attack)
                    var request = ECB.CreateEntity();
                    ECB.AddComponent<GameplayEntityTag>(request);
                    var requestData = data.StatChangeRequest;
                    requestData.Interactee = target.Entity;
                    requestData.AbsAmount = (int)(requestData.AbsAmount * underDefendTargetGetDamageScale);
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