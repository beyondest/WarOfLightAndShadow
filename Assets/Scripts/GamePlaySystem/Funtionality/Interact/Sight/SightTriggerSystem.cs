using SparFlame.GamePlaySystem.General;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Physics;
using Unity.Physics.Stateful;
using Unity.Physics.Systems;
using UnityEngine;

namespace SparFlame.GamePlaySystem.Interact
{
    [UpdateInGroup(typeof(PhysicsSystemGroup))]
    [UpdateAfter(typeof(StatefulTriggerEventBufferSystem))]
    public partial struct SightTriggerSystem : ISystem
    {
        private ComponentLookup<InteractPriority> _priorityLookup;
        private BufferLookup<InsightTarget> _targetLookup;
        
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<SimulationSingleton>();
            state.RequireForUpdate<NotPauseTag>();
            state.RequireForUpdate<SightSystemConfig>();
            // _interactableLookup = state.GetComponentLookup<InteractableAttr>(true);
            _priorityLookup = state.GetComponentLookup<InteractPriority>(true);
            _targetLookup = state.GetBufferLookup<InsightTarget>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            _priorityLookup.Update(ref state);
            _targetLookup.Update(ref state);
            // _interactableLookup.Update(ref state);
            // Calculate disValue and check trigger events
            new SightTriggerJob
            {
                // InteractableAttrLookup = _interactableLookup,
                // PriorityLookup = _priorityLookup,
                TargetLookup = _targetLookup
            }.ScheduleParallel();
        }

        // Warning : This job cannot delay for even a little time;MUST be in front of all other jobs
        [BurstCompile]
        public partial struct SightTriggerJob : IJobEntity
        {
            // [ReadOnly] public ComponentLookup<InteractPriority> PriorityLookup;
            [NativeDisableParallelForRestriction] public BufferLookup<InsightTarget> TargetLookup;
            
            private void Execute(ref DynamicBuffer<StatefulTriggerEvent> events, in SightData data,
                Entity entity)
            {
                // This may happen when belongs to entity is dead but the sight not been removed by sight system yet
                if(!TargetLookup.TryGetBuffer(data.BelongsTo, out var targets))return;
                
                // var selfFaction = InteractableAttrLookup[entity].FactionTag;
                // Add insight target, remove out sight target
                foreach (var triggerEvent in events)
                {
                    var target = triggerEvent.GetOtherEntity(entity);
                    
                    // Check if target is valid for target
                    // if( !PriorityLookup.TryGetComponent(target, out var priority))continue;

                    var insightTarget = new InsightTarget
                    {
                        Entity = target,
                        PriorityValue = 0f,
                        DisValue = 0f,
                        StatChangValue = 0f,
                        InteractOverride = 0f,
                        MemoryValue = 0f,
                        TotalValue = 0f
                    };
                    switch (triggerEvent.State)
                    {
                        // // Enter must be first time target added to list, so don't need to check
                        // case StatefulEventState.Enter:
                        //     // Debug.Log($"Enter :  A : {triggerEvent.EntityA},   : {triggerEvent.ColliderKeyA}\n " +
                        //     //           $" B : {triggerEvent.EntityB}, : {triggerEvent.ColliderKeyB} Self  : {entity}");
                        //     targets.Add(insightTarget);
                        //     break;
                        //
                        case StatefulEventState.Exit:
                
                            // Debug.Log($"Exit :  A : {triggerEvent.EntityA},   : {triggerEvent.ColliderKeyA}\n " +
                            //           $" B : {triggerEvent.EntityB}, : {triggerEvent.ColliderKeyB} Self  : {entity}");
                            for (var i = targets.Length - 1; i >= 0; i--)
                            {
                                if(targets[i].Entity == target)
                                    targets.RemoveAt(i);
                            }
                            break;
                        // TODO : Split healer job from other units, cause this spends too much
                        // Healer must check the target even when stay because ally unit may get hurt after it gets insight to healer
                        case StatefulEventState.Stay :
                            InteractUtils.NoDupAdd(ref targets, insightTarget);
                            break;
                        case StatefulEventState.Undefined:
                            break;
                        default:
                            return;
                    }
                }
                
                
                /*foreach (var e in events)
                {
                    switch(e.State)
                    {
                        case StatefulEventState.Undefined:
                            break;
                        case StatefulEventState.Enter:
                            Debug.Log($"Enter :  A : {e.EntityA},   : {e.ColliderKeyA}\n " +
                                      $" B : {e.EntityB}, : {e.ColliderKeyB} Self  : {entity}");
                            break;
                        case StatefulEventState.Stay:
                            break;
                        case StatefulEventState.Exit:
                            Debug.Log($"Exit :  A : {e.EntityA},   : {e.ColliderKeyA}\n " +
                                      $" B : {e.EntityB}, : {e.ColliderKeyB} Self  : {entity}");
                            // Debug.Log($"Exit : Entity A : {entity}, Entity B : {e.GetOtherEntity(entity)}");
                            break;
                        default:
                            return;
                    }
                
                }*/
                
            }

            
            


  
        }
        
        
        



    }
}