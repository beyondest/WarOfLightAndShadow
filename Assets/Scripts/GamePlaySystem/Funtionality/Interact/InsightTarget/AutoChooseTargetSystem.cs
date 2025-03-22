using System;
using System.Runtime.CompilerServices;
using SparFlame.GamePlaySystem.General;
using SparFlame.GamePlaySystem.Movement;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics.Stateful;
using Unity.Transforms;

namespace SparFlame.GamePlaySystem.Interact
{
    [UpdateAfter(typeof(VolumeObstacleSystem))]
    public partial struct AutoChooseTargetSystem : ISystem
    {
        private ComponentLookup<InteractableAttr> _interactableLookup;
        private ComponentLookup<StatData> _statDataLookup;
        private ComponentLookup<InteractPriority> _priorityLookup;
        private ComponentLookup<LocalTransform> _localTransformLookup;
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<NotPauseTag>();
            state.RequireForUpdate<AutoChooseTargetSystemConfig>();
            _interactableLookup = state.GetComponentLookup<InteractableAttr>(true);
            _statDataLookup = state.GetComponentLookup<StatData>(true);
            _priorityLookup = state.GetComponentLookup<InteractPriority>(true);
            _localTransformLookup = state.GetComponentLookup<LocalTransform>(true);
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            _localTransformLookup.Update(ref state);
            _priorityLookup.Update(ref state);
            _statDataLookup.Update(ref state);
            _interactableLookup.Update(ref state);
            var config = SystemAPI.GetSingleton<AutoChooseTargetSystemConfig>();
            // Calculate disValue and check trigger events
            new UpDateTargetListJob
            {
                Config = config,
                InteractableAttrLookup = _interactableLookup,
                StatDataLookup = _statDataLookup,
                PriorityLookup = _priorityLookup,
                TransformLookup = _localTransformLookup,
            }.ScheduleParallel();
        }

        [BurstCompile]
        public partial struct UpDateTargetListJob : IJobEntity
        {
            [ReadOnly] public ComponentLookup<InteractableAttr> InteractableAttrLookup;
            [ReadOnly] public ComponentLookup<StatData> StatDataLookup;
            [ReadOnly] public ComponentLookup<InteractPriority> PriorityLookup;
            [ReadOnly] public ComponentLookup<LocalTransform> TransformLookup;
            [ReadOnly] public AutoChooseTargetSystemConfig Config;
            
            private void Execute(ref DynamicBuffer<StatefulTriggerEvent> events, ref DynamicBuffer<InsightTarget> targets,
                Entity entity)
            {
                
                var selfFaction = InteractableAttrLookup[entity].FactionTag;
                var selfPos = TransformLookup[entity].Position;
                Entity target;
                FactionTag targetFaction;
                StatData targetStat;
                float3 targetPosition;
                
                for (var i = targets.Length - 1; i >=0 ; i--)
                {
                    var insightTarget = targets[i];
                    target = insightTarget.Entity;
                    targetStat = StatDataLookup[target];
                    targetFaction = InteractableAttrLookup[target].FactionTag;
                    // Remove invalid target
                    if (!InteractUtils.IsTargetValid(targetFaction, selfFaction,in targetStat))
                    {
                        targets.RemoveAt(i);
                        continue;
                    }
                    // Update value via position
                    targetPosition = TransformLookup[target].Position;
                    insightTarget.DisValue = CalDisPriority(ref targetPosition,ref selfPos,ref Config);
                    targets[i] = insightTarget;
                }
                
                // Add insight target, remove out sight target
                foreach (var triggerEvent in events)
                {
                    switch (triggerEvent.State)
                    {
                        case StatefulEventState.Enter:
                            target = triggerEvent.GetOtherEntity(entity);
                            targetFaction = InteractableAttrLookup[target].FactionTag;
                            targetStat = StatDataLookup[target];
                            // Check if target is valid
                            if(!InteractUtils.IsTargetValid(targetFaction, selfFaction,in targetStat))continue;
                            var priority = PriorityLookup.GetRefRO(target).ValueRO.Value;
                            
                            // Apply the priority lifting of different interact types
                            if (targetFaction == FactionTag.Neutral)
                            {   // Harvest
                                priority += Config.HarvestAboveAttack;
                            }
                            else
                            {   // Heal
                                if (targetFaction == selfFaction)
                                {
                                    priority += Config.HealAboveAttack;
                                }
                            }
                            targetPosition = TransformLookup[target].Position;
                            
                            // Add
                            var insightTarget = new InsightTarget
                            {
                                BaseValue = priority,
                                DisValue = CalDisPriority(ref targetPosition,ref selfPos,ref Config),
                                Entity = target,
                                StatChangValue = 0
                            };
                            targets.Add(insightTarget);
                            break;
                        
                        case StatefulEventState.Exit:
                            var targetExit = triggerEvent.GetOtherEntity(entity);
                            for (var i = targets.Length - 1; i >= 0; i--)
                            {
                                if(targets[i].Entity == targetExit)
                                    targets.RemoveAt(i);
                            }
                            break;
                        case StatefulEventState.Stay:
                        case StatefulEventState.Undefined:
                        default:
                            throw new ArgumentOutOfRangeException();
                        
                    }
                }
            }

            
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private static float CalDisPriority(ref float3 targetPosition, ref float3 selfPos, ref AutoChooseTargetSystemConfig config)
            {
                var disSq = math.distancesq(targetPosition, selfPos);
                return math.max(0f, config.BaseLineDistanceSq - disSq);
            }


  
        }
        
        
        



    }
}