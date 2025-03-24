using System;
using SparFlame.GamePlaySystem.State;
using Unity.Burst;
using Unity.Entities;
using Unity.Physics;
using Unity.Physics.Stateful;
using Unity.Physics.Systems;
using UnityEngine;
namespace DefaultNamespace
{
    [UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
    [UpdateAfter(typeof(PhysicsSystemGroup))]
    public partial struct TestStatefulTrigger : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<SimulationSingleton>();
            state.RequireForUpdate<TestTriggerAuthoring.EnableStateFulEvent>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            new TriggerEventsTest
            {

            }.ScheduleParallel();
        }

        [BurstCompile]
        public void OnDestroy(ref SystemState state)
        {

        }

        private partial struct TriggerEventsTest : IJobEntity
        {
            private void Execute(ref DynamicBuffer<StatefulTriggerEvent> events, Entity entity)
            {
                foreach (var e in events)
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
                            throw new ArgumentOutOfRangeException();
                    }

                }
            }
        }
    }
}