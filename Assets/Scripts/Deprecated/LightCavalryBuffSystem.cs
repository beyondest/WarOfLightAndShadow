// using SparFlame.GamePlaySystem.CustomParticleSystem;
// using SparFlame.GamePlaySystem.Fow;
// using SparFlame.GamePlaySystem.General;
// using SparFlame.GamePlaySystem.Interact.Blindness;
// using SparFlame.GamePlaySystem.Interact.GamePlaySystem.Functionality.Interact.Buff.Authoring;
// using Unity.Burst;
// using Unity.Collections;
// using Unity.Entities;
// using Unity.Mathematics;
// using Unity.Transforms;
//
// namespace SparFlame.GamePlaySystem.Interact
// {
//     public partial struct LightCavalryBuffSystem : ISystem
//     {
//         [BurstCompile]
//         public void OnCreate(ref SystemState state)
//         {
//             state.RequireForUpdate<EndSimulationEntityCommandBufferSystem.Singleton>();
//             state.RequireForUpdate<GameTimeData>();
//         }
//
//         [BurstCompile]
//         public void OnUpdate(ref SystemState state)
//         {
//             var curTime = SystemAPI.GetSingleton<GameTimeData>().ElapsedTime;
//             var ecb = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>();
//             var ecbP = ecb.CreateCommandBuffer(state.WorldUnmanaged).AsParallelWriter();
//             new LightCavalryFireLastTimeJob
//             {
//                 CurTime = curTime,
//                 ECB = ecbP,
//             }.ScheduleParallel();
//             new LightCavalrySelfBurnJob
//             {
//                 CurTime = curTime,
//                 ECB = ecbP,
//             }.ScheduleParallel();
//         }
//
//   
//         [BurstCompile]
//         [WithAll(typeof(ContributeSightTag))]
//         [WithNone(typeof(BlindnessLastData))]
//         public partial struct LightCavalryFireLastTimeJob : IJobEntity
//         {
//             [ReadOnly] public float CurTime;
//             public EntityCommandBuffer.ParallelWriter ECB;
//             private void Execute([ChunkIndexInQuery] int index, Entity selfEntity, in LightCavalryBuffData data)
//             {
//                 if (CurTime > data.StopTime)
//                 {
//                     ECB.SetComponentEnabled<ContributeSightTag>(index,selfEntity,false);
//                 }
//             }
//         }
//         
//         
//         [BurstCompile]
//         [WithDisabled(typeof(ContributeSightTag))]
//         [WithAll(typeof(InDarknessTag))]
//         [WithNone(typeof(BlindnessLastData))]
//         public partial struct LightCavalrySelfBurnJob : IJobEntity
//         {
//             [ReadOnly] public float CurTime;
//             public EntityCommandBuffer.ParallelWriter ECB;
//             private void Execute([ChunkIndexInQuery] int index, Entity selfEntity, ref StatData statData, ref LightCavalryBuffData data,
//                 in LocalTransform transform)
//             {
//                 ECB.SetComponentEnabled<ContributeSightTag>(index, selfEntity,true);
//                 data.StopTime = CurTime + data.LastDuration;
//                 statData.CurValue = math.max(1f, statData.CurValue - statData.CurValue * data.ReduceCurrentHpRatio);
//                 var vfx = ECB.CreateEntity(index);
//                 ECB.AddComponent<GameplayEntityTag>(index,vfx);
//                 ECB.AddComponent(index,vfx, new VFXRequest
//                 {
//                     Filter = default,
//                     SpawnPosition = transform.Position,
//                     KeepDuration = 3,
//                     RequestType = VFXRequestType.Spawn,
//                     StatChangeRequest = default,
//                     TargetPosition = transform.Position,
//                     VFXName = VFXName.LightSelfBurn,
//                     VFXTrackTarget = selfEntity,
//                 });
//                 
//             }
//         }
//         
//     }
// }