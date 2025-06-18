using SparFlame.Components.General;
using SparFlame.Components.SubGameplay;
using SparFlame.Components.VFX;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Transforms;

namespace SparFlame.Systems.SubGameplay.Interact.GamePlaySystem.Functionality.Interact.Buff.Cavalry
{
    public partial struct CavalryMoveBuffSystem : ISystem
    {
        private ComponentLookup<CavalryMoveBuff> _cavalryMoveBuffLookup;
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<SubGamingTag>();
            state.RequireForUpdate<CavalryTag>();
            state.RequireForUpdate<BeginSimulationEntityCommandBufferSystem.Singleton>();
            _cavalryMoveBuffLookup = state.GetComponentLookup<CavalryMoveBuff>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            _cavalryMoveBuffLookup.Update(ref state);
            new CavalryMoveBuffJob
            {
                ECB = SystemAPI.GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>()
                    .CreateCommandBuffer(state.WorldUnmanaged).AsParallelWriter(),
                CavalryMoveBuffLookup = _cavalryMoveBuffLookup
            }.ScheduleParallel();
        }

       
        
        [BurstCompile]
        [WithAll(typeof(CavalryTag))]
        public partial struct CavalryMoveBuffJob : IJobEntity
        {
            public EntityCommandBuffer.ParallelWriter ECB;
            [NativeDisableParallelForRestriction] public ComponentLookup<CavalryMoveBuff> CavalryMoveBuffLookup;
            private void Execute([ChunkIndexInQuery]int index,in SubGameplayGeneralAttr subGameplayGeneralAttr,in BasicStateData stateData, Entity selfEntity,
                in LocalTransform transform)
            {
                var buffEnable = CavalryMoveBuffLookup.IsComponentEnabled(selfEntity);
                if (stateData.CurState == InteractState.Moving && !buffEnable)
                {
                    CavalryMoveBuffLookup.SetComponentEnabled(selfEntity, true);
                    var vfxRequest = ECB.CreateEntity(index);
                    ECB.AddComponent<SubGameplayEntityTag>(index, vfxRequest);
                    ECB.AddComponent(index, vfxRequest, new VFXRequest
                    {
                        Filter = new VFXSubFilter
                        {
                            FactionFilterEnable  = true,
                            Faction = subGameplayGeneralAttr.FactionTag
                        },
                        RequestType = VFXRequestType.Spawn,
                        VFXName = VFXName.CavalryMoveDamageReduction,
                        SpawnPosition = transform.Position,
                        VFXTrackTarget = selfEntity,
                        KeepDuration = float.MaxValue,
                    });
                }

                if (stateData.CurState != InteractState.Moving && buffEnable)
                {
                    CavalryMoveBuffLookup.SetComponentEnabled(selfEntity, false);
                    var vfxRequest = ECB.CreateEntity(index);
                    ECB.AddComponent<SubGameplayEntityTag>(index, vfxRequest);
                    ECB.AddComponent(index, vfxRequest, new VFXRequest
                    {
                        RequestType = VFXRequestType.Kill,
                        VFXName = VFXName.CavalryMoveDamageReduction,
                        VFXTrackTarget = selfEntity,
                    });
                }
            }
        }
    }
}