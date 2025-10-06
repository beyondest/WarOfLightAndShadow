// using SparFlame.Components.General;
// using SparFlame.Components.SubGameplay;
// using Unity.Burst;
// using Unity.Collections;
// using Unity.Entities;
// using Unity.Physics;
// using Unity.Physics.Stateful;
// using Unity.Physics.Systems;
//
// namespace SparFlame.Systems.SubGameplay.Interact
// {
//     [UpdateInGroup(typeof(PhysicsSystemGroup))]
//     [UpdateAfter(typeof(StatefulTriggerEventBufferSystem))]
//     public partial struct SightTriggerSystem : ISystem
//     {
//         private BufferLookup<InsightTarget> _targetLookup;
//         
//         [BurstCompile]
//         public void OnCreate(ref SystemState state)
//         {
//             state.RequireForUpdate<SimulationSingleton>();
//             state.RequireForUpdate<SubGamingTag>();
//             state.RequireForUpdate<SightSystemConfig>();
//             state.RequireForUpdate<SightData>();
//             _targetLookup = state.GetBufferLookup<InsightTarget>();
//         }
//
//         [BurstCompile]
//         public void OnUpdate(ref SystemState state)
//         {
//             _targetLookup.Update(ref state);
//             new SightTriggerJob
//             {
//                 TargetLookup = _targetLookup
//             }.ScheduleParallel();
//         }
//
//         // Warning : This job cannot delay for even a little time;MUST be in front of all other jobs
//         [BurstCompile]
//         public partial struct SightTriggerJob : IJobEntity
//         {
//             // [ReadOnly] public ComponentLookup<SightPriority> PriorityLookup;
//             [NativeDisableParallelForRestriction] public BufferLookup<InsightTarget> TargetLookup;
//             
//             private void Execute(ref DynamicBuffer<StatefulTriggerEvent> events, in SightData data,
//                 Entity entity)
//             {
//                 // This may happen when belongs to entity is dead but the sight not been removed by sight system yet
//                 if(!TargetLookup.TryGetBuffer(data.BelongsTo, out var targets))return;
//                 
//                 // var selfFaction = GeneralAttrLookup[entity].FactionTag;
//                 // Add insight target, remove out sight target
//                 foreach (var triggerEvent in events)
//                 {
//                     var target = triggerEvent.GetOtherEntity(entity);
//                     
//                     // Check if target is valid for target
//                     // if( !PriorityLookup.TryGetComponent(target, out var priority))continue;
//
//                     var insightTarget = new InsightTarget
//                     {
//                         Entity = target,
//                         PriorityValue = 0f,
//                         DisValue = 0f,
//                         StatChangValue = 0f,
//                         InteractOverride = 0f,
//                         MemoryValue = 0f,
//                         TotalValue = 0f
//                     };
//                     switch (triggerEvent.State)
//                     {
//                         case StatefulEventState.Exit:
//                 
//                             InteractUtils.Remove(ref targets,target);
//                             break;
//                         case StatefulEventState.Stay :
//                             InteractUtils.NoDupAdd(ref targets, insightTarget);
//                             break;
//                         case StatefulEventState.Undefined:
//                             break;
//                         default:
//                             return;
//                     }
//                 }
//                 
//            
//                 
//             }
//
//             
//             
//
//
//   
//         }
//         
//         
//         
//
//
//
//     }
// }