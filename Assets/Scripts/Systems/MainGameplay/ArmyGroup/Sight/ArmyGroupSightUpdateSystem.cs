using SparFlame.Components.General;
using SparFlame.Systems.MainGameplay.ArmyGroup;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Physics;
using Unity.Physics.Stateful;
using Unity.Physics.Systems;

namespace SparFlame.Systems.SubGameplay.Interact
{
    
    [UpdateInGroup(typeof(PhysicsSystemGroup))]
    [UpdateAfter(typeof(StatefulTriggerEventBufferSystem))]
    public partial struct ArmyGroupSightUpdateSystem : ISystem
    {
        private BufferLookup<ArmyGroupSightTarget> _targetLookup;
        
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<SimulationSingleton>();
            state.RequireForUpdate<MainGamingTag>();
            state.RequireForUpdate<ArmyGroupSightData>();
            _targetLookup = state.GetBufferLookup<ArmyGroupSightTarget>();
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
            [NativeDisableParallelForRestriction] public BufferLookup<ArmyGroupSightTarget> TargetLookup;
            private void Execute(ref DynamicBuffer<StatefulTriggerEvent> events, in ArmyGroupSightData triggerData,
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
                            targets.Add(new ArmyGroupSightTarget
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