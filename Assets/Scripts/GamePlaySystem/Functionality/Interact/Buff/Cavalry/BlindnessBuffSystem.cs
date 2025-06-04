// using SparFlame.GamePlaySystem.General;
// using SparFlame.GamePlaySystem.Interact.Blindness;
// using Unity.Burst;
// using Unity.Entities;
//
// namespace SparFlame.GamePlaySystem.Interact
// {
//     public partial struct BlindnessBuffSystem : ISystem
//     {
//         [BurstCompile]
//         public void OnCreate(ref SystemState state)
//         {
//             state.RequireForUpdate<GameTimeData>();
//             state.RequireForUpdate<EndSimulationEntityCommandBufferSystem.Singleton>();
//             state.RequireForUpdate<GamingTag>();
//             state.RequireForUpdate<BlindnessLastData>();
//         }
//
//         [BurstCompile]
//         public void OnUpdate(ref SystemState state)
//         {
//             new BlindnessLastTimeJob
//             {
//                 ECB = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>()
//                     .CreateCommandBuffer(state.WorldUnmanaged).AsParallelWriter(),
//                 CurTime = SystemAPI.GetSingleton<GameTimeData>().ElapsedTime
//             }.ScheduleParallel();
//         }
//
//
//         [BurstCompile]
//         [WithDisabled(typeof(ContributeSightTag))]
//         public partial struct BlindnessLastTimeJob : IJobEntity
//         {
//             public EntityCommandBuffer.ParallelWriter ECB;
//             public float CurTime;
//
//             private void Execute([ChunkIndexInQuery] int index, Entity selfEntity,in BlindnessLastData data)
//             {
//                 if (CurTime > data.StopTime)
//                 {
//                     // ECB.RemoveComponent<BlindnessLastData>(index, selfEntity);
//                     ECB.SetComponentEnabled<ContributeSightTag>(index, selfEntity, true);
//                 }
//             }
//         }
//     }
// }