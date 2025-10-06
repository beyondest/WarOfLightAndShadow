using SparFlame.Components.General;
using SparFlame.Components.SubGameplay;
using SparFlame.Components.VFX;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;

namespace SparFlame.Systems.SubGameplay.Interact.MagicDamage
{
    public partial struct DarkMagicDamageSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<SubGamingTag>();
            state.RequireForUpdate<LightMagicDamageBuff>();
            state.RequireForUpdate<BeginSimulationEntityCommandBufferSystem.Singleton>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            new DarkMagicDamageTimerJob
            {
                ECB = SystemAPI.GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>()
                    .CreateCommandBuffer(state.WorldUnmanaged).AsParallelWriter(),
                DeltaTime = SystemAPI.Time.DeltaTime
            }.ScheduleParallel();
        }

        
        [BurstCompile]
        [WithNone(typeof(UnitDeadTag))]
        public partial struct DarkMagicDamageTimerJob : IJobEntity
        {
            public EntityCommandBuffer.ParallelWriter ECB;
            [ReadOnly] public float DeltaTime;

            private void Execute([ChunkIndexInQuery] int index, ref DarkMagicDamageBuff buff, Entity selfEntity)
            {
                buff.LastTime -= DeltaTime;
                if (buff.LastTime <= 0)
                {
                    ECB.SetComponentEnabled<DarkMagicDamageBuff>(index, selfEntity, false);
                    buff.HealingReductionPercent = 0;
                    var vfxRequest = ECB.CreateEntity(index);
                    ECB.AddComponent<SubGameplayEntityTag>(index, vfxRequest);
                    ECB.AddComponent(index, vfxRequest, new VFXRequest
                    {
                        RequestType = VFXRequestType.Kill,
                        VFXName = VFXName.DarkMagicDamageDebuff,
                        VFXTrackTarget = selfEntity,
                    });
                }
            }
        
        }
    }
    
    
    
}