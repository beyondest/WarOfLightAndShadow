using SparFlame.GamePlaySystem.General;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Physics;
using Unity.Physics.Stateful;
using Unity.Physics.Systems;

namespace SparFlame.GamePlaySystem.Interact
{


    public struct AoeTarget : IBufferElementData
    {
        public Entity Entity;
    }

    public struct AoeTriggerData : IComponentData
    {
        public Entity BelongsTo;
    }
    
    
    [UpdateInGroup(typeof(PhysicsSystemGroup))]
    [UpdateAfter(typeof(StatefulTriggerEventBufferSystem))]
    public partial struct AoeTriggerSystem : ISystem
    {
        private BufferLookup<AoeTarget> _targetLookup;
        
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<SimulationSingleton>();
            state.RequireForUpdate<GamingTag>();
            state.RequireForUpdate<SightSystemConfig>();
            state.RequireForUpdate<SightData>();
            _targetLookup = state.GetBufferLookup<AoeTarget>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            _targetLookup.Update(ref state);
            new AoeTriggerJob
            {
                TargetLookup = _targetLookup
            }.ScheduleParallel();
        }

        [BurstCompile]
        public partial struct AoeTriggerJob : IJobEntity
        {
            [NativeDisableParallelForRestriction] public BufferLookup<AoeTarget> TargetLookup;
            private void Execute(ref DynamicBuffer<StatefulTriggerEvent> events, in AoeTriggerData triggerData,
                Entity entity)
            {
                // This may happen when belongs to entity is dead but the sight not been removed by sight system yet
                if (!TargetLookup.TryGetBuffer(triggerData.BelongsTo, out var targets)) return;
                foreach (var triggerEvent in events)
                {
                    var target = triggerEvent.GetOtherEntity(entity);
                    switch (triggerEvent.State)
                    {
                        case StatefulEventState.Enter:
                            int j;
                            for ( j= 0; j < targets.Length; j++)
                            {
                                if(targets[j].Entity == target)break;
                            }
                            if (j != targets.Length) break;
                            targets.Add(new AoeTarget
                            {
                                Entity = target,
                            });
                            break;
                        case StatefulEventState.Exit:
                            for (var i = targets.Length - 1; i >= 0; i--)
                            {
                                if (targets[i].Entity == target)
                                {
                                    targets.RemoveAt(i);
                                    break;
                                }
                            }
                            break;
                        case StatefulEventState.Stay:
                            break;
                        case StatefulEventState.Undefined:
                            break;
                        default:
                            return;
                    }
                }
            }
        }
        
        



    }
}