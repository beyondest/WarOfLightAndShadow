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
    public partial struct CheckSightTriggerSystem : ISystem
    {
        private ComponentLookup<InteractPriority> _priorityLookup;
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<SimulationSingleton>();
            state.RequireForUpdate<NotPauseTag>();
            state.RequireForUpdate<AutoChooseTargetSystemConfig>();
            // _interactableLookup = state.GetComponentLookup<InteractableAttr>(true);
            _priorityLookup = state.GetComponentLookup<InteractPriority>(true);
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            _priorityLookup.Update(ref state);
            // _interactableLookup.Update(ref state);
            // Calculate disValue and check trigger events
            new CheckSightTriggerJob
            {
                // InteractableAttrLookup = _interactableLookup,
                PriorityLookup = _priorityLookup,
            }.ScheduleParallel();
        }

        // Warning : This job cannot delay for even a little time;MUST be in front of all other jobs
        [BurstCompile]
        public partial struct CheckSightTriggerJob : IJobEntity
        {
            [ReadOnly] public ComponentLookup<InteractPriority> PriorityLookup;
            
            private void Execute(ref DynamicBuffer<StatefulTriggerEvent> events, ref DynamicBuffer<InsightTarget> targets,
                Entity entity)
            {
                
                // var selfFaction = InteractableAttrLookup[entity].FactionTag;
                
                // Add insight target, remove out sight target
                foreach (var triggerEvent in events)
                {
                    var target = triggerEvent.GetOtherEntity(entity);
                    
                    // Check if target is valid for target
                    if( !PriorityLookup.TryGetComponent(target, out InteractPriority priority))continue;

                    var insightTarget = new InsightTarget
                    {
                        Entity = target,
                        BaseValue = priority.Value,
                        DisValue = 0f,
                        StatChangValue = 0f,
                        InteractOverride = 0f
                    };
                    switch (triggerEvent.State)
                    {
                        // Enter must be first time target added to list, so don't need to check
                        case StatefulEventState.Enter:
                            // Debug.Log($"Enter :  A : {triggerEvent.EntityA},   : {triggerEvent.ColliderKeyA}\n " +
                            //           $" B : {triggerEvent.EntityB}, : {triggerEvent.ColliderKeyB} Self  : {entity}");
                            targets.Add(insightTarget);
                            break;
                        
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
                            int j;
                            for ( j= 0; j < targets.Length; j++)
                            {
                                if(targets[j].Entity == target)break;
                            }
                            if(j == targets.Length)targets.Add(insightTarget);
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