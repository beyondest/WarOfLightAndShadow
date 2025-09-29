using SparFlame.Components.General;
using SparFlame.Components.SubGameplay;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Physics.Stateful;
using Unity.Physics.Systems;

namespace SparFlame.Systems.SubGameplay.Movement.FakeCollision
{

    
    
    
    [UpdateInGroup(typeof(PhysicsSystemGroup))]
    [UpdateAfter(typeof(StatefulTriggerEventBufferSystem))]
    public partial struct FakeCollisionTriggerAddTargetsSystem : ISystem
    {
        private BufferLookup<FakeColliderTarget> _targetLookup;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<FakeCollisionTriggerData>();
            state.RequireForUpdate<SubGamingTag>();
            _targetLookup =state.GetBufferLookup<FakeColliderTarget>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            _targetLookup.Update(ref state);
            new FakeCollisionTriggerJob
            {
                TargetLookup = _targetLookup,
            }.ScheduleParallel();
        }

        [BurstCompile]
        public void OnDestroy(ref SystemState state)
        {

        }
        
        [BurstCompile]
        public partial struct FakeCollisionTriggerJob : IJobEntity
        {
            [NativeDisableParallelForRestriction] public BufferLookup<FakeColliderTarget> TargetLookup;
            private void Execute(ref DynamicBuffer<StatefulTriggerEvent> events, in FakeCollisionTriggerData data,
                Entity entity)
            {
                // This may happen when belongs to entity is dead but the sight not been removed by sight system yet
                if(!TargetLookup.TryGetBuffer(data.BelongsTo, out var targets))return;
                targets.Clear();
                foreach (var triggerEvent in events)
                {
                    
                    switch (triggerEvent.State)
                    {
                        case StatefulEventState.Stay :
                            var target = triggerEvent.GetOtherEntity(entity);
                            var fakeColliderTarget = new FakeColliderTarget
                            {
                                Target   = target
                            };
                            targets.Add(fakeColliderTarget);
                            break;
                        case StatefulEventState.Undefined:
                        case StatefulEventState.Exit:
                        case StatefulEventState.Enter:
                        default:
                            return;
                    }
                }
            }

            
            


  
        }
        
        
    }
}