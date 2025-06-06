using SparFlame.GamePlaySystem.CustomParticleSystem;
using SparFlame.GamePlaySystem.General;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;

namespace SparFlame.GamePlaySystem.Interact.Cleric
{
    public partial struct DarkClericBuffSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<GamingTag>();
            state.RequireForUpdate<DarkClericBuff>();
            state.RequireForUpdate<BeginSimulationEntityCommandBufferSystem.Singleton>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            new DarkClericBuffJob
            {
                ECB = SystemAPI.GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>()
                    .CreateCommandBuffer(state.WorldUnmanaged).AsParallelWriter(),
                DeltaTime = SystemAPI.Time.DeltaTime
            }.ScheduleParallel();
        }

   
        [BurstCompile]
        [WithNone(typeof(UnitDeadTag))]
        public partial struct DarkClericBuffJob : IJobEntity
        {
            public EntityCommandBuffer.ParallelWriter ECB;
           [ReadOnly] public float DeltaTime;
            private void Execute([ChunkIndexInQuery]int index,ref DarkClericBuff buff
                ,ref InteractAbilityBonus bonus, Entity selfEntity,in AttackAbility attackAbility)
            {
                buff.LastTime -= DeltaTime;
                if (buff.LastTime <= 0)
                {
                    ECB.SetComponentEnabled<DarkClericBuff>(index, selfEntity, false);
                    bonus.AmountBonus = 0;
                    var vfxRequest = ECB.CreateEntity(index);
                    ECB.AddComponent<GameplayEntityTag>(index, vfxRequest);
                    ECB.AddComponent(index, vfxRequest, new VFXRequest
                    {
                        RequestType = VFXRequestType.Kill,
                        VFXName = VFXName.DarkMagicDamageDebuff,
                        VFXTrackTarget = selfEntity,
                    });
                    return;
                }
                bonus.AmountBonus = (int)(buff.AttackAmountBonusScale * attackAbility.Amount);
            }
        }
    }
}