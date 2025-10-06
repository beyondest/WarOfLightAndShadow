using SparFlame.Components.General;
using SparFlame.Components.SubGameplay;
using SparFlame.Components.VFX;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;

namespace SparFlame.Systems.SubGameplay.Interact.MagicDamage
{
    public partial struct LightMagicDamageBuffSystem : ISystem
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
            new LightMagicDamageTimerJob
            {
                ECB = SystemAPI.GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>()
                    .CreateCommandBuffer(state.WorldUnmanaged).AsParallelWriter(),
                DeltaTime = SystemAPI.Time.DeltaTime
            }.ScheduleParallel();
        }


        [BurstCompile]
        [WithNone(typeof(UnitDeadTag))]
        public partial struct LightMagicDamageTimerJob : IJobEntity
        {
            public EntityCommandBuffer.ParallelWriter ECB;
            [ReadOnly] public float DeltaTime;

            private void Execute([ChunkIndexInQuery] int index, ref LightMagicDamageBuff buff, Entity selfEntity
                , ref InteractAbilityBonus bonus, in MovableData movableData)
            {
                buff.LastTime -= DeltaTime;
                if (buff.LastTime <= 0)
                {
                    ECB.SetComponentEnabled<LightMagicDamageBuff>(index, selfEntity, false);
                    bonus.SpeedBonus = 0;
                    bonus.MoveSpeedBonus = 0;
                    var vfxRequest = ECB.CreateEntity(index);
                    ECB.AddComponent<SubGameplayEntityTag>(index, vfxRequest);
                    ECB.AddComponent(index, vfxRequest, new VFXRequest
                    {
                        RequestType = VFXRequestType.Kill,
                        VFXName = VFXName.LightMagicDamageDebuff,
                        VFXTrackTarget = selfEntity,
                    });
                    return;
                }

                bonus.SpeedBonus = buff.SpeedNegativeBonus;
                bonus.MoveSpeedBonus = buff.MoveSpeedNegativeBonus;
            }
        }
    }
}