// using SparFlame.Components.General;
// using SparFlame.Components.SubGameplay;
// using SparFlame.Components.VFX;
// using Unity.Burst;
// using Unity.Collections;
// using Unity.Entities;
// using Unity.Transforms;
//
// namespace SparFlame.Systems.SubGameplay.Interact.GamePlaySystem.Functionality.Interact.Buff.Cavalry
// {
//     public partial struct DamageReduceShieldBuffSystem : ISystem
//     {
//         private ComponentLookup<DamageReduceShieldBuff> _cavalryMoveBuffLookup;
//         [BurstCompile]
//         public void OnCreate(ref SystemState state)
//         {
//             state.RequireForUpdate<SubGamingTag>();
//             state.RequireForUpdate<DualSpearTag>();
//             state.RequireForUpdate<BeginSimulationEntityCommandBufferSystem.Singleton>();
//             _cavalryMoveBuffLookup = state.GetComponentLookup<DamageReduceShieldBuff>();
//         }
//
//         [BurstCompile]
//         public void OnUpdate(ref SystemState state)
//         {
//             _cavalryMoveBuffLookup.Update(ref state);
//             new DamageReduceShieldBuffJob
//             {
//                 ECB = SystemAPI.GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>()
//                     .CreateCommandBuffer(state.WorldUnmanaged).AsParallelWriter(),
//                 DamageReduceShieldBuffLookup = _cavalryMoveBuffLookup
//             }.ScheduleParallel();
//         }
//
//        
//         
//         [BurstCompile]
//         [WithPresent(typeof(DamageReduceShieldBuff))]
//         public partial struct DamageReduceShieldBuffJob : IJobEntity
//         {
//             public EntityCommandBuffer.ParallelWriter ECB;
//             [NativeDisableParallelForRestriction] public ComponentLookup<DamageReduceShieldBuff> DamageReduceShieldBuffLookup;
//             private void Execute([ChunkIndexInQuery]int index,in SubGameplayGeneralAttr subGameplayGeneralAttr,in BasicStateData stateData, Entity selfEntity,
//                 in LocalTransform transform, in DynamicBuffer<TrackedByVFX> trackedByVfx)
//             {
//                 var buffEnable = DamageReduceShieldBuffLookup.IsComponentEnabled(selfEntity);
//                 if (!buffEnable)
//                 {
//                     DamageReduceShieldBuffLookup.SetComponentEnabled(selfEntity, true);
//                     var vfxRequest = ECB.CreateEntity(index);
//                     ECB.AddComponent<SubGameplayEntityTag>(index, vfxRequest);
//                     ECB.AddComponent(index, vfxRequest, new VFXRequest
//                     {
//                         Filter = new VFXSubFilter
//                         {
//                             FactionFilterEnable  = true,
//                             Faction = subGameplayGeneralAttr.Faction
//                         },
//                         RequestType = VFXRequestType.Spawn,
//                         VFXName = VFXName.CavalryMoveDamageReduction,
//                         SpawnPosition = transform.Position,
//                         VFXTrackTarget = selfEntity,
//                         KeepDuration = float.MaxValue,
//                     });
//                 }
//
//                 if (stateData.CurState != InteractState.Moving && buffEnable)
//                 {
//                     DamageReduceShieldBuffLookup.SetComponentEnabled(selfEntity, false);
//                     var vfxRequest = ECB.CreateEntity(index);
//                     ECB.AddComponent<SubGameplayEntityTag>(index, vfxRequest);
//                     ECB.AddComponent(index, vfxRequest, new VFXRequest
//                     {
//                         RequestType = VFXRequestType.Kill,
//                         VFXName = VFXName.CavalryMoveDamageReduction,
//                         VFXTrackTarget = selfEntity,
//                     });
//                 }
//             }
//         }
//     }
// }